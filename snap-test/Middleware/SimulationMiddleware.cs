using Microsoft.AspNetCore.WebUtilities;

namespace snap_test.Middleware
{
    /// <summary>
    /// Cross-cutting ?delay= and ?error= simulation (PRD Phase 5). Runs before controllers so it
    /// applies to ANY endpoint. ?delay=N waits N seconds (max 10); ?error=CODE short-circuits with a
    /// simulated error body and an X-Simulated: true header. They combine: delay happens first.
    /// </summary>
    public class SimulationMiddleware
    {
        private readonly RequestDelegate _next;

        private static readonly HashSet<int> Supported = new()
        {
            400, 401, 403, 404, 408, 429, 500, 502, 503
        };

        public SimulationMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var query = context.Request.Query;

            if (query.TryGetValue("delay", out var delayVal) &&
                int.TryParse(delayVal, out var seconds) && seconds > 0)
            {
                seconds = Math.Min(seconds, 10); // cap at 10s to prevent abuse
                await Task.Delay(seconds * 1000);
            }

            if (query.TryGetValue("error", out var errorVal) &&
                int.TryParse(errorVal, out var code) && Supported.Contains(code))
            {
                context.Response.StatusCode = code;
                context.Response.Headers["X-Simulated"] = "true";
                await context.Response.WriteAsJsonAsync(new
                {
                    status = code,
                    error = ReasonPhrases.GetReasonPhrase(code),
                    message = "Simulated error. Use ?error={code} to test different status codes.",
                    simulated = true
                });
                return;
            }

            await _next(context);
        }
    }
}
