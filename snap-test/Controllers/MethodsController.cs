using Microsoft.AspNetCore.Mvc;

namespace snap_test.Controllers
{
    /// <summary>
    /// One endpoint per HTTP verb. Each path accepts only its own verb, so any other verb gets 405 + Allow header.
    /// </summary>
    [ApiController]
    [Route("api/methods")]
    public class MethodsController : ControllerBase
    {
        private const int MaxBodyBytes = 1024 * 1024;

        private static readonly string[] AllMethods = { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS" };

        // -------------------- GET --------------------
        /// <summary>Accepts only GET and describes the request.</summary>
        /// <response code="200">Method, path, query and timestamp.</response>
        /// <response code="405">Any other verb; the <c>Allow</c> header lists GET.</response>
        [HttpGet("get")]
        public Task<IActionResult> Get() => Describe(readBody: false);

        // -------------------- POST --------------------
        /// <summary>Accepts only POST and echoes the body.</summary>
        /// <remarks>Up to 1MB of body is echoed; larger bodies are counted and flagged with <c>bodyTruncated</c>.</remarks>
        /// <response code="200">Method, query, content type and body.</response>
        /// <response code="405">Any other verb.</response>
        [HttpPost("post")]
        public Task<IActionResult> Post() => Describe(readBody: true);

        // -------------------- PUT --------------------
        /// <summary>Accepts only PUT and echoes the body.</summary>
        /// <response code="200">Method, query, content type and body.</response>
        /// <response code="405">Any other verb.</response>
        [HttpPut("put")]
        public Task<IActionResult> Put() => Describe(readBody: true);

        // -------------------- PATCH --------------------
        /// <summary>Accepts only PATCH and echoes the body.</summary>
        /// <response code="200">Method, query, content type and body.</response>
        /// <response code="405">Any other verb.</response>
        [HttpPatch("patch")]
        public Task<IActionResult> Patch() => Describe(readBody: true);

        // -------------------- DELETE --------------------
        /// <summary>Accepts only DELETE and echoes any body sent with it.</summary>
        /// <response code="200">Method, query, content type and body.</response>
        /// <response code="405">Any other verb.</response>
        [HttpDelete("delete")]
        public Task<IActionResult> Delete() => Describe(readBody: true);

        // -------------------- HEAD (headers only, no body) --------------------
        /// <summary>Accepts only HEAD and answers with headers only.</summary>
        /// <remarks>Look for the <c>X-Method</c> and <c>X-Message</c> response headers.</remarks>
        /// <response code="200">Empty body with <c>X-Method: HEAD</c>.</response>
        /// <response code="405">Any other verb.</response>
        [HttpHead("head")]
        public IActionResult Head()
        {
            Response.Headers["X-Method"] = "HEAD";
            Response.Headers["X-Message"] = "HEAD request received. Responses to HEAD carry headers only.";
            return Ok();
        }

        // -------------------- OPTIONS --------------------
        /// <summary>Accepts only OPTIONS and lists the per-verb test endpoints.</summary>
        /// <remarks>A plain OPTIONS request reaches this action. A CORS preflight (with <c>Origin</c> and
        /// <c>Access-Control-Request-Method</c>) is answered by the CORS middleware instead.</remarks>
        /// <response code="200">An <c>Allow</c> header plus the endpoint list.</response>
        /// <response code="405">Any other verb.</response>
        [HttpOptions("options")]
        public IActionResult Options()
        {
            Response.Headers.Allow = "OPTIONS";
            return Ok(new
            {
                method = "OPTIONS",
                message = "OPTIONS request received.",
                endpoints = AllMethods.Select(m => new { method = m, path = $"/api/methods/{m.ToLower()}" })
            });
        }

        // -------------------- ANY VERB --------------------
        /// <summary>Accepts any verb and describes the request.</summary>
        /// <remarks>The body is read for POST, PUT, PATCH and DELETE, and ignored for GET, HEAD and OPTIONS.</remarks>
        /// <response code="200">Method, query, content type and body.</response>
        [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS", Route = "any")]
        public Task<IActionResult> Any() => Describe(readBody: Request.Method is not ("GET" or "HEAD" or "OPTIONS"));

        private async Task<IActionResult> Describe(bool readBody)
        {
            string? body = null;
            long bodyLength = 0;
            var truncated = false;

            if (readBody)
            {
                var (bytes, total, cut) = await HttpRequestReader.ReadCappedAsync(Request, MaxBodyBytes);
                body = bytes.Length == 0 ? null : HttpRequestReader.TryDecodeUtf8(bytes) ?? Convert.ToBase64String(bytes);
                bodyLength = total;
                truncated = cut;
            }

            return Ok(new
            {
                method = Request.Method,
                message = $"{Request.Method} request received",
                path = Request.Path.Value,
                query = HttpRequestReader.Flatten(Request.Query),
                contentType = Request.ContentType,
                bodyLength,
                bodyTruncated = truncated,
                body,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            });
        }
    }
}
