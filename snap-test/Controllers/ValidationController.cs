using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// Request validation in its common styles:
    ///   /register    — hand-written rules, 422 application/problem+json (RFC 7807) with per-field errors.
    ///   /strict      — rejects unknown fields (400) as well as invalid ones (422).
    ///   /annotations — DataAnnotations model; [ApiController] produces the framework's automatic 400.
    ///   /problem/{type} — sample problem documents for each common error status.
    /// Nothing is stored, so the same payload always gets the same answer.
    /// </summary>
    [ApiController]
    [Route("api/validation")]
    public class ValidationController : ControllerBase
    {
        private static readonly string[] Countries = { "US", "GB", "IN", "DE", "FR", "JP", "CA", "AU", "BR" };
        private static readonly HashSet<string> TakenUsernames = new(StringComparer.OrdinalIgnoreCase) { "admin", "root", "apibee", "test_user" };
        private static readonly Regex UsernamePattern = new("^[A-Za-z0-9_]+$");
        private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        private static readonly Regex PhonePattern = new(@"^\+[1-9]\d{6,14}$");
        private static int _nextUserId = 1000;

        /// <summary>Signup model validated by DataAnnotations.</summary>
        public class AnnotatedSignup
        {
            /// <summary>Required, 3-20 characters.</summary>
            [Required, StringLength(20, MinimumLength = 3)]
            public string Username { get; set; } = string.Empty;

            /// <summary>Required, must be a valid email address.</summary>
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            /// <summary>Age, 18-120.</summary>
            [Range(18, 120)]
            public int Age { get; set; }

            /// <summary>Required, 2-letter uppercase ISO country code, e.g. <c>US</c>.</summary>
            [Required, RegularExpression("^[A-Z]{2}$", ErrorMessage = "The CountryCode field must be a 2-letter uppercase ISO code.")]
            public string CountryCode { get; set; } = string.Empty;

            /// <summary>Optional absolute http(s) or ftp URL.</summary>
            [Url]
            public string? Website { get; set; }

            /// <summary>Optional phone number.</summary>
            [Phone]
            public string? Phone { get; set; }

            /// <summary>Optional list of 1-5 tags.</summary>
            [MinLength(1), MaxLength(5)]
            public List<string>? Tags { get; set; }
        }

        // -------------------- REGISTER (422 problem+json) --------------------
        /// <summary>Register a user; invalid fields return 422 problem+json.</summary>
        /// <param name="body">JSON object with username, email, password, optional confirmPassword, age, country, optional phone and acceptTerms.</param>
        /// <remarks>
        /// Rules:
        /// <list type="bullet">
        /// <item><description>username: 3-20 letters, digits or underscores; admin, root, apibee and test_user are taken</description></item>
        /// <item><description>email: must be a valid address</description></item>
        /// <item><description>password: 8+ characters with an uppercase letter, a lowercase letter, a digit and a special character</description></item>
        /// <item><description>confirmPassword: if present, must match password</description></item>
        /// <item><description>age: integer, 18-120</description></item>
        /// <item><description>country: one of US, GB, IN, DE, FR, JP, CA, AU, BR</description></item>
        /// <item><description>phone: optional, E.164 format</description></item>
        /// <item><description>acceptTerms: must be <c>true</c></description></item>
        /// </list>
        /// Errors come back as RFC 7807 <c>application/problem+json</c>, with an <c>errors</c> map of field to messages. Nothing
        /// is stored, so the same payload always gets the same answer.
        /// </remarks>
        /// <response code="201">User registered (the password is never echoed back).</response>
        /// <response code="400">Body is not a JSON object.</response>
        /// <response code="422">One or more fields failed validation.</response>
        [HttpPost("register")]
        public IActionResult Register([FromBody] JsonElement body)
        {
            if (body.ValueKind != JsonValueKind.Object)
                return BadRequest(ApiResponse.Error(400, "Request body must be a JSON object."));

            var errors = new Dictionary<string, List<string>>();
            void Add(string field, string message)
            {
                if (!errors.TryGetValue(field, out var list)) errors[field] = list = new List<string>();
                list.Add(message);
            }

            // username
            var username = GetString(body, "username", Add);
            if (username != null)
            {
                if (username.Length < 3 || username.Length > 20) Add("username", "Username must be 3-20 characters long.");
                if (!UsernamePattern.IsMatch(username)) Add("username", "Username may contain only letters, digits and underscores.");
                if (TakenUsernames.Contains(username)) Add("username", $"Username '{username}' is already taken.");
            }

            // email
            var email = GetString(body, "email", Add);
            if (email != null && !EmailPattern.IsMatch(email)) Add("email", "Email must be a valid email address.");

            // password (+ optional confirmPassword)
            var password = GetString(body, "password", Add);
            if (password != null)
            {
                if (password.Length < 8) Add("password", "Password must be at least 8 characters long.");
                if (!password.Any(char.IsUpper)) Add("password", "Password must contain an uppercase letter.");
                if (!password.Any(char.IsLower)) Add("password", "Password must contain a lowercase letter.");
                if (!password.Any(char.IsDigit)) Add("password", "Password must contain a digit.");
                if (password.All(char.IsLetterOrDigit)) Add("password", "Password must contain a special character.");
            }

            if (TryGet(body, "confirmPassword", out var confirm) &&
                (confirm.ValueKind != JsonValueKind.String || confirm.GetString() != password))
                Add("confirmPassword", "Passwords do not match.");

            // age
            int? age = null;
            if (!TryGet(body, "age", out var ageEl) || ageEl.ValueKind == JsonValueKind.Null)
                Add("age", "Age is required.");
            else if (ageEl.ValueKind != JsonValueKind.Number || !ageEl.TryGetInt32(out var parsedAge))
                Add("age", "Age must be an integer.");
            else if (parsedAge < 18 || parsedAge > 120)
                Add("age", "Age must be between 18 and 120.");
            else
                age = parsedAge;

            // country
            var country = GetString(body, "country", Add)?.ToUpperInvariant();
            if (country != null && !Countries.Contains(country))
                Add("country", $"Country must be one of: {string.Join(", ", Countries)}.");

            // phone (optional)
            if (TryGet(body, "phone", out var phoneEl) && phoneEl.ValueKind != JsonValueKind.Null &&
                (phoneEl.ValueKind != JsonValueKind.String || !PhonePattern.IsMatch(phoneEl.GetString()!)))
                Add("phone", "Phone must be in E.164 format, e.g. +15550101.");

            // acceptTerms
            if (!TryGet(body, "acceptTerms", out var terms) || terms.ValueKind == JsonValueKind.Null)
                Add("acceptTerms", "acceptTerms is required.");
            else if (terms.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                Add("acceptTerms", "acceptTerms must be a boolean.");
            else if (terms.ValueKind == JsonValueKind.False)
                Add("acceptTerms", "You must accept the terms and conditions.");

            if (errors.Count > 0) return ValidationFailed(errors);

            return StatusCode(201, new
            {
                message = "User registered successfully",
                data = new
                {
                    id = Interlocked.Increment(ref _nextUserId),
                    username,
                    email,
                    age,
                    country,
                    createdAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }
            });
        }

        // -------------------- STRICT (unknown fields rejected) --------------------
        /// <summary>Accept only known fields; unknown fields return 400.</summary>
        /// <param name="body">JSON object with exactly: name (1-50 characters), email and quantity (integer 1-99).</param>
        /// <remarks>Field names are case-sensitive. Unknown fields are checked before values are validated.</remarks>
        /// <response code="200">Payload accepted.</response>
        /// <response code="400">Unknown fields (problem+json with <c>unknownFields</c>), or the body is not an object.</response>
        /// <response code="422">Invalid values.</response>
        [HttpPost("strict")]
        public IActionResult Strict([FromBody] JsonElement body)
        {
            if (body.ValueKind != JsonValueKind.Object)
                return BadRequest(ApiResponse.Error(400, "Request body must be a JSON object."));

            var allowed = new[] { "name", "email", "quantity" };
            var unknown = body.EnumerateObject().Select(p => p.Name).Where(n => !allowed.Contains(n)).ToList();

            if (unknown.Count > 0)
            {
                return ProblemResult(400, new
                {
                    type = "https://example.com/problems/unknown-fields",
                    title = "Request body contains unknown fields.",
                    status = 400,
                    detail = $"Allowed fields: {string.Join(", ", allowed)} (names are case-sensitive).",
                    instance = Request.Path.Value,
                    unknownFields = unknown
                });
            }

            var errors = new Dictionary<string, List<string>>();
            void Add(string field, string message) => errors[field] = new List<string> { message };

            body.TryGetProperty("name", out var name);
            if (name.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(name.GetString()) || name.GetString()!.Length > 50)
                Add("name", "name is required and must be a string of 1-50 characters.");

            body.TryGetProperty("email", out var email);
            if (email.ValueKind != JsonValueKind.String || !EmailPattern.IsMatch(email.GetString()!))
                Add("email", "email is required and must be a valid email address.");

            body.TryGetProperty("quantity", out var quantity);
            if (quantity.ValueKind != JsonValueKind.Number || !quantity.TryGetInt32(out var qty) || qty < 1 || qty > 99)
                Add("quantity", "quantity is required and must be an integer between 1 and 99.");

            if (errors.Count > 0) return ValidationFailed(errors);

            return Ok(new
            {
                message = "Payload accepted",
                data = new { name = name.GetString(), email = email.GetString(), quantity = quantity.GetInt32() }
            });
        }

        // -------------------- DATA ANNOTATIONS (framework auto-400) --------------------
        /// <summary>Validate a DataAnnotations model using the framework's automatic 400.</summary>
        /// <param name="signup">The signup to validate.</param>
        /// <remarks>Invalid models are rejected by <c>[ApiController]</c> before the action runs, with a standard ValidationProblemDetails body.</remarks>
        /// <response code="201">Model is valid.</response>
        /// <response code="400">Validation errors (ValidationProblemDetails).</response>
        [HttpPost("annotations")]
        public IActionResult Annotations([FromBody] AnnotatedSignup signup) =>
            StatusCode(201, new { message = "Signup is valid", data = signup });

        // -------------------- SAMPLE PROBLEM DOCUMENTS --------------------
        /// <summary>List the sample problem document types.</summary>
        /// <response code="200">Each type, its status code and its URL.</response>
        [HttpGet("problem")]
        public IActionResult ProblemTypes() => Ok(Problems.Select(p => new
        {
            type = p.Key,
            status = p.Value.Status,
            url = $"/api/validation/problem/{p.Key}"
        }));

        /// <summary>Get a sample RFC 7807 problem document.</summary>
        /// <param name="type">bad-request, unauthorized, payment-required, forbidden, not-found, conflict, validation, rate-limited, internal or unavailable (case-insensitive).</param>
        /// <remarks>The status code matches the problem. 401 adds <c>WWW-Authenticate</c>; 429 and 503 add <c>Retry-After</c>.</remarks>
        /// <response code="200">Never returned: every sample uses its own error status.</response>
        /// <response code="404">Unknown problem type.</response>
        [HttpGet("problem/{type}")]
        public IActionResult ProblemSample(string type)
        {
            if (!Problems.TryGetValue(type.ToLowerInvariant(), out var p))
                return NotFound(ApiResponse.Error(404, $"Unknown problem type '{type}'. Available: {string.Join(", ", Problems.Keys)}."));

            if (p.Status == 401) Response.Headers.WWWAuthenticate = "Bearer realm=\"apibee\", error=\"invalid_token\"";
            if (p.Status == 429) Response.Headers.RetryAfter = "60";
            if (p.Status == 503) Response.Headers.RetryAfter = "120";

            return ProblemResult(p.Status, new Dictionary<string, object?>
            {
                ["type"] = $"https://example.com/problems/{type.ToLowerInvariant()}",
                ["title"] = p.Title,
                ["status"] = p.Status,
                ["detail"] = p.Detail,
                ["instance"] = Request.Path.Value,
                ["traceId"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
            }.Concat(p.Extensions).ToDictionary(kv => kv.Key, kv => kv.Value));
        }

        private static readonly Dictionary<string, (int Status, string Title, string Detail, Dictionary<string, object?> Extensions)> Problems = new()
        {
            ["bad-request"] = (400, "Bad Request", "The request body could not be parsed as JSON.", new() { ["line"] = 3, ["column"] = 14 }),
            ["unauthorized"] = (401, "Unauthorized", "The access token is missing, expired or invalid.", new()),
            ["payment-required"] = (402, "Payment Required", "Your trial has ended. Upgrade your plan to continue.", new() { ["plan"] = "free", ["upgradeUrl"] = "https://example.com/pricing" }),
            ["forbidden"] = (403, "Forbidden", "Your role 'viewer' cannot delete projects.", new() { ["requiredRole"] = "admin" }),
            ["not-found"] = (404, "Not Found", "Order 9999 does not exist.", new() { ["resource"] = "order", ["id"] = 9999 }),
            ["conflict"] = (409, "Conflict", "The resource was modified by another request. Reload and try again.", new() { ["currentVersion"] = 7, ["yourVersion"] = 6 }),
            ["validation"] = (422, "One or more fields failed validation.", "2 field(s) failed validation.", new()
            {
                ["errors"] = new Dictionary<string, string[]>
                {
                    ["email"] = new[] { "Email must be a valid email address." },
                    ["age"] = new[] { "Age must be between 18 and 120." }
                }
            }),
            ["rate-limited"] = (429, "Too Many Requests", "Rate limit of 100 requests per minute exceeded.", new() { ["limit"] = 100, ["retryAfterSeconds"] = 60 }),
            ["internal"] = (500, "Internal Server Error", "An unexpected error occurred. Quote the traceId when contacting support.", new()),
            ["unavailable"] = (503, "Service Unavailable", "Scheduled maintenance in progress.", new() { ["retryAfterSeconds"] = 120 })
        };

        private ObjectResult ValidationFailed(Dictionary<string, List<string>> errors) => ProblemResult(422, new
        {
            type = "https://example.com/problems/validation-error",
            title = "One or more fields failed validation.",
            status = 422,
            detail = $"{errors.Count} field(s) failed validation.",
            instance = Request.Path.Value,
            errors
        });

        private static ObjectResult ProblemResult(int status, object body) => new(body)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };

        // Case-insensitive property lookup, so "Username" and "username" both work.
        private static bool TryGet(JsonElement obj, string name, out JsonElement value)
        {
            foreach (var p in obj.EnumerateObject())
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = p.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }

        private static string? GetString(JsonElement obj, string name, Action<string, string> addError)
        {
            if (!TryGet(obj, name, out var value) || value.ValueKind == JsonValueKind.Null ||
                (value.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(value.GetString())))
            {
                addError(name, $"{char.ToUpper(name[0])}{name[1..]} is required.");
                return null;
            }

            if (value.ValueKind != JsonValueKind.String)
            {
                addError(name, $"{char.ToUpper(name[0])}{name[1..]} must be a string.");
                return null;
            }

            return value.GetString();
        }
    }
}
