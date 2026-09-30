using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// HTTP caching and conditional requests: ETag / If-None-Match (304), If-Match (412 / 428),
    /// Last-Modified / If-Modified-Since, Cache-Control variants and Vary.
    /// </summary>
    [ApiController]
    [Route("api/cache")]
    public class CachingController : ControllerBase
    {
        private const string DefaultTitle = "APIBee cached document";
        private const string DefaultContent = "Send If-None-Match to get a 304, and If-Match on PUT to make a conditional update.";
        private const string DefaultUpdatedAt = "2025-01-01T00:00:00Z";

        private static readonly DateTimeOffset LastModified = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private static readonly Dictionary<string, string> Greetings = new()
        {
            ["en"] = "Hello, welcome to APIBee!",
            ["es"] = "¡Hola, bienvenido a APIBee!",
            ["fr"] = "Bonjour, bienvenue sur APIBee !",
            ["de"] = "Hallo, willkommen bei APIBee!",
            ["hi"] = "नमस्ते, APIBee में आपका स्वागत है!"
        };

        // Versioned in-memory document behind the ETag endpoints.
        private static readonly object Gate = new();
        private static int _version = 1;
        private static string _title = DefaultTitle;
        private static string _content = DefaultContent;
        private static string _updatedAt = DefaultUpdatedAt;

        /// <summary>Fields to change on the cached document. At least one is required.</summary>
        public class DocumentUpdate
        {
            /// <summary>New title, or null to keep the current one.</summary>
            public string? Title { get; set; }
            /// <summary>New content, or null to keep the current one.</summary>
            public string? Content { get; set; }
        }

        // -------------------- ETAG: GET (If-None-Match -> 304) --------------------
        /// <summary>Returns the versioned document with its ETag.</summary>
        /// <remarks>
        /// Reads <c>If-None-Match</c>: when it matches the current ETag (weak comparison, <c>*</c> matches anything), the
        /// response is 304 with no body. The ETag has the form <c>"doc-v{version}"</c>. Also answers HEAD.
        /// </remarks>
        /// <response code="200">The document, with <c>ETag</c> and <c>Cache-Control: no-cache</c>.</response>
        /// <response code="304">The client's copy is current.</response>
        [HttpGet("etag")]
        [HttpHead("etag")]
        public IActionResult GetETag()
        {
            lock (Gate)
            {
                var etag = ETagFor(_version);
                Response.Headers.ETag = etag;
                Response.Headers.CacheControl = "no-cache";

                if (Matches(Request.Headers.IfNoneMatch.ToString(), etag, weak: true))
                    return StatusCode(304);

                return Ok(new { id = 1, version = _version, title = _title, content = _content, updatedAt = _updatedAt, etag });
            }
        }

        // -------------------- ETAG: CONDITIONAL UPDATE (If-Match -> 412 / 428) --------------------
        /// <summary>Updates the document only if the client's ETag is current.</summary>
        /// <param name="body">New title and/or content.</param>
        /// <remarks>
        /// Requires <c>If-Match</c> with the current ETag (strong comparison, so <c>W/</c> tags never match; <c>*</c> matches).
        /// A successful update increments the version and returns the new ETag. Use it to test lost-update protection.
        /// </remarks>
        /// <response code="200">Updated; the new ETag is in the header and body.</response>
        /// <response code="400">Neither title nor content supplied, or malformed body.</response>
        /// <response code="412">If-Match does not match the current ETag.</response>
        /// <response code="428">If-Match header missing.</response>
        [HttpPut("etag")]
        public IActionResult PutETag([FromBody] DocumentUpdate body)
        {
            var ifMatch = Request.Headers.IfMatch.ToString();

            lock (Gate)
            {
                var current = ETagFor(_version);

                if (string.IsNullOrWhiteSpace(ifMatch))
                {
                    Response.Headers.ETag = current;
                    return StatusCode(428, ApiResponse.Error(428, $"If-Match header is required. GET /api/cache/etag and send its ETag (currently {current})."));
                }

                if (!Matches(ifMatch, current, weak: false))
                {
                    Response.Headers.ETag = current;
                    return StatusCode(412, ApiResponse.Error(412, $"ETag mismatch: document has changed. Current ETag is {current}."));
                }

                if (body.Title == null && body.Content == null)
                    return BadRequest(ApiResponse.Error(400, "Provide at least one of: title, content."));

                _title = body.Title ?? _title;
                _content = body.Content ?? _content;
                _version++;
                _updatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                var etag = ETagFor(_version);
                Response.Headers.ETag = etag;
                return Ok(new
                {
                    message = "Document updated",
                    data = new { id = 1, version = _version, title = _title, content = _content, updatedAt = _updatedAt, etag }
                });
            }
        }

        // -------------------- ETAG: RESET --------------------
        /// <summary>Resets the ETag document to version 1 and its default content.</summary>
        /// <response code="200">Document reset.</response>
        [HttpPost("etag/reset")]
        public IActionResult ResetETag()
        {
            lock (Gate)
            {
                _version = 1;
                _title = DefaultTitle;
                _content = DefaultContent;
                _updatedAt = DefaultUpdatedAt;
                Response.Headers.ETag = ETagFor(_version);
            }

            return Ok(new { message = "Document reset to version 1" });
        }

        // -------------------- LAST-MODIFIED (If-Modified-Since -> 304) --------------------
        /// <summary>Returns a resource with a fixed Last-Modified date.</summary>
        /// <remarks>
        /// Reads <c>If-Modified-Since</c>: a date on or after Wed, 01 Jan 2025 00:00:00 GMT gives a 304. Invalid dates are
        /// ignored. Also answers HEAD.
        /// </remarks>
        /// <response code="200">The resource, with a <c>Last-Modified</c> header.</response>
        /// <response code="304">Not modified since the given date.</response>
        [HttpGet("last-modified")]
        [HttpHead("last-modified")]
        public IActionResult GetLastModified()
        {
            Response.GetTypedHeaders().LastModified = LastModified;
            Response.Headers.CacheControl = "no-cache";

            var since = Request.GetTypedHeaders().IfModifiedSince;
            if (since.HasValue && LastModified <= since.Value)
                return StatusCode(304);

            return Ok(new
            {
                title = "Unchanged since New Year 2025",
                lastModified = LastModified.ToString("R"),
                hint = "Send If-Modified-Since with this date (or later) to get 304 Not Modified."
            });
        }

        // -------------------- MAX-AGE --------------------
        /// <summary>Returns a response that clients may cache for the given number of seconds.</summary>
        /// <param name="seconds">Cache lifetime, clamped to 0-31536000 (one year).</param>
        /// <remarks>Sets <c>Cache-Control: public, max-age=N</c> and <c>Expires</c>. A caching client reuses the same <c>generatedAt</c> until it expires.</remarks>
        /// <response code="200">Body with the generation timestamp.</response>
        [HttpGet("max-age/{seconds:int}")]
        public IActionResult MaxAge(int seconds)
        {
            seconds = Math.Clamp(seconds, 0, 31_536_000);
            Response.Headers.CacheControl = $"public, max-age={seconds}";
            Response.Headers.Expires = DateTimeOffset.UtcNow.AddSeconds(seconds).ToString("R");

            return Ok(new
            {
                maxAge = seconds,
                generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                hint = "A caching client should reuse this response (same generatedAt) until it expires."
            });
        }

        // -------------------- NO-STORE --------------------
        /// <summary>Returns a response that must never be cached.</summary>
        /// <remarks>Sets <c>Cache-Control: no-store, no-cache, must-revalidate</c>, <c>Pragma: no-cache</c> and <c>Expires: 0</c>. Each call returns a new <c>requestId</c>.</remarks>
        /// <response code="200">Body with a fresh request id.</response>
        [HttpGet("no-store")]
        public IActionResult NoStore()
        {
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";

            return Ok(new
            {
                requestId = Guid.NewGuid().ToString(),
                generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                hint = "Never cached: every request returns a new requestId."
            });
        }

        // -------------------- VARY (Accept-Language) --------------------
        /// <summary>Returns a greeting in the language chosen by Accept-Language.</summary>
        /// <remarks>
        /// Reads <c>Accept-Language</c> (quality values honoured) and supports en, es, fr, de and hi, falling back to en.
        /// Sets <c>Vary: Accept-Language</c> and <c>Content-Language</c>. Example: <c>Accept-Language: fr-CA, es;q=0.9</c> gives French.
        /// </remarks>
        /// <response code="200">The chosen language and greeting.</response>
        [HttpGet("vary")]
        public IActionResult Vary()
        {
            var lang = Request.GetTypedHeaders().AcceptLanguage
                .OrderByDescending(l => l.Quality ?? 1.0)
                .Select(l => l.Value.ToString().Split('-')[0].ToLowerInvariant())
                .FirstOrDefault(l => Greetings.ContainsKey(l)) ?? "en";

            Response.Headers.Vary = HeaderNames.AcceptLanguage;
            Response.Headers.ContentLanguage = lang;
            Response.Headers.CacheControl = "public, max-age=60";

            return Ok(new { language = lang, greeting = Greetings[lang], supported = Greetings.Keys });
        }

        private static string ETagFor(int version) => $"\"doc-v{version}\"";

        // Compares an If-None-Match / If-Match header value against the current ETag.
        // Weak comparison (If-None-Match) ignores the W/ prefix; strong comparison (If-Match) does not.
        private static bool Matches(string header, string etag, bool weak)
        {
            if (string.IsNullOrWhiteSpace(header)) return false;

            foreach (var raw in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (raw == "*") return true;

                var isWeak = raw.StartsWith("W/", StringComparison.Ordinal);
                if (isWeak && !weak) continue;

                if ((isWeak ? raw[2..] : raw) == etag) return true;
            }

            return false;
        }
    }
}
