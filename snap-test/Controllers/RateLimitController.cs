using Microsoft.AspNetCore.Mvc;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// Rate limit: a real fixed-window limiter (5 requests per 60 seconds per client) for testing 429 and
    /// Retry-After handling. Clients are identified by the X-Client-Id header, or by IP without it.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RateLimitController : ControllerBase
    {
        private const int Limit = 5;
        private const int WindowSeconds = 60;
        private const int MaxClients = 10000;

        private class Window
        {
            public DateTime Start;
            public int Count;
        }

        private static readonly object Sync = new();
        private static readonly Dictionary<string, Window> Windows = new();

        // -------------------- LIMITED ENDPOINT (consumes one request) --------------------
        /// <summary>Make a rate-limited request that counts toward the limit.</summary>
        /// <remarks>
        /// Header (optional): <c>X-Client-Id: &lt;any id&gt;</c>; without it the remote IP is used.
        /// Responses carry <c>X-RateLimit-Limit</c>, <c>X-RateLimit-Remaining</c> and <c>X-RateLimit-Reset</c> (Unix seconds).
        /// The 6th request in a window gets 429 with <c>Retry-After</c> in seconds.
        /// </remarks>
        /// <response code="200">Request allowed.</response>
        /// <response code="429">Limit exceeded; retry after the Retry-After delay.</response>
        [HttpGet]
        public IActionResult Hit()
        {
            var clientId = ClientId();
            Window window;
            bool allowed;

            lock (Sync)
            {
                window = GetWindow(clientId);
                allowed = window.Count < Limit;
                if (allowed) window.Count++;
            }

            var resetAt = window.Start.AddSeconds(WindowSeconds);
            SetHeaders(window, resetAt);

            if (!allowed)
            {
                var retryAfter = Math.Max(1, (int)Math.Ceiling((resetAt - DateTime.UtcNow).TotalSeconds));
                Response.Headers.RetryAfter = retryAfter.ToString();
                return StatusCode(429, new
                {
                    status = 429,
                    error = "Too Many Requests",
                    message = $"Rate limit of {Limit} requests per {WindowSeconds}s exceeded. Retry in {retryAfter}s.",
                    retryAfter
                });
            }

            return Ok(new
            {
                message = "Request allowed.",
                clientId,
                used = window.Count,
                remaining = Limit - window.Count,
                limit = Limit,
                windowSeconds = WindowSeconds
            });
        }

        // -------------------- STATUS (does not consume) --------------------
        /// <summary>Show the caller's current window without counting a request.</summary>
        /// <remarks>Header (optional): <c>X-Client-Id</c>.</remarks>
        /// <response code="200">Used, remaining, limit and seconds until the window resets.</response>
        [HttpGet("status")]
        public IActionResult Status()
        {
            var clientId = ClientId();
            Window window;
            lock (Sync) window = GetWindow(clientId);

            var resetAt = window.Start.AddSeconds(WindowSeconds);
            SetHeaders(window, resetAt);

            return Ok(new
            {
                clientId,
                used = window.Count,
                remaining = Math.Max(0, Limit - window.Count),
                limit = Limit,
                resetsInSeconds = Math.Max(0, (int)Math.Ceiling((resetAt - DateTime.UtcNow).TotalSeconds))
            });
        }

        // -------------------- RESET (for the calling client) --------------------
        /// <summary>Reset the caller's window.</summary>
        /// <remarks>Header (optional): <c>X-Client-Id</c>.</remarks>
        /// <response code="200">Window reset.</response>
        [HttpPost("reset")]
        public IActionResult Reset()
        {
            var clientId = ClientId();
            lock (Sync) Windows.Remove(clientId);

            Response.Headers["X-RateLimit-Limit"] = Limit.ToString();
            Response.Headers["X-RateLimit-Remaining"] = Limit.ToString();
            return Ok(new { message = "Rate limit window reset.", clientId });
        }

        // -------------------- helpers --------------------
        private string ClientId()
        {
            var header = Request.Headers["X-Client-Id"].ToString().Trim();
            if (header != "") return "id:" + (header.Length > 100 ? header.Substring(0, 100) : header);
            return "ip:" + (HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        }

        // Caller must hold Sync.
        private static Window GetWindow(string clientId)
        {
            var now = DateTime.UtcNow;

            if (Windows.TryGetValue(clientId, out var window) && (now - window.Start).TotalSeconds < WindowSeconds)
                return window;

            if (Windows.Count >= MaxClients)
            {
                foreach (var k in Windows.Where(kv => (now - kv.Value.Start).TotalSeconds >= WindowSeconds).Select(kv => kv.Key).ToList())
                    Windows.Remove(k);
                if (Windows.Count >= MaxClients) Windows.Clear();
            }

            window = new Window { Start = now, Count = 0 };
            Windows[clientId] = window;
            return window;
        }

        private void SetHeaders(Window window, DateTime resetAt)
        {
            Response.Headers["X-RateLimit-Limit"] = Limit.ToString();
            Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, Limit - window.Count).ToString();
            Response.Headers["X-RateLimit-Reset"] = new DateTimeOffset(resetAt).ToUnixTimeSeconds().ToString();
        }
    }
}
