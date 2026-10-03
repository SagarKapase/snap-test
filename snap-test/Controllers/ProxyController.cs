using Microsoft.AspNetCore.Mvc;
using snap_test.ApiExecution;
using snap_test.Helpers;
using System.Text;

namespace snap_test.Controllers
{
    /// <summary>
    /// Proxy: forward an HTTP request to any URL and return the upstream status and body.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProxyController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        public ProxyController(IHttpClientFactory httpClientFactory)
        {
            // Guarded client: refuses private, loopback, link-local and cloud-metadata addresses (see OutboundNetworkGuard).
            _httpClient = httpClientFactory.CreateClient(OutboundNetworkGuard.HttpClientName);
        }

        /// <summary>Forward a request to an upstream URL and relay its response.</summary>
        /// <param name="data">endpointUrl (absolute http or https URL), methodName (GET, POST, ...) and an optional requestBody, sent as JSON except for GET and HEAD.</param>
        /// <remarks>Returns 200 with the upstream status, statusText and raw body (as a string) whatever the upstream status was.
        /// Destinations on private, loopback, link-local or cloud-platform addresses (including redirects to them) are refused.</remarks>
        /// <response code="200">Upstream status, statusText and body.</response>
        /// <response code="400">endpointUrl isn't an absolute http(s) URL, or methodName is missing.</response>
        /// <response code="403">The destination resolves to a private or reserved network address.</response>
        /// <response code="502">The upstream request failed or timed out (30 s).</response>
        [HttpPost("call")]
        public async Task<IActionResult> Execute([FromBody] ApiRequestData data)
        {
            if (!Uri.TryCreate(data.EndpointUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return BadRequest(ApiResponse.Error(400, "endpointUrl must be an absolute http(s) URL."));

            if (string.IsNullOrWhiteSpace(data.MethodName))
                return BadRequest(ApiResponse.Error(400, "methodName is required."));

            var method = new HttpMethod(data.MethodName.Trim().ToUpperInvariant());
            var httpRequest = new HttpRequestMessage(method, uri);

            if (method != HttpMethod.Get && method != HttpMethod.Head && !string.IsNullOrEmpty(data.RequestBody))
            {
                httpRequest.Content = new StringContent(data.RequestBody, Encoding.UTF8, "application/json");
            }

            try
            {
                var response = await _httpClient.SendAsync(httpRequest);
                // Relay the body as-is; upstream may return JSON, text, HTML or nothing at all.
                var rawBody = await response.Content.ReadAsStringAsync();

                return Ok(new
                {
                    Status = (int)response.StatusCode,
                    StatusText = response.ReasonPhrase,
                    body = rawBody
                });
            }
            catch (HttpRequestException ex) when (ex.InnerException is BlockedDestinationException blocked)
            {
                return StatusCode(403, ApiResponse.Error(403, blocked.Message));
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return StatusCode(502, ApiResponse.Error(502, $"Upstream request failed: {ex.Message}"));
            }
        }
    }
}
