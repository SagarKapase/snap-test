using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using snap_test.ApiExecution;
using System.Text;

namespace snap_test.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProxyController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        public ProxyController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpPost("call")]
        public async Task<IActionResult> Execute([FromBody] ApiRequestData data)
        {
            var httpRequest = new HttpRequestMessage()
            {
                Method = new HttpMethod(data.MethodName),
                RequestUri = new Uri(data.EndpointUrl)
            };

            if (data.MethodName != "GET" && data.RequestBody != null)
            {
                httpRequest.Content = new StringContent(data.RequestBody, Encoding.UTF8, "application/json");
            }
           
            var response = await _httpClient.SendAsync(httpRequest);
            var rawBody = await response.Content.ReadAsStringAsync();

            // Auto detect JSON object or array
            JToken parsed = JToken.Parse(rawBody);

            // Return actual JSON object (not string)
            return Ok(new
            {
                Status = (int)response.StatusCode,
                StatusText = response.ReasonPhrase,
                body = rawBody
            });
        }
    }
}
