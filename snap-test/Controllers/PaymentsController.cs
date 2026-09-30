using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;
using snap_test.Models;

namespace snap_test.Controllers
{
    /// <summary>
    /// Payments: hardcoded payments plus an Idempotency-Key demo on create (replay, 422 for a different body,
    /// 409 while in flight) and refunds. Card ending 0002 is always declined.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private const int MaxKeys = 1000;
        private static readonly TimeSpan KeyLifetime = TimeSpan.FromHours(24);
        private static readonly HashSet<string> Methods = new() { "card", "upi", "paypal", "bank_transfer" };
        private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

        private class IdempotencyEntry
        {
            public string BodyHash = "";
            public bool Completed;
            public int StatusCode;
            public object? Response;
            public DateTime CreatedAt;
        }

        private static readonly object Sync = new();
        private static readonly Dictionary<string, IdempotencyEntry> Keys = new();

        // -------------------- GET ALL (pagination / sort / filter / search) --------------------
        /// <summary>List payments with filtering, search, sorting and pagination.</summary>
        /// <remarks>
        /// Filter on any field (for example <c>?status=refunded</c>, <c>?method=upi</c>, <c>?userId=101</c>), search with <c>?q=</c>,
        /// sort with <c>?sort=amount&amp;order=desc</c>, and page with <c>?limit=</c> plus <c>?page=</c> or <c>?offset=</c>.
        /// Totals are in the X-Total-Count, X-Page, X-Per-Page and X-Total-Pages headers.
        /// </remarks>
        /// <response code="200">Matching payments.</response>
        [HttpGet]
        public IActionResult GetAll()
        {
            lock (PaymentStore.Payments)
            {
                var (items, total, page, perPage, totalPages) = QueryHelper.Apply(PaymentStore.Payments, Request.Query);

                Response.Headers["X-Total-Count"] = total.ToString();
                Response.Headers["X-Page"] = page.ToString();
                Response.Headers["X-Per-Page"] = perPage.ToString();
                Response.Headers["X-Total-Pages"] = totalPages.ToString();

                return Ok(items);
            }
        }

