using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// Small utility endpoints handy for chaining requests in API tests: ids, time, encoding, hashing,
    /// random data, lorem ipsum and JWT inspection.
    /// </summary>
    [ApiController]
    [Route("api/utils")]
    public class UtilsController : ControllerBase
    {
        // -------------------- UUID --------------------
        /// <summary>Generate one or more UUIDs.</summary>
        /// <param name="count">How many to generate, 1–100, default 1. One returns <c>uuid</c>; more return a <c>uuids</c> array.</param>
        /// <param name="version"><c>4</c> (random, default) or <c>7</c> (time-ordered).</param>
        /// <response code="200">Success.</response>
        /// <response code="400"><c>count</c> or <c>version</c> is out of range.</response>
        [HttpGet("uuid")]
        public IActionResult Uuid([FromQuery] int count = 1, [FromQuery] int version = 4)
        {
            if (count < 1 || count > 100)
                return BadRequest(ApiResponse.Error(400, "count must be between 1 and 100."));
            if (version != 4 && version != 7)
                return BadRequest(ApiResponse.Error(400, "version must be 4 or 7."));

            var ids = Enumerable.Range(0, count)
                .Select(_ => (version == 7 ? Guid.CreateVersion7() : Guid.NewGuid()).ToString())
                .ToList();

            return count == 1 ? Ok(new { uuid = ids[0], version }) : Ok(new { uuids = ids, version, count });
        }

        // -------------------- TIME --------------------
        /// <summary>Return the current time in many formats.</summary>
        /// <param name="tz">Optional time zone, as an IANA id (<c>Asia/Kolkata</c>) or a Windows id (<c>India Standard Time</c>). Default UTC.</param>
        /// <response code="200">Success.</response>
        /// <response code="400">Unknown time zone.</response>
        [HttpGet("time")]
        public IActionResult Time([FromQuery] string? tz = null)
        {
            var now = DateTimeOffset.UtcNow;
            TimeZoneInfo zone = TimeZoneInfo.Utc;

            if (!string.IsNullOrWhiteSpace(tz))
            {
                try
                {
                    zone = TimeZoneInfo.FindSystemTimeZoneById(tz);
                }
                catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
                {
                    return BadRequest(ApiResponse.Error(400, $"Unknown time zone '{tz}'. Try an IANA id such as 'Asia/Kolkata' or 'America/New_York'."));
                }
            }

            var local = TimeZoneInfo.ConvertTime(now, zone);

            return Ok(new
            {
                utc = now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                unix = now.ToUnixTimeSeconds(),
                unixMs = now.ToUnixTimeMilliseconds(),
                rfc1123 = now.ToString("R"),
                timeZone = new
                {
                    id = zone.Id,
                    displayName = zone.DisplayName,
                    offset = local.ToString("zzz"),
                    isDaylightSaving = zone.IsDaylightSavingTime(local)
                },
                local = local.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
                date = local.ToString("yyyy-MM-dd"),
                time = local.ToString("HH:mm:ss"),
                dayOfWeek = local.DayOfWeek.ToString(),
                dayOfYear = local.DayOfYear,
                weekOfYear = System.Globalization.ISOWeek.GetWeekOfYear(local.DateTime)
            });
        }

        // -------------------- BASE64 --------------------
        /// <summary>Base64-encode UTF-8 text.</summary>
        /// <param name="text">Text to encode. Default empty.</param>
        /// <param name="urlSafe">Use the URL-safe alphabet (<c>-</c> and <c>_</c>) without padding. Default false.</param>
        /// <response code="200">Success.</response>
        [HttpGet("base64/encode")]
        public IActionResult Base64Encode([FromQuery] string text = "", [FromQuery] bool urlSafe = false)
        {
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
            if (urlSafe) encoded = encoded.TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return Ok(new { input = text, encoded, urlSafe });
        }

        /// <summary>Decode a base64 string.</summary>
        /// <param name="value">Standard or URL-safe base64, with or without padding. Send <c>+</c> as <c>%2B</c> in the query string.</param>
        /// <remarks>If the decoded bytes are not valid UTF-8, <c>decoded</c> is null and <c>hex</c> holds the bytes.</remarks>
        /// <response code="200">Success.</response>
        /// <response code="400"><c>value</c> is not valid base64.</response>
        [HttpGet("base64/decode")]
        public IActionResult Base64Decode([FromQuery] string value = "")
        {
            var normalized = value.Trim().Replace('-', '+').Replace('_', '/');
            normalized = normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(normalized);
            }
            catch (FormatException)
            {
                return BadRequest(ApiResponse.Error(400, "value is not valid base64."));
            }

            string? decoded;
            try
            {
                decoded = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                decoded = null; // binary content — the hex form is still returned
            }

            return Ok(new { input = value, decoded, isUtf8 = decoded != null, hex = Convert.ToHexString(bytes).ToLowerInvariant(), length = bytes.Length });
        }

        // -------------------- URL ENCODING --------------------
        /// <summary>Percent-encode text (RFC 3986).</summary>
        /// <param name="text">Text to encode. Default empty.</param>
        /// <response code="200">Success.</response>
        [HttpGet("url/encode")]
        public IActionResult UrlEncode([FromQuery] string text = "") =>
            Ok(new { input = text, encoded = Uri.EscapeDataString(text) });

        /// <summary>Percent-decode a string.</summary>
        /// <param name="value">Value to decode. The server already decodes the query string once, so double-encode it (e.g. <c>%2520</c> for <c>%20</c>). A literal <c>+</c> is kept as-is.</param>
        /// <response code="200">Success.</response>
        [HttpGet("url/decode")]
        public IActionResult UrlDecode([FromQuery] string value = "") =>
            Ok(new { input = value, decoded = Uri.UnescapeDataString(value) });

        // -------------------- HASH / HMAC --------------------
        /// <summary>Hash UTF-8 text.</summary>
        /// <param name="text">Text to hash. Default empty.</param>
        /// <param name="algorithm"><c>md5</c>, <c>sha1</c>, <c>sha256</c> (default), <c>sha384</c> or <c>sha512</c>. Case-insensitive.</param>
        /// <response code="200">Success.</response>
        /// <response code="400">Unknown algorithm.</response>
        [HttpGet("hash")]
        public IActionResult Hash([FromQuery] string text = "", [FromQuery] string algorithm = "sha256")
        {
            var data = Encoding.UTF8.GetBytes(text);
            byte[]? hash = algorithm.ToLowerInvariant() switch
            {
                "md5" => MD5.HashData(data),
                "sha1" => SHA1.HashData(data),
                "sha256" => SHA256.HashData(data),
                "sha384" => SHA384.HashData(data),
                "sha512" => SHA512.HashData(data),
                _ => null
            };

            if (hash == null)
                return BadRequest(ApiResponse.Error(400, "algorithm must be one of: md5, sha1, sha256, sha384, sha512."));

            return Ok(new { input = text, algorithm = algorithm.ToLowerInvariant(), hex = Convert.ToHexString(hash).ToLowerInvariant(), base64 = Convert.ToBase64String(hash) });
        }

        /// <summary>Compute an HMAC of UTF-8 text.</summary>
        /// <param name="text">Message to sign. Default empty.</param>
        /// <param name="key">Secret key. Default <c>apibee-secret</c>.</param>
        /// <param name="algorithm"><c>md5</c>, <c>sha1</c>, <c>sha256</c> (default), <c>sha384</c> or <c>sha512</c>. Case-insensitive.</param>
        /// <response code="200">Success.</response>
        /// <response code="400">Unknown algorithm.</response>
        [HttpGet("hmac")]
        public IActionResult Hmac([FromQuery] string text = "", [FromQuery] string key = "apibee-secret", [FromQuery] string algorithm = "sha256")
        {
            var data = Encoding.UTF8.GetBytes(text);
            var keyBytes = Encoding.UTF8.GetBytes(key);
            byte[]? mac = algorithm.ToLowerInvariant() switch
            {
                "md5" => HMACMD5.HashData(keyBytes, data),
                "sha1" => HMACSHA1.HashData(keyBytes, data),
                "sha256" => HMACSHA256.HashData(keyBytes, data),
                "sha384" => HMACSHA384.HashData(keyBytes, data),
                "sha512" => HMACSHA512.HashData(keyBytes, data),
                _ => null
            };

            if (mac == null)
                return BadRequest(ApiResponse.Error(400, "algorithm must be one of: md5, sha1, sha256, sha384, sha512."));

            return Ok(new { input = text, key, algorithm = algorithm.ToLowerInvariant(), hex = Convert.ToHexString(mac).ToLowerInvariant(), base64 = Convert.ToBase64String(mac) });
        }

        // -------------------- RANDOM --------------------
        /// <summary>Generate random integers.</summary>
        /// <param name="min">Inclusive lower bound. Default 0.</param>
        /// <param name="max">Inclusive upper bound, at least <c>min</c>. Default 100.</param>
        /// <param name="count">How many to generate, 1–100, default 1. One returns <c>number</c>; more return a <c>numbers</c> array.</param>
        /// <param name="seed">Optional seed for repeatable output.</param>
        /// <response code="200">Success.</response>
        /// <response code="400"><c>min</c> is greater than <c>max</c>, or <c>count</c> is out of range.</response>
        [HttpGet("random/number")]
        public IActionResult RandomNumber([FromQuery] int min = 0, [FromQuery] int max = 100, [FromQuery] int count = 1, [FromQuery] int? seed = null)
        {
            if (min > max) return BadRequest(ApiResponse.Error(400, "min must be less than or equal to max."));
            if (count < 1 || count > 100) return BadRequest(ApiResponse.Error(400, "count must be between 1 and 100."));

            var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
            var numbers = Enumerable.Range(0, count).Select(_ => (int)rng.NextInt64(min, (long)max + 1)).ToList();

            return count == 1 ? Ok(new { number = numbers[0], min, max, seed }) : Ok(new { numbers, min, max, seed });
        }

        private static readonly Dictionary<string, string> Charsets = new(StringComparer.OrdinalIgnoreCase)
        {
            ["alphanumeric"] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789",
            ["alpha"] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz",
            ["numeric"] = "0123456789",
            ["hex"] = "0123456789abcdef",
            ["symbols"] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()-_=+[]{}"
        };

        /// <summary>Generate a random string.</summary>
        /// <param name="length">Length, 1–1000, default 16.</param>
        /// <param name="charset"><c>alphanumeric</c> (default), <c>alpha</c>, <c>numeric</c>, <c>hex</c> or <c>symbols</c>. Case-insensitive.</param>
        /// <param name="seed">Optional seed for repeatable output.</param>
        /// <response code="200">Success.</response>
        /// <response code="400"><c>length</c> is out of range or the charset is unknown.</response>
        [HttpGet("random/string")]
        public IActionResult RandomString([FromQuery] int length = 16, [FromQuery] string charset = "alphanumeric", [FromQuery] int? seed = null)
        {
            if (length < 1 || length > 1000) return BadRequest(ApiResponse.Error(400, "length must be between 1 and 1000."));
            if (!Charsets.TryGetValue(charset, out var chars))
                return BadRequest(ApiResponse.Error(400, $"charset must be one of: {string.Join(", ", Charsets.Keys)}."));

            var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
            var value = new string(Enumerable.Range(0, length).Select(_ => chars[rng.Next(chars.Length)]).ToArray());

            return Ok(new { value, length, charset = charset.ToLowerInvariant(), seed });
        }

        // -------------------- LOREM IPSUM --------------------
        private static readonly string[] LoremParagraphs =
        {
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.",
            "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
            "Curabitur pretium tincidunt lacus. Nulla gravida orci a odio. Nullam varius, turpis et commodo pharetra, est eros bibendum elit, nec luctus magna felis sollicitudin mauris. Integer in mauris eu nibh euismod gravida.",
            "Praesent dapibus, neque id cursus faucibus, tortor neque egestas augue, eu vulputate magna eros eu erat. Aliquam erat volutpat. Nam dui mi, tincidunt quis, accumsan porttitor, facilisis luctus, metus.",
            "Phasellus ultrices nulla quis nibh. Quisque a lectus. Donec consectetuer ligula vulputate sem tristique cursus. Nam nulla quam, gravida non, commodo a, sodales sit amet, nisi."
        };

        /// <summary>Generate lorem ipsum text.</summary>
        /// <param name="paragraphs">Number of paragraphs, 1–50, default 3.</param>
        /// <param name="format"><c>json</c> (default) for an object with a word count, or <c>text</c> for plain text.</param>
        /// <remarks>Deterministic: five fixed paragraphs repeat in order.</remarks>
        /// <response code="200">Success.</response>
        /// <response code="400"><c>paragraphs</c> is outside 1–50.</response>
        [HttpGet("lorem")]
        public IActionResult Lorem([FromQuery] int paragraphs = 3, [FromQuery] string format = "json")
        {
            if (paragraphs < 1 || paragraphs > 50) return BadRequest(ApiResponse.Error(400, "paragraphs must be between 1 and 50."));

            var list = Enumerable.Range(0, paragraphs).Select(i => LoremParagraphs[i % LoremParagraphs.Length]).ToList();

            if (string.Equals(format, "text", StringComparison.OrdinalIgnoreCase))
                return Content(string.Join("\n\n", list) + "\n", "text/plain; charset=utf-8");

            var words = list.Sum(p => p.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
            return Ok(new { paragraphs = list, count = paragraphs, words });
        }

        // -------------------- JWT DECODE (no verification) --------------------
        /// <summary>Decode a JWT without verifying its signature.</summary>
        /// <param name="token">The JWT. If omitted, the Bearer token from the <c>Authorization</c> header is used.</param>
        /// <remarks>Returns the header, the payload and readable <c>exp</c>/<c>iat</c>/<c>nbf</c> claims with an expiry flag. The signature is never verified, so never trust the claims.</remarks>
        /// <response code="200">Success.</response>
        /// <response code="400">No token, wrong number of parts, or a part is not base64url-encoded JSON.</response>
        [HttpGet("jwt/decode")]
        public IActionResult JwtDecode([FromQuery] string? token = null)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                var auth = Request.Headers.Authorization.ToString();
                if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    token = auth["Bearer ".Length..].Trim();
            }

            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(ApiResponse.Error(400, "Provide a JWT via ?token= or an 'Authorization: Bearer <token>' header."));

            var parts = token.Split('.');
            if (parts.Length != 3)
                return BadRequest(ApiResponse.Error(400, $"A JWT must have 3 dot-separated parts; got {parts.Length}."));

            JsonElement header, payload;
            try
            {
                header = ParseSegment(parts[0]);
                payload = ParseSegment(parts[1]);
            }
            catch (Exception ex) when (ex is FormatException or JsonException or ArgumentException)
            {
                return BadRequest(ApiResponse.Error(400, "Token header/payload is not valid base64url-encoded JSON."));
            }

            DateTimeOffset? FromClaim(string name) =>
                payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty(name, out var v) && v.TryGetInt64(out var secs)
                    ? DateTimeOffset.FromUnixTimeSeconds(secs)
                    : null;

            var exp = FromClaim("exp");
            var iat = FromClaim("iat");
            var nbf = FromClaim("nbf");

            return Ok(new
            {
                header,
                payload,
                signature = parts[2],
                verified = false,
                claims = new
                {
                    issuedAt = iat?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    notBefore = nbf?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    expiresAt = exp?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    expired = exp.HasValue ? exp.Value < DateTimeOffset.UtcNow : (bool?)null
                },
                note = "Signature was NOT verified. Never trust decoded claims without verification."
            });
        }

        private static JsonElement ParseSegment(string segment)
        {
            var b64 = segment.Replace('-', '+').Replace('_', '/');
            b64 = b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Convert.FromBase64String(b64));
            return doc.RootElement.Clone();
        }
    }
}
