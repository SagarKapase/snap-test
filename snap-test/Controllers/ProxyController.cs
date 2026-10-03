using Microsoft.AspNetCore.Mvc;
using snap_test.ApiExecution;
using snap_test.Helpers;
using System.Security.Cryptography;
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
        /// <summary>Header that must carry the proxy access key.</summary>
        public const string KeyHeader = "X-Proxy-Key";

        private readonly HttpClient _httpClient;
        private readonly byte[]? _accessKey;

        public ProxyController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            // Guarded client: refuses private, loopback, link-local and cloud-metadata addresses (see OutboundNetworkGuard).
            _httpClient = httpClientFactory.CreateClient(OutboundNetworkGuard.HttpClientName);

            // Without a key the proxy stays off, so a missing setting can never leave an open proxy on the internet.
            var key = config["Proxy:AccessKey"];
            _accessKey = string.IsNullOrEmpty(key) ? null : Encoding.UTF8.GetBytes(key);
        }

        /// <summary>Forward a request to an upstream URL and relay its response.</summary>
        /// <param name="data">endpointUrl (absolute http or https URL), methodName (GET, POST, ...) and an optional requestBody, sent as JSON except for GET and HEAD.</param>
        /// <remarks>Requires the <c>X-Proxy-Key</c> header to match the server's <c>Proxy:AccessKey</c> setting; the proxy is
        /// disabled when no key is configured. Returns 200 with the upstream status, statusText and raw body (as a string)
        /// whatever the upstream status was. Destinations on private, loopback, link-local or cloud-platform addresses
        /// (including redirects to them) are refused.</remarks>
        /// <response code="200">Upstream status, statusText and body.</response>
        /// <response code="400">endpointUrl isn't an absolute http(s) URL, or methodName is missing.</response>
        /// <response code="401">The X-Proxy-Key header is missing or wrong.</response>
        /// <response code="403">The destination resolves to a private or reserved network address.</response>
        /// <response code="502">The upstream request failed or timed out (30 s).</response>
        /// <response code="503">The proxy is disabled because the server has no Proxy:AccessKey configured.</response>
        [HttpPost("call")]
        public async Task<IActionResult> Execute([FromBody] ApiRequestData data)
        {
            if (_accessKey == null)
                return StatusCode(503, ApiResponse.Error(503, "The proxy is disabled on this server (no Proxy:AccessKey configured)."));

            var supplied = Request.Headers[KeyHeader].ToString();
            if (supplied.Length == 0 || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied), _accessKey))
                return Unauthorized(ApiResponse.Error(401, $"Missing or invalid {KeyHeader} header."));

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
