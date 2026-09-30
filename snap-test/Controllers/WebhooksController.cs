using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// Webhook testing:
    ///   * Bins — create a bin, point a webhook sender at /api/webhooks/{binId}, then inspect what arrived.
    ///     Capped at 100 bins (oldest evicted), 50 requests per bin (newest kept) and 256KB per captured body.
    ///   * Samples — hardcoded Stripe / GitHub / Shopify event payloads with valid signature headers, for
    ///     testing signature verification. This controller never makes outbound HTTP calls.
    /// </summary>
    [ApiController]
    [Route("api/webhooks")]
    public class WebhooksController : ControllerBase
    {
        private const int MaxBins = 100;
        private const int MaxRequestsPerBin = 50;
        private const int MaxBodyBytes = 256 * 1024;
        private const string SigningSecret = "whsec_apibee_test_secret";
        private const long SampleTimestamp = 1721300000;

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
        private static readonly object Gate = new();
        private static readonly List<Bin> Bins = new(); // oldest first

        /// <summary>Body for creating a webhook bin.</summary>
        public class CreateBinRequest
        {
            /// <summary>Optional display name (up to 100 characters). Defaults to <c>bin-xxxxxx</c>.</summary>
            public string? Name { get; set; }
        }

        private class Bin
        {
            public string Id { get; init; } = string.Empty;
            public string Name { get; init; } = string.Empty;
            public DateTime CreatedAt { get; init; }
            public List<object> Requests { get; } = new(); // newest first
        }

        // -------------------- CREATE BIN --------------------
        /// <summary>Create a webhook bin that captures incoming requests.</summary>
        /// <param name="request">Optional name for the bin.</param>
        /// <remarks>
        /// Point a webhook sender at the returned <c>url</c>, then read what arrived at <c>inspectUrl</c>. Up to 100 bins
        /// are kept; when the limit is reached, the oldest bin is evicted.
        /// </remarks>
        /// <response code="201">Bin created; the response includes its capture URL and inspect URL.</response>
        /// <response code="400">Name too long.</response>
        [HttpPost("bins")]
        public IActionResult CreateBin([FromBody] CreateBinRequest? request)
        {
            var id = Guid.NewGuid().ToString("N")[..12];
            var name = string.IsNullOrWhiteSpace(request?.Name) ? $"bin-{id[..6]}" : request!.Name!.Trim();
            if (name.Length > 100)
                return BadRequest(ApiResponse.Error(400, "name must be 100 characters or fewer."));

            var bin = new Bin { Id = id, Name = name, CreatedAt = DateTime.UtcNow };

            lock (Gate)
            {
                Bins.Add(bin);
                if (Bins.Count > MaxBins) Bins.RemoveAt(0);
            }

            return StatusCode(201, new { message = "Webhook bin created", data = Summary(bin) });
        }

        // -------------------- LIST BINS --------------------
        /// <summary>List webhook bins, newest first.</summary>
        /// <response code="200">Array of bin summaries.</response>
        [HttpGet("bins")]
        public IActionResult ListBins()
        {
            lock (Gate) return Ok(Bins.AsEnumerable().Reverse().Select(Summary).ToList());
        }

        // -------------------- INSPECT BIN --------------------
        /// <summary>Inspect a bin and the requests it captured.</summary>
        /// <param name="id">Bin id.</param>
        /// <remarks>Shows up to 50 requests, newest first. Each includes method, path, query, headers, body and client IP.</remarks>
        /// <response code="200">The bin and its captured requests.</response>
        /// <response code="404">No bin with that id.</response>
        [HttpGet("bins/{id}")]
        public IActionResult GetBin(string id)
        {
            lock (Gate)
            {
                var bin = Bins.FirstOrDefault(b => b.Id == id);
                if (bin == null) return BinNotFound(id);

                return Ok(new
                {
                    id = bin.Id,
                    name = bin.Name,
                    createdAt = Iso(bin.CreatedAt),
                    url = AbsoluteUrl($"/api/webhooks/{bin.Id}"),
                    requestCount = bin.Requests.Count,
                    requests = bin.Requests.ToList()
                });
            }
        }

        // -------------------- DELETE BIN / CLEAR REQUESTS --------------------
        /// <summary>Delete a bin.</summary>
        /// <param name="id">Bin id.</param>
        /// <response code="200">Bin deleted.</response>
        /// <response code="404">No bin with that id.</response>
        [HttpDelete("bins/{id}")]
        public IActionResult DeleteBin(string id)
        {
            lock (Gate)
            {
                if (Bins.RemoveAll(b => b.Id == id) == 0) return BinNotFound(id);
            }
            return Ok(new { message = "Webhook bin deleted" });
        }

        /// <summary>Clear a bin's captured requests.</summary>
        /// <param name="id">Bin id.</param>
        /// <response code="200">Requests cleared.</response>
        /// <response code="404">No bin with that id.</response>
        [HttpDelete("bins/{id}/requests")]
        public IActionResult ClearBin(string id)
        {
            lock (Gate)
            {
                var bin = Bins.FirstOrDefault(b => b.Id == id);
                if (bin == null) return BinNotFound(id);

                var cleared = bin.Requests.Count;
                bin.Requests.Clear();
                return Ok(new { message = $"Cleared {cleared} captured request(s)" });
            }
        }

        // -------------------- CAPTURE (any verb, optional sub-path) --------------------
        /// <summary>Capture any request sent to a bin.</summary>
        /// <param name="binId">Bin id from <c>POST /api/webhooks/bins</c>.</param>
        /// <param name="rest">Optional extra path after the bin id, e.g. <c>orders/created</c>. It is recorded as <c>subPath</c>.</param>
        /// <param name="status">Status code to reply with (200-599, default 200). 204 and 304 reply without a body.</param>
        /// <remarks>
        /// Accepts GET, POST, PUT, PATCH, DELETE, HEAD and OPTIONS. Text bodies are stored as UTF-8; other bodies are
        /// stored as base64. Bodies over 256KB are truncated (<c>bodyTruncated: true</c>). No outbound calls are ever made.
        /// </remarks>
        /// <response code="200">Request captured (or the status chosen with <paramref name="status"/>).</response>
        /// <response code="400"><paramref name="status"/> is out of range.</response>
        /// <response code="404">No bin with that id.</response>
        [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS", Route = "{binId}")]
        [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS", Route = "{binId}/{**rest}")]
        public async Task<IActionResult> Capture(string binId, string? rest, [FromQuery] int status = 200)
        {
            if (status < 200 || status > 599)
                return BadRequest(ApiResponse.Error(400, "status must be between 200 and 599."));

            lock (Gate)
            {
                if (!Bins.Any(b => b.Id == binId)) return BinNotFound(binId);
            }

            var (body, size, truncated, encoding) = await ReadBodyAsync();
            var requestId = $"req_{Guid.NewGuid():N}"[..16];

            var captured = new
            {
                id = requestId,
                method = Request.Method,
                path = Request.Path.Value,
                subPath = rest,
                query = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString()),
                headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                contentType = Request.ContentType,
                bodySize = size,
                bodyEncoding = encoding,
                bodyTruncated = truncated,
                body,
                ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
                receivedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            lock (Gate)
            {
                var bin = Bins.FirstOrDefault(b => b.Id == binId);
                if (bin == null) return BinNotFound(binId); // deleted while we were reading

                bin.Requests.Insert(0, captured);
                if (bin.Requests.Count > MaxRequestsPerBin) bin.Requests.RemoveAt(bin.Requests.Count - 1);
            }

            if (status is 204 or 304) return StatusCode(status);
            return StatusCode(status, new { received = true, binId, requestId, message = "Request captured" });
        }

        // -------------------- SAMPLE EVENTS --------------------
        /// <summary>List the sample signed webhook events.</summary>
        /// <remarks>All samples are signed with the secret <c>whsec_apibee_test_secret</c>.</remarks>
        /// <response code="200">The signing secret and the list of available samples.</response>
        [HttpGet("samples")]
        public IActionResult Samples() => Ok(new
        {
            signingSecret = SigningSecret,
            note = "Each sample returns the exact signed payload as the body, with the provider's signature headers. ?format=envelope returns everything as JSON instead.",
            samples = new[]
            {
                new { provider = "stripe", eventType = "payment_intent.succeeded", signatureHeader = "Stripe-Signature", url = "/api/webhooks/samples/stripe" },
                new { provider = "github", eventType = "push", signatureHeader = "X-Hub-Signature-256", url = "/api/webhooks/samples/github" },
                new { provider = "shopify", eventType = "orders/create", signatureHeader = "X-Shopify-Hmac-Sha256", url = "/api/webhooks/samples/shopify" }
            }
        });

        /// <summary>Get a signed sample event for testing signature verification.</summary>
        /// <param name="provider"><c>stripe</c>, <c>github</c> or <c>shopify</c> (case-insensitive).</param>
        /// <param name="format">Omit to receive the raw signed body with the provider's signature headers. Use <c>envelope</c> to receive body, headers and secret as JSON.</param>
        /// <remarks>
        /// Signature schemes:
        /// <list type="bullet">
        /// <item><description>Stripe: <c>Stripe-Signature: t=..,v1=hex(HMAC-SHA256(secret, "t.body"))</c></description></item>
        /// <item><description>GitHub: <c>X-Hub-Signature-256: sha256=hex(HMAC-SHA256(secret, body))</c></description></item>
        /// <item><description>Shopify: <c>X-Shopify-Hmac-Sha256: base64(HMAC-SHA256(secret, body))</c></description></item>
        /// </list>
        /// Payloads and signatures are deterministic.
        /// </remarks>
        /// <response code="200">The signed sample.</response>
        /// <response code="404">Unknown provider.</response>
        [HttpGet("samples/{provider}")]
        public IActionResult Sample(string provider, [FromQuery] string? format = null)
        {
            var sample = BuildSample(provider.ToLowerInvariant());
            if (sample == null)
                return NotFound(ApiResponse.Error(404, $"No sample for '{provider}'. Available: stripe, github, shopify."));

            var (payload, headers, verification) = sample.Value;

            if (string.Equals(format, "envelope", StringComparison.OrdinalIgnoreCase))
                return Ok(new { provider, secret = SigningSecret, headers, body = payload, verification });

            foreach (var (name, value) in headers) Response.Headers[name] = value;
            return Content(payload, "application/json", Encoding.UTF8);
        }

        private static (string payload, Dictionary<string, string> headers, string verification)? BuildSample(string provider)
        {
            switch (provider)
            {
                case "stripe":
                {
                    var payload = JsonSerializer.Serialize(new
                    {
                        id = "evt_1PqAbC2eZvKYlo2C",
                        @object = "event",
                        api_version = "2024-06-20",
                        created = SampleTimestamp,
                        type = "payment_intent.succeeded",
                        livemode = false,
                        data = new
                        {
                            @object = new
                            {
                                id = "pi_3PqAbC2eZvKYlo2C0x1y2z3a",
                                @object = "payment_intent",
                                amount = 2000,
                                currency = "usd",
                                status = "succeeded",
                                customer = "cus_QhA1b2C3d4E5f6",
                                receipt_email = "ava.thompson@example.com",
                                metadata = new { order_id = "1001" }
                            }
                        }
                    }, Json);

                    var signature = HexHmac($"{SampleTimestamp}.{payload}");
                    return (payload,
                        new Dictionary<string, string> { ["Stripe-Signature"] = $"t={SampleTimestamp},v1={signature}" },
                        "v1 = hex(HMAC-SHA256(secret, \"{t}.{body}\"))");
                }

                case "github":
                {
                    var payload = JsonSerializer.Serialize(new
                    {
                        @ref = "refs/heads/main",
                        before = "6113728f27ae82c7b1a177c8d03f9e96e0adf246",
                        after = "76ae82c7b1a177c8d03f9e96e0adf2466113728f",
                        repository = new { id = 186853002, name = "apibee", full_name = "apibee-dev/apibee", @private = false },
                        pusher = new { name = "ava-thompson", email = "ava.thompson@example.com" },
                        commits = new[]
                        {
                            new
                            {
                                id = "76ae82c7b1a177c8d03f9e96e0adf2466113728f",
                                message = "Add webhook samples",
                                timestamp = "2024-07-18T10:53:20Z",
                                author = new { name = "Ava Thompson", email = "ava.thompson@example.com" }
                            }
                        }
                    }, Json);

                    return (payload,
                        new Dictionary<string, string>
                        {
                            ["X-GitHub-Event"] = "push",
                            ["X-GitHub-Delivery"] = "72d3162e-cc78-11e3-81ab-4c9367dc0958",
                            ["X-Hub-Signature-256"] = $"sha256={HexHmac(payload)}"
                        },
                        "sha256= + hex(HMAC-SHA256(secret, body))");
                }

                case "shopify":
                {
                    var payload = JsonSerializer.Serialize(new
                    {
                        id = 820982911946154508L,
                        email = "ava.thompson@example.com",
                        created_at = "2024-07-18T10:53:20-04:00",
                        currency = "USD",
                        total_price = "199.00",
                        financial_status = "paid",
                        line_items = new[]
                        {
                            new { id = 466157049, title = "Wireless Noise-Cancelling Headphones", quantity = 1, price = "149.00", sku = "SKU-0001" },
                            new { id = 518995019, title = "Cotton Crew T-Shirt", quantity = 2, price = "25.00", sku = "SKU-0006" }
                        }
                    }, Json);

                    var hmac = Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(SigningSecret), Encoding.UTF8.GetBytes(payload)));
                    return (payload,
                        new Dictionary<string, string>
                        {
                            ["X-Shopify-Topic"] = "orders/create",
                            ["X-Shopify-Shop-Domain"] = "apibee-demo.myshopify.com",
                            ["X-Shopify-Hmac-Sha256"] = hmac
                        },
                        "base64(HMAC-SHA256(secret, body))");
                }

                default:
                    return null;
            }
        }

        private async Task<(string? body, long size, bool truncated, string encoding)> ReadBodyAsync()
        {
            using var captured = new MemoryStream();
            var chunk = new byte[8192];
            long total = 0;
            int read;

            while ((read = await Request.Body.ReadAsync(chunk, HttpContext.RequestAborted)) > 0)
            {
                total += read;
                var room = MaxBodyBytes - (int)captured.Length;
                if (room > 0) captured.Write(chunk, 0, Math.Min(read, room));
            }

            if (total == 0) return (null, 0, false, "none");

            var type = Request.ContentType ?? string.Empty;
            var isText = type.Length == 0 || type.StartsWith("text/") || type.Contains("json") || type.Contains("xml") ||
                         type.Contains("x-www-form-urlencoded") || type.Contains("javascript");

            return isText
                ? (Encoding.UTF8.GetString(captured.ToArray()), total, total > MaxBodyBytes, "utf-8")
                : (Convert.ToBase64String(captured.ToArray()), total, total > MaxBodyBytes, "base64");
        }

        private object Summary(Bin bin) => new
        {
            id = bin.Id,
            name = bin.Name,
            createdAt = Iso(bin.CreatedAt),
            requestCount = bin.Requests.Count,
            url = AbsoluteUrl($"/api/webhooks/{bin.Id}"),
            inspectUrl = AbsoluteUrl($"/api/webhooks/bins/{bin.Id}")
        };

        private IActionResult BinNotFound(string id) =>
            NotFound(ApiResponse.Error(404, $"Webhook bin '{id}' does not exist. Create one with POST /api/webhooks/bins."));

        private string AbsoluteUrl(string path) => $"{Request.Scheme}://{Request.Host}{Request.PathBase}{path}";

        private static string HexHmac(string data) =>
            Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(SigningSecret), Encoding.UTF8.GetBytes(data))).ToLowerInvariant();

        private static string Iso(DateTime value) => value.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
}
