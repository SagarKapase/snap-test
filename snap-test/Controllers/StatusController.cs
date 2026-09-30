using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// Returns any HTTP status code on demand, with the headers that status normally carries.
    /// </summary>
    [ApiController]
    [Route("api/status")]
    public class StatusController : ControllerBase
    {
        private const int MaxPoolEntries = 20;
        private const string DefaultPool = "200,201,400,404,500";

        // -------------------- LIST --------------------
        /// <summary>Lists every known HTTP status code with its reason phrase and category.</summary>
        /// <remarks>Codes below 200 are listed for reference but cannot be requested (<c>supported: false</c>).</remarks>
        /// <response code="200">All known status codes, sorted.</response>
        [HttpGet("list")]
        public IActionResult List() => Ok(Enumerable.Range(100, 500)
            .Where(c => !string.IsNullOrEmpty(ReasonPhrases.GetReasonPhrase(c)))
            .Select(c => new
            {
                code = c,
                statusText = ReasonPhrases.GetReasonPhrase(c),
                category = Category(c),
                supported = c >= 200,
                url = c >= 200 ? $"/api/status/{c}" : null
            }));

        // -------------------- RANDOM (weighted) --------------------
        // ?codes=200:3,500:1 -> 200 three times as likely as 500. Weight defaults to 1.
        /// <summary>Responds with a status code picked at random from a weighted pool.</summary>
        /// <param name="codes">Comma-separated <c>code[:weight]</c> entries, e.g. <c>200:3,500:1</c> (200 is three times as likely).
        /// Codes must be 200-599, weights 1-100 (default 1), at most 20 entries. Default pool: <c>200,201,400,404,500</c>.</param>
        /// <remarks>Accepts any verb. The <c>X-Status-Pool</c> response header shows the parsed pool.</remarks>
        /// <response code="200">Or whichever code was drawn; the body and headers match <c>/api/status/{code}</c>.</response>
        /// <response code="400">Invalid code, weight or pool size.</response>
        [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS", Route = "random")]
        public IActionResult RandomStatus([FromQuery] string? codes)
        {
            var entries = (string.IsNullOrWhiteSpace(codes) ? DefaultPool : codes)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (entries.Length > MaxPoolEntries)
                return BadRequest(ApiResponse.Error(400, $"At most {MaxPoolEntries} codes are allowed."));

            var pool = new List<(int code, int weight)>();
            foreach (var entry in entries)
            {
                var parts = entry.Split(':');
                if (!int.TryParse(parts[0], out var code) || code < 200 || code > 599)
                    return BadRequest(ApiResponse.Error(400, $"Invalid status code '{parts[0]}'. Use codes between 200 and 599."));

                var weight = 1;
                if (parts.Length > 1 && (!int.TryParse(parts[1], out weight) || weight < 1 || weight > 100))
                    return BadRequest(ApiResponse.Error(400, $"Invalid weight in '{entry}'. Weights must be 1-100."));

                pool.Add((code, weight));
            }

            var roll = Random.Shared.Next(pool.Sum(p => p.weight));
            var chosen = pool.First(p => (roll -= p.weight) < 0).code;

            Response.Headers["X-Status-Pool"] = string.Join(",", pool.Select(p => $"{p.code}:{p.weight}"));
            return Respond(chosen);
        }

        // -------------------- ANY STATUS CODE --------------------
        /// <summary>Responds with the requested HTTP status code.</summary>
        /// <param name="code">Status code to return, 200-599. Codes outside the IANA registry (e.g. 599) are allowed.</param>
        /// <remarks>
        /// Accepts any verb. 204, 205 and 304 have no body. The response carries the headers that status normally has:
        /// 3xx (except 304) <c>Location: /api/echo</c>; 401 <c>WWW-Authenticate</c>; 405 <c>Allow</c>; 407 <c>Proxy-Authenticate</c>;
        /// 416 <c>Content-Range</c>; 426 <c>Upgrade</c>; 429 and 503 <c>Retry-After: 5</c>.
        /// </remarks>
        /// <response code="200">Or the requested code, with a <c>{ status, statusText, category, message }</c> body.</response>
        /// <response code="400">Code outside 200-599 (1xx cannot be sent as a final response).</response>
        [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS", Route = "{code:int}")]
        public IActionResult Status(int code)
        {
            if (code < 200 || code > 599)
                return BadRequest(ApiResponse.Error(400, "Status code must be between 200 and 599 (1xx cannot be sent as a final response)."));

            return Respond(code);
        }

        private IActionResult Respond(int code)
        {
            switch (code)
            {
                case 401:
                    Response.Headers.WWWAuthenticate = "Basic realm=\"APIBee\"";
                    break;
                case 405:
                    Response.Headers.Allow = "GET, POST";
                    break;
                case 407:
                    Response.Headers.ProxyAuthenticate = "Basic realm=\"APIBee Proxy\"";
                    break;
                case 416:
                    Response.Headers.ContentRange = "bytes */0";
                    break;
                case 426:
                    Response.Headers.Upgrade = "HTTP/2";
                    break;
                case 429:
                case 503:
                    Response.Headers.RetryAfter = "5";
                    break;
                case >= 300 and < 400 and not 304:
                    Response.Headers.Location = "/api/echo";
                    break;
            }

            // These statuses must not carry a body.
            if (code is 204 or 205 or 304)
                return StatusCode(code);

            var statusText = ReasonPhrases.GetReasonPhrase(code);
            return StatusCode(code, new
            {
                status = code,
                statusText = string.IsNullOrEmpty(statusText) ? "Unknown Status" : statusText,
                category = Category(code),
                message = $"This is a {code} response. Use /api/status/{{code}} to request any status."
            });
        }

        private static string Category(int code) => (code / 100) switch
        {
            1 => "informational",
            2 => "success",
            3 => "redirection",
            4 => "client-error",
            _ => "server-error"
        };
    }
}
