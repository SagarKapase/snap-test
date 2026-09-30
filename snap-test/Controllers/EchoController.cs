using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace snap_test.Controllers
{
    /// <summary>
    /// Request inspection (httpbin-style). Echoes back everything the server received, for any verb and any sub-path.
    /// </summary>
    [ApiController]
    [Route("api/echo")]
    public class EchoController : ControllerBase
    {
        private const int MaxEchoBytes = 1024 * 1024; // 1MB of body is echoed; the rest is counted but dropped

        // -------------------- ECHO (any verb, any sub-path) --------------------
        /// <summary>Echoes back everything the server received: method, URL, query, headers, cookies, body and client info.</summary>
        /// <remarks>
        /// Accepts GET, POST, PUT, PATCH, DELETE, HEAD and OPTIONS on <c>/api/echo</c> and on any sub-path
        /// (<c>/api/echo/{**path}</c>); the sub-path is returned as <c>subPath</c>.
        /// Up to 1MB of body is echoed (UTF-8 text, or base64 for binary); larger bodies are counted in
        /// <c>contentLength</c> and flagged with <c>bodyTruncated</c>. JSON bodies are also returned parsed in <c>json</c>
        /// (or <c>jsonError</c> when malformed), and form bodies as <c>form</c> fields plus <c>files</c> metadata.
        /// Example: <c>POST /api/echo/orders/7?tag=a&amp;tag=b</c> with a JSON body.
        /// </remarks>
        /// <response code="200">The request as the server saw it (HEAD responses carry headers only).</response>
        [Route("")]
        [Route("{**path}")]
        [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS")]
        // No action parameters on purpose: a bindable parameter makes MVC's form value provider consume
        // form bodies before the action runs, which would hide the raw body.
        public async Task<IActionResult> Echo()
        {
            var path = RouteData.Values["path"]?.ToString();

            Request.EnableBuffering();
            var (bytes, totalBytes, truncated) = await HttpRequestReader.ReadCappedAsync(Request, MaxEchoBytes);
            Request.Body.Position = 0;

            string? body = null;
            string? bodyEncoding = null;
            if (bytes.Length > 0)
            {
                body = HttpRequestReader.TryDecodeUtf8(bytes);
                bodyEncoding = "utf-8";
                if (body == null)
                {
                    body = Convert.ToBase64String(bytes);
                    bodyEncoding = "base64";
                }
            }

            object? json = null;
            string? jsonError = null;
            if (bytes.Length > 0 && !truncated && HttpRequestReader.IsJsonContentType(Request.ContentType))
            {
                try
                {
                    using var doc = JsonDocument.Parse(bytes);
                    json = doc.RootElement.Clone();
                }
                catch (JsonException ex)
                {
                    jsonError = ex.Message;
                }
            }

            object? form = null;
            object? files = null;
            if (Request.HasFormContentType)
            {
                try
                {
                    var f = await Request.ReadFormAsync();
                    form = HttpRequestReader.Flatten(f);
                    files = f.Files.Select(x => new
                    {
                        field = x.Name,
                        fileName = x.FileName,
                        contentType = x.ContentType,
                        size = x.Length
                    }).ToList();
                }
                catch (Exception ex) when (ex is InvalidDataException or IOException)
                {
                    form = new { parseError = ex.Message };
                }
            }

            return Ok(new
            {
                method = Request.Method,
                url = Request.GetDisplayUrl(),
                path = Request.Path.Value,
                subPath = path,
                queryString = Request.QueryString.Value,
                query = HttpRequestReader.Flatten(Request.Query),
                headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                cookies = Request.Cookies.ToDictionary(c => c.Key, c => c.Value),
                contentType = Request.ContentType,
                contentLength = totalBytes,
                body,
                bodyEncoding,
                bodyTruncated = truncated,
                json,
                jsonError,
                form,
                files,
                client = ClientInfo(),
                protocol = Request.Protocol,
                scheme = Request.Scheme,
                host = Request.Host.Value,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            });
        }

        // -------------------- HEADERS --------------------
        /// <summary>Returns the request's headers.</summary>
        /// <response code="200">Header count and a name-to-value map.</response>
        [HttpGet("headers")]
        public IActionResult Headers() => Ok(new
        {
            count = Request.Headers.Count,
            headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
        });

        // -------------------- IP --------------------
        /// <summary>Returns the caller's IP address and port.</summary>
        /// <remarks>Also reports the <c>X-Forwarded-For</c> and <c>X-Real-IP</c> headers when present (display only, not trusted).</remarks>
        /// <response code="200">Client IP, port and forwarded headers.</response>
        [HttpGet("ip")]
        public IActionResult Ip() => Ok(ClientInfo());

        // -------------------- USER AGENT --------------------
        /// <summary>Returns the request's User-Agent header.</summary>
        /// <response code="200">The User-Agent value.</response>
        [HttpGet("user-agent")]
        public IActionResult UserAgent() => Ok(new { userAgent = Request.Headers.UserAgent.ToString() });

        private object ClientInfo() => new
        {
            ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
            port = HttpContext.Connection.RemotePort,
            forwardedFor = NullIfEmpty(Request.Headers["X-Forwarded-For"]),
            realIp = NullIfEmpty(Request.Headers["X-Real-IP"])
        };

        private static string? NullIfEmpty(StringValues v) => StringValues.IsNullOrEmpty(v) ? null : v.ToString();
    }

    /// <summary>
    /// Shared request-reading helpers for the HTTP fundamentals controllers (echo, methods, bodies).
    /// </summary>
    internal static class HttpRequestReader
    {
        private static readonly UTF8Encoding StrictUtf8 = new(false, true);

        /// <summary>Reads the whole body, keeping at most maxBytes. Returns the total length seen.</summary>
        public static async Task<(byte[] data, long total, bool truncated)> ReadCappedAsync(HttpRequest request, int maxBytes)
        {
            using var ms = new MemoryStream();
            var buffer = new byte[81920];
            long total = 0;
            int read;

            while ((read = await request.Body.ReadAsync(buffer, request.HttpContext.RequestAborted)) > 0)
            {
                var keep = (int)Math.Max(0, Math.Min(read, maxBytes - ms.Length));
                if (keep > 0) ms.Write(buffer, 0, keep);
                total += read;
            }

            return (ms.ToArray(), total, total > maxBytes);
        }

        /// <summary>Reads the body, or returns null as soon as it grows past maxBytes (caller answers 413).</summary>
        public static async Task<byte[]?> ReadLimitedAsync(HttpRequest request, int maxBytes)
        {
            using var ms = new MemoryStream();
            var buffer = new byte[81920];
            int read;

            while ((read = await request.Body.ReadAsync(buffer, request.HttpContext.RequestAborted)) > 0)
            {
                if (ms.Length + read > maxBytes) return null;
                ms.Write(buffer, 0, read);
            }

            return ms.ToArray();
        }

        public static string? TryDecodeUtf8(byte[] data)
        {
            try { return StrictUtf8.GetString(data); }
            catch (DecoderFallbackException) { return null; }
        }

        public static bool IsJsonContentType(string? contentType) =>
            contentType != null &&
            (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
             contentType.Contains("+json", StringComparison.OrdinalIgnoreCase) ||
             contentType.Contains("text/json", StringComparison.OrdinalIgnoreCase));

        /// <summary>Single values become strings, repeated keys become arrays.</summary>
        public static Dictionary<string, object> Flatten(IEnumerable<KeyValuePair<string, StringValues>> values) =>
            values.ToDictionary(kv => kv.Key, kv => kv.Value.Count == 1 ? (object)kv.Value.ToString() : kv.Value.ToArray());
    }
}
