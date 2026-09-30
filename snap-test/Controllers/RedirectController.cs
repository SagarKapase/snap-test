using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// Redirect chains, every redirect status, a local-only redirect-to, and an infinite loop for max-redirect tests.
    /// Every chain ends at /api/echo so the client can see the final method and body it arrived with.
    /// </summary>
    [ApiController]
    [Route("api/redirect")]
    public class RedirectController : ControllerBase
    {
        private const int MaxRedirects = 20;
        private const string FinalTarget = "/api/echo";

        private static readonly HashSet<int> RedirectCodes = new() { 301, 302, 303, 307, 308 };

        // -------------------- CHAIN (n hops, relative Location paths) --------------------
        /// <summary>Starts a redirect chain of <c>n</c> hops that ends at <c>/api/echo</c>.</summary>
        /// <param name="n">Number of redirects remaining, 1-20. Each hop uses a path-absolute Location (<c>/api/redirect/{n-1}</c>).</param>
        /// <response code="302">Redirect to the next hop.</response>
        /// <response code="400"><c>n</c> outside 1-20.</response>
        [HttpGet("{n:int}")]
        public IActionResult Chain(int n)
        {
            if (n < 1 || n > MaxRedirects)
                return BadRequest(ApiResponse.Error(400, $"n must be between 1 and {MaxRedirects}."));

            return RedirectTo(n == 1 ? FinalTarget : $"/api/redirect/{n - 1}", 302, n);
        }

        // -------------------- CHAIN WITH ABSOLUTE URLS --------------------
        /// <summary>Starts a redirect chain of <c>n</c> hops that uses absolute URLs.</summary>
        /// <param name="n">Number of redirects remaining, 1-20.</param>
        /// <remarks>The Location host is taken from the request's <c>Host</c> header.</remarks>
        /// <response code="302">Redirect to the next hop, as a full URL.</response>
        /// <response code="400"><c>n</c> outside 1-20.</response>
        [HttpGet("absolute/{n:int}")]
        public IActionResult Absolute(int n)
        {
            if (n < 1 || n > MaxRedirects)
                return BadRequest(ApiResponse.Error(400, $"n must be between 1 and {MaxRedirects}."));

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            return RedirectTo(n == 1 ? baseUrl + FinalTarget : $"{baseUrl}/api/redirect/absolute/{n - 1}", 302, n);
        }

        // -------------------- CHAIN WITH RELATIVE REFERENCES --------------------
        // Location values like "2" and "../../echo" must be resolved against the current URL by the client.
        /// <summary>Starts a redirect chain of <c>n</c> hops that uses relative references.</summary>
        /// <param name="n">Number of redirects remaining, 1-20.</param>
        /// <remarks>Location values such as <c>2</c> and <c>../../echo</c> must be resolved against the current URL by the client.</remarks>
        /// <response code="302">Redirect to the next hop.</response>
        /// <response code="400"><c>n</c> outside 1-20.</response>
        [HttpGet("relative/{n:int}")]
        public IActionResult Relative(int n)
        {
            if (n < 1 || n > MaxRedirects)
                return BadRequest(ApiResponse.Error(400, $"n must be between 1 and {MaxRedirects}."));

            return RedirectTo(n == 1 ? "../../echo" : (n - 1).ToString(), 302, n);
        }

        // -------------------- SPECIFIC REDIRECT STATUS (any verb) --------------------
        // POST to /status/303 -> client follows with GET (body dropped).
        // POST to /status/307 or /308 -> client must repeat POST with the same body.
        /// <summary>Redirects to <c>/api/echo</c> with the chosen redirect status.</summary>
        /// <param name="code">Redirect status: 301, 302, 303, 307 or 308.</param>
        /// <remarks>
        /// Accepts GET, POST, PUT, PATCH, DELETE and HEAD. Use it to check client behaviour: after a POST, a 303 should be
        /// followed with GET (body dropped), while 307 and 308 must repeat the same method and body. <c>/api/echo</c> shows
        /// what arrived.
        /// </remarks>
        /// <response code="301">Moved Permanently.</response>
        /// <response code="302">Found.</response>
        /// <response code="303">See Other.</response>
        /// <response code="307">Temporary Redirect (method and body kept).</response>
        /// <response code="308">Permanent Redirect (method and body kept).</response>
        /// <response code="400">Unsupported code.</response>
        [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", Route = "status/{code:int}")]
        public IActionResult WithStatus(int code)
        {
            if (!RedirectCodes.Contains(code))
                return BadRequest(ApiResponse.Error(400, "Redirect status must be one of 301, 302, 303, 307, 308."));

            return RedirectTo(FinalTarget, code);
        }

        // -------------------- REDIRECT TO A LOCAL PATH --------------------
        /// <summary>Redirects to a local path on this server.</summary>
        /// <param name="url">Target path. It must start with a single <c>/</c> (e.g. <c>/api/echo?x=1</c>) and be at most 2048 characters.
        /// Absolute URLs, <c>//host</c>, <c>/\host</c> and control characters are rejected to prevent open redirects.</param>
        /// <param name="status">Redirect status: 301, 302, 303, 307 or 308. Default 302.</param>
        /// <remarks>Accepts GET, POST, PUT, PATCH, DELETE and HEAD.</remarks>
        /// <response code="302">Or the chosen redirect status, with <c>Location</c> set to <c>url</c>.</response>
        /// <response code="400">Missing or non-local <c>url</c>, or unsupported <c>status</c>.</response>
        [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", Route = "to")]
        public IActionResult To([FromQuery] string? url, [FromQuery] int status = 302)
        {
            // Local paths only: reject absolute URLs and protocol-relative tricks ("//evil", "/\evil") to prevent open redirects.
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith('/') || url.StartsWith("//") ||
                url.StartsWith("/\\") || url.Any(char.IsControl) || url.Length > 2048)
            {
                return BadRequest(ApiResponse.Error(400,
                    "Only local paths are allowed, e.g. ?url=/api/echo. Absolute URLs are rejected to prevent open redirects."));
            }

            if (!RedirectCodes.Contains(status))
                return BadRequest(ApiResponse.Error(400, "status must be one of 301, 302, 303, 307, 308."));

            return RedirectTo(url, status);
        }

        // -------------------- INFINITE LOOP (A -> B -> A ...) --------------------
        /// <summary>Starts an infinite redirect loop between <c>/loop</c> and <c>/loop/back</c>.</summary>
        /// <remarks>Use it to test a client's maximum-redirect limit.</remarks>
        /// <response code="302">Redirect to <c>/api/redirect/loop/back</c>.</response>
        [HttpGet("loop")]
        public IActionResult Loop() => RedirectTo("/api/redirect/loop/back", 302);

        /// <summary>Continues the infinite redirect loop back to <c>/loop</c>.</summary>
        /// <response code="302">Redirect to <c>/api/redirect/loop</c>.</response>
        [HttpGet("loop/back")]
        public IActionResult LoopBack() => RedirectTo("/api/redirect/loop", 302);

        private IActionResult RedirectTo(string location, int status, int? remaining = null)
        {
            Response.Headers.Location = location;
            return StatusCode(status, new
            {
                status,
                statusText = ReasonPhrases.GetReasonPhrase(status),
                location,
                remaining,
                keepsMethod = status is 307 or 308
            });
        }
    }
}