        // -------------------- GET BY ID --------------------
        /// <summary>Get a payment by ID.</summary>
        /// <param name="id">Payment ID (1-12 for the seed payments).</param>
        /// <response code="200">The payment.</response>
        /// <response code="404">No payment with that ID.</response>
        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            lock (PaymentStore.Payments)
            {
                var payment = PaymentStore.Payments.FirstOrDefault(p => p.Id == id);
                return payment == null
                    ? NotFound(ApiResponse.Error(404, $"Payment with ID {id} does not exist."))
                    : Ok(payment);
            }
        }

        private static byte[] CanonicalJson(JsonElement element)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
                WriteCanonical(writer, element);
            return stream.ToArray();
        }

        private static void WriteCanonical(Utf8JsonWriter writer, JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    foreach (var prop in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                    {
                        writer.WritePropertyName(prop.Name);
                        WriteCanonical(writer, prop.Value);
                    }
                    writer.WriteEndObject();
                    break;
                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var item in element.EnumerateArray())
                        WriteCanonical(writer, item);
                    writer.WriteEndArray();
                    break;
                default:
                    element.WriteTo(writer);
                    break;
            }
        }

        // -------------------- CREATE (requires Idempotency-Key) --------------------
        /// <summary>Create a payment. Requires an Idempotency-Key header.</summary>
        /// <param name="body">userId, orderId (optional), amount (greater than 0, max 1,000,000), currency (3-letter code), method (card, upi, paypal or bank_transfer), description, cardLast4 (4 digits, required for card).</param>
        /// <param name="processingSeconds">Hold the request open for this many seconds (max 5) so a duplicate can be sent while it is in flight.</param>
        /// <remarks>
        /// Header: <c>Idempotency-Key: &lt;unique value, e.g. a UUID&gt;</c> (max 255 characters, remembered for 24 hours).
        /// <para>Same key and same body: the original response is replayed with <c>Idempotent-Replayed: true</c>. Bodies are compared
        /// after canonicalizing, so key order and whitespace don't matter.</para>
        /// <para>Same key and a different body: 422. Same key while the first request is still processing: 409.
        /// A request that fails validation isn't remembered, so its key can be reused.</para>
        /// <para>Test data: <c>cardLast4</c> 0002 is always declined (status failed, still 201). bank_transfer payments start as pending.</para>
        /// </remarks>
        /// <response code="201">Payment created, or the original response replayed.</response>
        /// <response code="400">Idempotency-Key missing or too long, or the body is invalid.</response>
        /// <response code="409">A request with the same key is still being processed.</response>
        /// <response code="422">The key was already used with a different body.</response>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JsonElement body, [FromQuery] int processingSeconds = 0)
        {
            var key = Request.Headers["Idempotency-Key"].ToString().Trim();
            if (key == "")
                return BadRequest(ApiResponse.Error(400, "Missing Idempotency-Key header. Send a unique value (e.g. a UUID) per logical payment."));
            if (key.Length > 255)
                return BadRequest(ApiResponse.Error(400, "Idempotency-Key must be at most 255 characters."));

            // Hash a canonical form (keys sorted, whitespace dropped) so key order / formatting don't count as "different".
            var bodyHash = Convert.ToHexString(SHA256.HashData(CanonicalJson(body)));

            lock (Sync)
            {
                PruneKeys();

                if (Keys.TryGetValue(key, out var existing))
                {
                    if (!existing.Completed)
                        return Conflict(ApiResponse.Error(409, "A request with this Idempotency-Key is still being processed. Retry shortly."));

                    if (existing.BodyHash != bodyHash)
                        return UnprocessableEntity(ApiResponse.Error(422, "This Idempotency-Key was already used with a different request body."));

                    Response.Headers["Idempotent-Replayed"] = "true";
                    return StatusCode(existing.StatusCode, existing.Response);
                }

                Keys[key] = new IdempotencyEntry { BodyHash = bodyHash, CreatedAt = DateTime.UtcNow };
            }

            if (processingSeconds > 0)
                await Task.Delay(Math.Min(processingSeconds, 5) * 1000);

            CreatePaymentRequest? request = null;
            string? validationError;
            try
            {
                request = body.Deserialize<CreatePaymentRequest>(WebJson);
                validationError = Validate(request);
            }
            catch (JsonException ex)
            {
                validationError = $"Invalid payment body: {ex.Message}";
            }

            if (validationError != null)
            {
                // Validation failures are not remembered, so the client can fix the body and reuse the key.
                lock (Sync) Keys.Remove(key);
                return BadRequest(ApiResponse.Error(400, validationError));
            }

            var payment = new Payment
            {
                UserId = request!.UserId,
                OrderId = request.OrderId,
                Amount = request.Amount,
                Currency = request.Currency.ToUpperInvariant(),
                Method = request.Method,
                Description = request.Description,
                CardLast4 = request.CardLast4,
                Status = request.Method == "bank_transfer" ? "pending"
                       : request.CardLast4 == "0002" ? "failed"
                       : "succeeded",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            lock (PaymentStore.Payments)
            {
                payment.Id = PaymentStore.Payments.Count == 0 ? 1 : PaymentStore.Payments.Max(p => p.Id) + 1;
                PaymentStore.Payments.Add(payment);
            }

            var response = new
            {
                message = payment.Status == "failed" ? "Payment declined" : "Payment created successfully",
                idempotencyKey = key,
                data = payment
            };

            lock (Sync)
            {
                if (Keys.TryGetValue(key, out var entry))
                {
                    entry.Completed = true;
                    entry.StatusCode = 201;
                    entry.Response = response;
                }
            }

            return StatusCode(201, response);
        }

        // -------------------- REFUND --------------------
        /// <summary>Refund a succeeded payment.</summary>
        /// <param name="id">Payment ID.</param>
        /// <response code="200">Payment refunded.</response>
        /// <response code="404">No payment with that ID.</response>
        /// <response code="409">Already refunded, or not in succeeded status.</response>
        [HttpPost("{id:int}/refund")]
        public IActionResult Refund(int id)
        {
            lock (PaymentStore.Payments)
            {
                var payment = PaymentStore.Payments.FirstOrDefault(p => p.Id == id);
                if (payment == null)
                    return NotFound(ApiResponse.Error(404, $"Payment with ID {id} does not exist."));

                if (payment.Status == "refunded")
                    return Conflict(ApiResponse.Error(409, $"Payment {id} was already refunded at {payment.RefundedAt}."));

                if (payment.Status != "succeeded")
                    return Conflict(ApiResponse.Error(409, $"Only succeeded payments can be refunded (payment {id} is '{payment.Status}')."));

                payment.Status = "refunded";
                payment.RefundedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                return Ok(new { message = "Payment refunded successfully", data = payment });
            }
        }

        // -------------------- helpers --------------------
        private static string? Validate(CreatePaymentRequest? r)
        {
            if (r == null) return "Body must be a JSON object.";
            if (r.Amount <= 0) return "amount must be greater than 0.";
            if (r.Amount > 1_000_000) return "amount must not exceed 1,000,000.";
            if (r.Currency.Length != 3 || !r.Currency.All(char.IsLetter)) return "currency must be a 3-letter ISO code, e.g. USD.";
            if (!Methods.Contains(r.Method)) return $"method must be one of: {string.Join(", ", Methods)}.";
            if (r.Method == "card" && (r.CardLast4 == null || r.CardLast4.Length != 4 || !r.CardLast4.All(char.IsDigit)))
                return "cardLast4 (4 digits) is required for card payments.";
            return null;
        }

        // Caller must hold Sync.
        private static void PruneKeys()
        {
            var cutoff = DateTime.UtcNow - KeyLifetime;
            foreach (var k in Keys.Where(kv => kv.Value.CreatedAt < cutoff).Select(kv => kv.Key).ToList())
                Keys.Remove(k);

            if (Keys.Count >= MaxKeys)
                foreach (var k in Keys.Where(kv => kv.Value.Completed).OrderBy(kv => kv.Value.CreatedAt)
                             .Take(Keys.Count - MaxKeys + 1).Select(kv => kv.Key).ToList())
                    Keys.Remove(k);
        }
    }
}
