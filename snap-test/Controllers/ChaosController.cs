using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// Failure injection for testing retries, timeouts and resilience: random failures, deterministic
    /// fail-N-times-then-succeed, slow/hanging responses, dropped connections, truncated bodies and huge headers.
    /// All waits end early if the client disconnects.
    /// </summary>
    [ApiController]
    [Route("api/chaos")]
    public class ChaosController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, int> Attempts = new();
        private static readonly int[] RetryableStatuses = { 429, 500, 502, 503, 504 };

        // -------------------- FLAKY (random failures) --------------------
        /// <summary>Fail at random with 503 to test retries.</summary>
        /// <param name="failRate">Probability of failure, 0 to 1 (default 0.5).</param>
        /// <remarks>Failures include <c>Retry-After: 1</c>. Available for GET and POST.</remarks>
        /// <response code="200">The request got through.</response>
        /// <response code="400"><paramref name="failRate"/> is outside 0-1.</response>
        /// <response code="503">Simulated random failure.</response>
        [HttpGet("flaky")]
        [HttpPost("flaky")]
        public IActionResult Flaky([FromQuery] double failRate = 0.5)
        {
            if (failRate < 0 || failRate > 1)
                return BadRequest(ApiResponse.Error(400, "failRate must be between 0 and 1."));

            if (Random.Shared.NextDouble() < failRate)
            {
                Response.Headers.RetryAfter = "1";
                return StatusCode(503, new
                {
                    status = 503,
                    error = "Service Unavailable",
                    message = $"Random failure (failRate={failRate}). Retry the request.",
                    failRate
                });
            }

            return Ok(new { message = "Success — the request got through this time.", failRate });
        }

        // -------------------- RETRY (fail N times, then succeed) --------------------
        /// <summary>Fail the first N attempts for a key, then succeed.</summary>
        /// <param name="key">Your own counter name (up to 64 characters). Each key counts attempts independently.</param>
        /// <param name="succeedAfter">Number of attempts that fail before success (0-20, default 3).</param>
        /// <param name="failStatus">Status used for failures: 429, 500, 502, 503 or 504 (default 503).</param>
        /// <remarks>
        /// A deterministic way to test retry logic: with <c>succeedAfter=3</c>, attempts 1-3 fail and attempt 4 succeeds.
        /// Every response has an <c>X-Attempt</c> header, and failures add <c>Retry-After: 1</c>. Reset the counter with
        /// <c>DELETE /api/chaos/retry/{key}</c>. Available for GET and POST.
        /// </remarks>
        /// <response code="200">This attempt succeeded.</response>
        /// <response code="400">Invalid key, succeedAfter or failStatus.</response>
        /// <response code="503">This attempt failed (or the chosen failStatus).</response>
        [HttpGet("retry/{key}")]
        [HttpPost("retry/{key}")]
        public IActionResult Retry(string key, [FromQuery] int succeedAfter = 3, [FromQuery] int failStatus = 503)
        {
            if (key.Length > 64)
                return BadRequest(ApiResponse.Error(400, "key must be 64 characters or fewer."));
            if (succeedAfter < 0 || succeedAfter > 20)
                return BadRequest(ApiResponse.Error(400, "succeedAfter must be between 0 and 20."));
            if (!RetryableStatuses.Contains(failStatus))
                return BadRequest(ApiResponse.Error(400, $"failStatus must be one of: {string.Join(", ", RetryableStatuses)}."));

            // Bounded: forget everything rather than grow without limit.
            if (Attempts.Count >= 1000 && !Attempts.ContainsKey(key)) Attempts.Clear();

            var attempt = Attempts.AddOrUpdate(key, 1, (_, n) => n + 1);
            Response.Headers["X-Attempt"] = attempt.ToString();

            if (attempt <= succeedAfter)
            {
                Response.Headers.RetryAfter = "1";
                return StatusCode(failStatus, new
                {
                    status = failStatus,
                    error = ReasonPhrases.GetReasonPhrase(failStatus),
                    message = $"Attempt {attempt} failed. The first {succeedAfter} attempt(s) fail; attempt {succeedAfter + 1} succeeds.",
                    attempt,
                    succeedAfter,
                    remainingFailures = succeedAfter - attempt
                });
            }

            return Ok(new { message = $"Succeeded on attempt {attempt}.", attempt, succeedAfter });
        }

        /// <summary>Reset the attempt counter for a key.</summary>
        /// <param name="key">The counter name to reset.</param>
        /// <response code="200">Counter reset (or it never existed).</response>
        [HttpDelete("retry/{key}")]
        public IActionResult ResetRetry(string key)
        {
            var existed = Attempts.TryRemove(key, out var attempts);
            return Ok(new { message = existed ? $"Counter for '{key}' reset after {attempts} attempt(s)." : $"No counter existed for '{key}'.", key });
        }

        // -------------------- SLOW (random wait in a range) --------------------
        /// <summary>Wait a random time within a range, then respond.</summary>
        /// <param name="minMs">Minimum wait in milliseconds (0-30000, default 1000).</param>
        /// <param name="maxMs">Maximum wait in milliseconds (0-30000, default 3000). If min is greater than max, they are swapped.</param>
        /// <response code="200">Response after the wait, reporting <c>waitedMs</c>.</response>
        [HttpGet("slow")]
        public async Task<IActionResult> Slow([FromQuery] int minMs = 1000, [FromQuery] int maxMs = 3000)
        {
            minMs = Math.Clamp(minMs, 0, 30_000);
            maxMs = Math.Clamp(maxMs, 0, 30_000);
            if (minMs > maxMs) (minMs, maxMs) = (maxMs, minMs);

            var waitMs = Random.Shared.Next(minMs, maxMs + 1);
            if (!await WaitAsync(waitMs)) return new EmptyResult();

            return Ok(new { message = $"Responded after {waitMs}ms.", waitedMs = waitMs, minMs, maxMs });
        }

        // -------------------- TIMEOUT (hang, then answer) --------------------
        /// <summary>Hang for a number of seconds, then respond.</summary>
        /// <param name="seconds">How long to hang (1-60, default 30).</param>
        /// <remarks>Use this to test client timeouts. The wait stops as soon as the client disconnects.</remarks>
        /// <response code="200">Response after the wait.</response>
        [HttpGet("timeout")]
        public async Task<IActionResult> Timeout([FromQuery] int seconds = 30)
        {
            seconds = Math.Clamp(seconds, 1, 60);
            if (!await WaitAsync(seconds * 1000)) return new EmptyResult();

            return Ok(new { message = $"Responded after {seconds}s — your client did not time out.", seconds });
        }

        // -------------------- RANDOM LATENCY (realistic p50/p90/p99 spread) --------------------
        /// <summary>Respond with a realistic latency spread.</summary>
        /// <remarks>About 90% of calls take 10-100ms, 9% take 500-1500ms and 1% take 3-5s.</remarks>
        /// <response code="200">Reports <c>latencyMs</c> and which bucket it came from.</response>
        [HttpGet("random-latency")]
        public async Task<IActionResult> RandomLatency()
        {
            var roll = Random.Shared.NextDouble();
            var (bucket, waitMs) = roll switch
            {
                < 0.90 => ("fast (p0-p90)", Random.Shared.Next(10, 100)),
                < 0.99 => ("medium (p90-p99)", Random.Shared.Next(500, 1500)),
                _ => ("slow (p99+)", Random.Shared.Next(3000, 5000))
            };

            if (!await WaitAsync(waitMs)) return new EmptyResult();
            return Ok(new { latencyMs = waitMs, bucket });
        }

        // -------------------- ABORT (drop the connection) --------------------
        /// <summary>Drop the connection without sending any response.</summary>
        /// <remarks>
        /// <b>Warning:</b> this endpoint deliberately drops the connection. Clients see a network error, not an HTTP status,
        /// and Swagger UI shows "Failed to fetch". Available for GET and POST.
        /// </remarks>
        /// <response code="200">Never sent: the connection is dropped instead.</response>
        [HttpGet("abort")]
        [HttpPost("abort")]
        public IActionResult Abort()
        {
            HttpContext.Abort();
            return new EmptyResult();
        }

        // -------------------- PARTIAL (truncated body, then drop) --------------------
        /// <summary>Send a truncated body, then drop the connection.</summary>
        /// <remarks>
        /// <b>Warning:</b> this endpoint deliberately drops the connection. It promises a 10KB body
        /// (<c>Content-Length: 10000</c>), sends about 70 bytes of incomplete JSON, then disconnects. Clients should report
        /// an incomplete or truncated response.
        /// </remarks>
        /// <response code="200">Headers are sent, but the body is incomplete and the connection is dropped.</response>
        [HttpGet("partial")]
        public async Task<IActionResult> Partial()
        {
            // Promise 10KB, deliver ~70 bytes of invalid JSON, then kill the connection.
            Response.ContentType = "application/json";
            Response.ContentLength = 10_000;

            try
            {
                await Response.WriteAsync("{\"message\":\"This response is cut off mid-stream\",\"items\":[1,2,3,", HttpContext.RequestAborted);
                await Response.Body.FlushAsync(HttpContext.RequestAborted);
                await Task.Delay(200, HttpContext.RequestAborted);
            }
            catch (Exception ex) when (ex is OperationCanceledException or IOException)
            {
            }

            HttpContext.Abort();
            return new EmptyResult();
        }

        // -------------------- HUGE HEADERS --------------------
        /// <summary>Respond with many large headers.</summary>
        /// <param name="count">Number of <c>X-Chaos-Header-NNN</c> headers (1-100, default 50).</param>
        /// <param name="size">Characters per header value (1-1000, default 200).</param>
        /// <remarks>
        /// Some clients reject responses with too many headers. For example, Python's http.client caps responses at
        /// 100 headers.
        /// </remarks>
        /// <response code="200">Response with the extra headers.</response>
        [HttpGet("huge-headers")]
        public IActionResult HugeHeaders([FromQuery] int count = 50, [FromQuery] int size = 200)
        {
            count = Math.Clamp(count, 1, 100);
            size = Math.Clamp(size, 1, 1000);

            var value = string.Concat(Enumerable.Repeat("abcdefghij", size / 10 + 1))[..size];
            for (var i = 1; i <= count; i++)
                Response.Headers[$"X-Chaos-Header-{i:000}"] = value;

            return Ok(new { message = $"Added {count} headers of {size} characters each.", headersAdded = count, approxHeaderBytes = count * (size + 20) });
        }

        private async Task<bool> WaitAsync(int ms)
        {
            try
            {
                await Task.Delay(ms, HttpContext.RequestAborted);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
    }
}
