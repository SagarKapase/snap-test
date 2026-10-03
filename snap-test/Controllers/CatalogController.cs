using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;

namespace snap_test.Controllers
{
    /// <summary>Discovery and health checks: a live catalog of every endpoint plus liveness and readiness probes.</summary>
    [ApiController]
    [Route("api")]
    public class CatalogController : ControllerBase
    {
        private static readonly DateTime StartedAt = DateTime.UtcNow;
        private readonly EndpointDataSource _endpoints;

        public CatalogController(EndpointDataSource endpoints) => _endpoints = endpoints;

        // -------------------- GET /api (every route, grouped by controller) --------------------
        /// <summary>List every API endpoint, grouped by controller.</summary>
        /// <remarks>Built from the live route table, so it always matches the running build. Also points to the GraphQL and WebSocket endpoints.</remarks>
        /// <response code="200">The endpoint catalog.</response>
        [HttpGet]
        public IActionResult Catalog()
        {
            var routes = _endpoints.Endpoints
                .OfType<RouteEndpoint>()
                .Select(e => new
                {
                    Endpoint = e,
                    Action = e.Metadata.GetMetadata<ControllerActionDescriptor>(),
                    Methods = e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? new[] { "ANY" }
                })
                .Where(x => x.Action != null)
                .GroupBy(x => x.Action!.ControllerName)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    group = g.Key,
                    endpoints = g
                        .SelectMany(x => x.Methods.Select(m => new { method = m, path = "/" + x.Endpoint.RoutePattern.RawText?.TrimStart('/') }))
                        .Distinct()
                        .OrderBy(x => x.path).ThenBy(x => x.method)
                        .ToList()
                })
                .ToList();

            return Ok(new
            {
                name = "TestingAPIs",
                description = "Hardcoded dummy APIs for testing HTTP clients. Add ?delay=N or ?error=CODE to any request.",
                graphql = "/graphql",
                websockets = new[] { "/ws/echo", "/ws/ticker" },
                totalEndpoints = routes.Sum(r => r.endpoints.Count),
                groups = routes
            });
        }

        // -------------------- HEALTH --------------------
        /// <summary>Check that the API is up (HEAD also supported).</summary>
        /// <response code="200">Status, uptime in seconds and the current time.</response>
        [HttpGet("health")]
        [HttpHead("health")]
        public IActionResult Health() => Ok(new
        {
            status = "healthy",
            uptimeSeconds = (long)(DateTime.UtcNow - StartedAt).TotalSeconds,
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        });

        /// <summary>Report readiness with a fixed set of dependency checks.</summary>
        /// <response code="200">Ready, with per-dependency status.</response>
        [HttpGet("health/ready")]
        public IActionResult Ready() => Ok(new
        {
            status = "ready",
            checks = new[]
            {
                new { name = "memory-store", status = "up", responseTimeMs = 1 },
                new { name = "graphql", status = "up", responseTimeMs = 3 },
                new { name = "websockets", status = "up", responseTimeMs = 2 }
            }
        });

        /// <summary>Report liveness.</summary>
        /// <response code="200">Always { status: "alive" }.</response>
        [HttpGet("health/live")]
        public IActionResult Live() => Ok(new { status = "alive" });
    }
}
