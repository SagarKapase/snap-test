using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// Auth schemes: one endpoint per common scheme (Basic, Bearer, API key in header/query/cookie, Digest,
    /// HMAC request signing, role and scope checks, double-submit CSRF) for testing how a client sends credentials.
    /// All credentials are hardcoded and listed by <c>GET /api/auth</c>.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthSchemesController : ControllerBase
    {
        private const string BasicUser = "apibee";
        private const string BasicPassword = "password123";
        private const string BearerToken = "apibee-token-123";
        private const string ApiKey = "apibee-key-123";
        private const string HmacSecret = "apibee-hmac-secret";
        private const string DigestRealm = "apibee";
        private const int HmacToleranceSeconds = 300;
        private const int DigestNonceLifetimeSeconds = 300;

        // Server-side secret used to sign stateless Digest nonces. Regenerated on restart (old nonces become stale).
        private static readonly byte[] NonceKey = RandomNumberGenerator.GetBytes(32);

        private record RoleToken(string Name, string[] Roles, string[] Scopes);

        private static readonly Dictionary<string, RoleToken> RoleTokens = new()
        {
            ["admin-token"] = new("Alice Admin", new[] { "admin", "user" }, new[] { "read", "write", "delete" }),
            ["user-token"] = new("Uma User", new[] { "user" }, new[] { "read", "write" }),
            ["readonly-token"] = new("Rita Reader", Array.Empty<string>(), new[] { "read" })
        };

        // -------------------- SCHEME LIST --------------------
        /// <summary>List every auth scheme with its test credentials.</summary>
        /// <response code="200">Schemes, endpoints and credentials.</response>
        [HttpGet]
        public IActionResult Schemes() => Ok(new
        {
            message = "Hardcoded credentials for every auth scheme. Send them wrong to test your 401/403 handling.",
            schemes = new object[]
            {
                new { scheme = "basic", endpoint = "GET /api/auth/basic", credentials = new { username = BasicUser, password = BasicPassword } },
                new { scheme = "basic (path creds)", endpoint = "GET /api/auth/basic/{user}/{pass}", credentials = "Any user/pass — must match the path" },
                new { scheme = "basic (hidden)", endpoint = "GET /api/auth/basic-hidden/{user}/{pass}", credentials = "Like above but returns 404 instead of 401" },
                new { scheme = "bearer", endpoint = "GET /api/auth/bearer", credentials = new { header = $"Authorization: Bearer {BearerToken}" } },
                new { scheme = "api key (header)", endpoint = "GET /api/auth/api-key/header", credentials = new { header = $"X-API-Key: {ApiKey}" } },
                new { scheme = "api key (query)", endpoint = "GET /api/auth/api-key/query", credentials = new { query = $"?api_key={ApiKey}" } },
                new { scheme = "api key (cookie)", endpoint = "GET /api/auth/api-key/cookie", credentials = new { cookie = $"api_key={ApiKey}" } },
                new { scheme = "digest", endpoint = "GET /api/auth/digest", credentials = new { username = BasicUser, password = BasicPassword, algorithm = "MD5", qop = "auth" } },
                new { scheme = "hmac", endpoint = "POST /api/auth/hmac", credentials = new { secret = HmacSecret, headers = "X-Timestamp: <unix seconds>, X-Signature: hex(HMAC-SHA256(secret, timestamp + \".\" + rawBody))" } },
                new { scheme = "roles/scopes", endpoint = "GET /api/auth/roles/admin, GET /api/auth/roles/user, DELETE /api/auth/roles/resource", credentials = new { tokens = RoleTokens.Keys } },
                new { scheme = "csrf", endpoint = "GET /api/auth/csrf/token then POST /api/auth/csrf/submit", credentials = "Send the XSRF-TOKEN cookie back as the X-CSRF-Token header" },
                new { scheme = "oauth2", endpoint = "/api/auth/oauth/*", credentials = new { client_id = "apibee-client", client_secret = "apibee-secret", public_client_id = "apibee-public", username = BasicUser, password = BasicPassword } },
                new { scheme = "jwt", endpoint = "/api/auth/jwt/*", credentials = new[] { new { username = "admin", password = "admin123", role = "admin" }, new { username = "user", password = "user123", role = "user" } } }
            }
        });

        // -------------------- BASIC --------------------
        /// <summary>Authenticate with HTTP Basic auth.</summary>
        /// <remarks>
        /// Credentials: <c>apibee</c> / <c>password123</c>.
        /// <para>Header: <c>Authorization: Basic YXBpYmVlOnBhc3N3b3JkMTIz</c> (base64 of <c>apibee:password123</c>).</para>
        /// </remarks>
        /// <response code="200">Authenticated.</response>
        /// <response code="401">Header missing or malformed, or wrong credentials. Includes <c>WWW-Authenticate: Basic realm="apibee"</c>.</response>
        [HttpGet("basic")]
        public IActionResult Basic()
        {
            if (!AuthHelpers.TryParseBasic(Request, out var user, out var pass))
                return BasicChallenge("Missing or malformed Basic Authorization header.");

            if (!AuthHelpers.SecureEquals(user, BasicUser) || !AuthHelpers.SecureEquals(pass, BasicPassword))
                return BasicChallenge("Invalid username or password.");

            return Ok(new { authenticated = true, scheme = "basic", user });
        }

        // -------------------- BASIC (credentials in path, httpbin style) --------------------
        /// <summary>Authenticate with Basic auth against credentials taken from the URL.</summary>
        /// <param name="user">Username the Authorization header must contain.</param>
        /// <param name="pass">Password the Authorization header must contain.</param>
        /// <remarks>httpbin style: <c>GET /api/auth/basic/foo/bar</c> with <c>Authorization: Basic base64(foo:bar)</c>.</remarks>
        /// <response code="200">Header matches the URL credentials.</response>
        /// <response code="401">Header missing, malformed or different. Includes a Basic challenge.</response>
        [HttpGet("basic/{user}/{pass}")]
        public IActionResult BasicPath(string user, string pass)
        {
            if (!AuthHelpers.TryParseBasic(Request, out var u, out var p))
                return BasicChallenge("Missing or malformed Basic Authorization header.");

            if (!AuthHelpers.SecureEquals(u, user) || !AuthHelpers.SecureEquals(p, pass))
                return BasicChallenge("Credentials do not match the ones in the URL.");

            return Ok(new { authenticated = true, scheme = "basic", user });
        }

        // -------------------- BASIC (hidden: 404 instead of 401) --------------------
        /// <summary>Authenticate with Basic auth, returning 404 instead of 401 on failure.</summary>
        /// <param name="user">Username the Authorization header must contain.</param>
        /// <param name="pass">Password the Authorization header must contain.</param>
        /// <remarks>Mimics servers that hide protected resources. No <c>WWW-Authenticate</c> challenge is sent.</remarks>
        /// <response code="200">Header matches the URL credentials.</response>
        /// <response code="404">Header missing, malformed or different.</response>
        [HttpGet("basic-hidden/{user}/{pass}")]
        public IActionResult BasicHidden(string user, string pass)
        {
            if (!AuthHelpers.TryParseBasic(Request, out var u, out var p) ||
                !AuthHelpers.SecureEquals(u, user) || !AuthHelpers.SecureEquals(p, pass))
                return NotFound(ApiResponse.Error(404, "Not found."));

            return Ok(new { authenticated = true, scheme = "basic", user });
        }

        // -------------------- BEARER --------------------
        /// <summary>Authenticate with a static bearer token.</summary>
        /// <remarks>Header: <c>Authorization: Bearer apibee-token-123</c>.</remarks>
        /// <response code="200">Authenticated.</response>
        /// <response code="401">Token missing, or invalid (then <c>WWW-Authenticate: Bearer error="invalid_token"</c>).</response>
        [HttpGet("bearer")]
        public IActionResult Bearer()
        {
            var token = AuthHelpers.GetBearer(Request);

            if (token == null)
            {
                Response.Headers.WWWAuthenticate = "Bearer realm=\"apibee\"";
                return Unauthorized(ApiResponse.Error(401, "Missing Authorization: Bearer <token> header."));
            }

            if (!AuthHelpers.SecureEquals(token, BearerToken))
            {
                Response.Headers.WWWAuthenticate = "Bearer realm=\"apibee\", error=\"invalid_token\", error_description=\"The access token is invalid\"";
                return Unauthorized(ApiResponse.Error(401, "Invalid bearer token."));
            }

            return Ok(new { authenticated = true, scheme = "bearer", token });
        }

        // -------------------- API KEY (header) --------------------
        /// <summary>Authenticate with an API key in the X-API-Key header.</summary>
        /// <remarks>Header: <c>X-API-Key: apibee-key-123</c>.</remarks>
        /// <response code="200">Authenticated.</response>
        /// <response code="401">No key sent.</response>
        /// <response code="403">Key sent but wrong.</response>
        [HttpGet("api-key/header")]
        public IActionResult ApiKeyHeader()
        {
            var key = Request.Headers["X-API-Key"].ToString();
            return CheckApiKey(key, "X-API-Key header");
        }

        // -------------------- API KEY (query) --------------------
        /// <summary>Authenticate with an API key in the api_key query parameter.</summary>
        /// <remarks>Example: <c>GET /api/auth/api-key/query?api_key=apibee-key-123</c>.</remarks>
        /// <response code="200">Authenticated.</response>
        /// <response code="401">No key sent.</response>
        /// <response code="403">Key sent but wrong.</response>
        [HttpGet("api-key/query")]
        public IActionResult ApiKeyQuery()
        {
            var key = Request.Query["api_key"].ToString();
            return CheckApiKey(key, "api_key query parameter");
        }

        // -------------------- API KEY (cookie) --------------------
        /// <summary>Authenticate with an API key in the api_key cookie.</summary>
        /// <remarks>Header: <c>Cookie: api_key=apibee-key-123</c>.</remarks>
        /// <response code="200">Authenticated.</response>
        /// <response code="401">No key sent.</response>
        /// <response code="403">Key sent but wrong.</response>
        [HttpGet("api-key/cookie")]
        public IActionResult ApiKeyCookie()
        {
            var key = Request.Cookies["api_key"] ?? string.Empty;
            return CheckApiKey(key, "api_key cookie");
        }

        // -------------------- DIGEST (RFC 7616, MD5, qop=auth) --------------------
        /// <summary>Authenticate with HTTP Digest auth (MD5, qop=auth).</summary>
        /// <remarks>
        /// Credentials: <c>apibee</c> / <c>password123</c>, realm <c>apibee</c>. A request without credentials gets 401 with a
        /// <c>WWW-Authenticate: Digest</c> challenge carrying <c>nonce</c> and <c>opaque</c>; clients such as <c>curl --digest</c>,
        /// Postman and <c>requests.auth.HTTPDigestAuth</c> then retry automatically.
        /// <para>
        /// <c>response = MD5(HA1:nonce:nc:cnonce:qop:HA2)</c> where <c>HA1 = MD5(username:realm:password)</c> and
        /// <c>HA2 = MD5(method:uri)</c>. Without qop: <c>MD5(HA1:nonce:HA2)</c>. Nonces expire after 5 minutes; the 401 then includes <c>stale=true</c>.
        /// </para>
        /// </remarks>
        /// <response code="200">Authenticated.</response>
        /// <response code="401">Missing or invalid Digest header, wrong credentials, or an unknown or stale nonce. Includes a fresh challenge.</response>
        [HttpGet("digest")]
        public IActionResult Digest()
        {
            var header = Request.Headers.Authorization.ToString();
            if (!header.StartsWith("Digest ", StringComparison.OrdinalIgnoreCase))
                return DigestChallenge("Missing Digest Authorization header.", stale: false);

            var parts = Regex.Matches(header.Substring(7), "(\\w+)\\s*=\\s*(?:\"([^\"]*)\"|([^,\\s]*))")
                .ToDictionary(m => m.Groups[1].Value.ToLowerInvariant(),
                              m => m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Value);

            string Get(string k) => parts.TryGetValue(k, out var v) ? v : string.Empty;

            var username = Get("username");
            var nonce = Get("nonce");
            var uri = Get("uri");
            var response = Get("response");
            var qop = Get("qop");
            var nc = Get("nc");
            var cnonce = Get("cnonce");
            var algorithm = Get("algorithm");

            if (username == "" || nonce == "" || uri == "" || response == "")
                return DigestChallenge("Digest header is missing username, nonce, uri or response.", stale: false);

            if (Get("realm") != DigestRealm)
                return DigestChallenge($"Realm must be \"{DigestRealm}\".", stale: false);

            if (algorithm != "" && !algorithm.Equals("MD5", StringComparison.OrdinalIgnoreCase))
                return DigestChallenge("Only algorithm=MD5 is supported.", stale: false);

            if (qop != "" && qop != "auth")
                return DigestChallenge("Only qop=auth is supported.", stale: false);

            if (qop == "auth" && (nc == "" || cnonce == ""))
                return DigestChallenge("qop=auth requires nc and cnonce.", stale: false);

            var nonceState = CheckNonce(nonce);
            if (nonceState == NonceState.Invalid)
                return DigestChallenge("Nonce was not issued by this server.", stale: false);
            if (nonceState == NonceState.Stale)
                return DigestChallenge("Nonce expired — retry with the new nonce.", stale: true);

            var requestPath = Request.Path.Value ?? string.Empty;
            if (uri.Split('?')[0] != requestPath)
                return DigestChallenge("The digest uri does not match the request path.", stale: false);

            var ha1 = Md5Hex($"{username}:{DigestRealm}:{BasicPassword}");
            var ha2 = Md5Hex($"{Request.Method}:{uri}");
            var expected = qop == "auth"
                ? Md5Hex($"{ha1}:{nonce}:{nc}:{cnonce}:{qop}:{ha2}")
                : Md5Hex($"{ha1}:{nonce}:{ha2}");

            if (!AuthHelpers.SecureEquals(username, BasicUser) ||
                !AuthHelpers.SecureEquals(response.ToLowerInvariant(), expected))
                return DigestChallenge("Invalid username or password.", stale: false);

            return Ok(new { authenticated = true, scheme = "digest", user = username, qop = qop == "" ? "none" : qop });
        }

        // -------------------- HMAC REQUEST SIGNING --------------------
        /// <summary>Verify an HMAC-SHA256 request signature.</summary>
        /// <remarks>
        /// Headers:
        /// <para><c>X-Timestamp</c>: current Unix time in seconds; must be within 300 seconds of server time.</para>
        /// <para><c>X-Signature</c>: lowercase hex of <c>HMAC-SHA256(key: "apibee-hmac-secret", message: X-Timestamp + "." + raw request body)</c>. An optional <c>sha256=</c> prefix is accepted.</para>
        /// <para>Bash: <c>SIG=$(printf '%s' "$TS.$BODY" | openssl dgst -sha256 -hmac apibee-hmac-secret | awk '{print $NF}')</c></para>
        /// </remarks>
        /// <response code="200">Signature valid.</response>
        /// <response code="401">Headers missing, timestamp not numeric or outside the tolerance, or signature mismatch.</response>
        [HttpPost("hmac")]
        public async Task<IActionResult> Hmac()
        {
            string body;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                body = await reader.ReadToEndAsync();

            var timestampHeader = Request.Headers["X-Timestamp"].ToString();
            var signatureHeader = Request.Headers["X-Signature"].ToString();

            if (timestampHeader == "" || signatureHeader == "")
                return Unauthorized(ApiResponse.Error(401, "Missing X-Timestamp and/or X-Signature header."));

            if (!long.TryParse(timestampHeader, out var timestamp))
                return Unauthorized(ApiResponse.Error(401, "X-Timestamp must be a Unix timestamp in seconds."));

            var skew = Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestamp);
            if (skew > HmacToleranceSeconds)
                return Unauthorized(ApiResponse.Error(401, $"X-Timestamp is {skew}s away from server time (tolerance {HmacToleranceSeconds}s)."));

            var signature = signatureHeader.Trim();
            if (signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
                signature = signature.Substring(7);

            var stringToSign = $"{timestampHeader}.{body}";
            var expected = Convert.ToHexString(
                HMACSHA256.HashData(Encoding.UTF8.GetBytes(HmacSecret), Encoding.UTF8.GetBytes(stringToSign))).ToLowerInvariant();

            if (!AuthHelpers.SecureEquals(signature.ToLowerInvariant(), expected))
                return Unauthorized(new
                {
                    status = 401,
                    error = "Unauthorized",
                    message = "Signature mismatch.",
                    hint = "X-Signature = lowercase hex HMAC-SHA256(secret, X-Timestamp + \".\" + raw request body). Optional 'sha256=' prefix.",
                    bodyLength = Encoding.UTF8.GetByteCount(body)
                });

            return Ok(new { authenticated = true, scheme = "hmac-sha256", timestamp, bodyLength = Encoding.UTF8.GetByteCount(body) });
        }

        // -------------------- ROLES: admin only --------------------
        /// <summary>Allow only tokens with the admin role (401 vs 403 demo).</summary>
        /// <remarks>
        /// Header: <c>Authorization: Bearer &lt;token&gt;</c> with one of the test tokens:
        /// <c>admin-token</c> (roles admin, user; scopes read, write, delete),
        /// <c>user-token</c> (role user; scopes read, write),
        /// <c>readonly-token</c> (no roles; scope read). Only <c>admin-token</c> passes.
        /// </remarks>
        /// <response code="200">Token has the admin role.</response>
        /// <response code="401">Token missing or not one of the test tokens.</response>
        /// <response code="403">Valid token without the admin role.</response>
        [HttpGet("roles/admin")]
        public IActionResult AdminOnly() => RequireRoleToken(t => t.Roles.Contains("admin"), "This endpoint requires the 'admin' role.");

        // -------------------- ROLES: user (or admin) --------------------
        /// <summary>Allow only tokens with the user role.</summary>
        /// <remarks>
        /// Header: <c>Authorization: Bearer &lt;token&gt;</c>. <c>admin-token</c> and <c>user-token</c> pass;
        /// <c>readonly-token</c> gets 403.
        /// </remarks>
        /// <response code="200">Token has the user role.</response>
        /// <response code="401">Token missing or not one of the test tokens.</response>
        /// <response code="403">Valid token without the user role.</response>
        [HttpGet("roles/user")]
        public IActionResult UserOnly() => RequireRoleToken(t => t.Roles.Contains("user"), "This endpoint requires the 'user' role.");

        // -------------------- SCOPES: delete --------------------
        /// <summary>Allow only tokens with the delete scope.</summary>
        /// <remarks>
        /// Header: <c>Authorization: Bearer &lt;token&gt;</c>. Only <c>admin-token</c> has the delete scope;
        /// <c>user-token</c> and <c>readonly-token</c> get 403.
        /// </remarks>
        /// <response code="200">Token has the delete scope.</response>
        /// <response code="401">Token missing or not one of the test tokens.</response>
        /// <response code="403">Valid token without the delete scope.</response>
        [HttpDelete("roles/resource")]
        public IActionResult DeleteResource() => RequireRoleToken(t => t.Scopes.Contains("delete"), "This endpoint requires the 'delete' scope.");

        // -------------------- CSRF: issue token --------------------
        /// <summary>Issue a CSRF token and set it as the XSRF-TOKEN cookie.</summary>
        /// <remarks>Send the same value back in the <c>X-CSRF-Token</c> header when calling <c>POST /api/auth/csrf/submit</c>.</remarks>
        /// <response code="200">Token issued; <c>Set-Cookie: XSRF-TOKEN=...</c>.</response>
        [HttpGet("csrf/token")]
        public IActionResult CsrfToken()
        {
            var token = AuthHelpers.RandomToken("csrf_");
            Response.Cookies.Append("XSRF-TOKEN", token, new CookieOptions { Path = "/", SameSite = SameSiteMode.Lax });
            return Ok(new
            {
                csrfToken = token,
                message = "Cookie XSRF-TOKEN set. POST /api/auth/csrf/submit with the same value in the X-CSRF-Token header."
            });
        }

        // -------------------- CSRF: protected submit (double-submit cookie) --------------------
        /// <summary>Submit a CSRF-protected form (double-submit cookie check).</summary>
        /// <remarks>Requires the <c>XSRF-TOKEN</c> cookie from <c>GET /api/auth/csrf/token</c> and a matching <c>X-CSRF-Token</c> header.</remarks>
        /// <response code="200">Cookie and header match.</response>
        /// <response code="403">Cookie or header missing, or they don't match.</response>
        [HttpPost("csrf/submit")]
        public IActionResult CsrfSubmit()
        {
            var cookie = Request.Cookies["XSRF-TOKEN"];
            var header = Request.Headers["X-CSRF-Token"].ToString();

            if (string.IsNullOrEmpty(cookie))
                return StatusCode(403, ApiResponse.Error(403, "Missing XSRF-TOKEN cookie. Call GET /api/auth/csrf/token first."));
            if (header == "")
                return StatusCode(403, ApiResponse.Error(403, "Missing X-CSRF-Token header."));
            if (!AuthHelpers.SecureEquals(cookie, header))
                return StatusCode(403, ApiResponse.Error(403, "X-CSRF-Token header does not match the XSRF-TOKEN cookie."));

            return Ok(new { message = "CSRF check passed. Form submitted.", submittedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") });
        }

        // -------------------- helpers --------------------
        private IActionResult BasicChallenge(string message)
        {
            Response.Headers.WWWAuthenticate = "Basic realm=\"apibee\", charset=\"UTF-8\"";
            return Unauthorized(ApiResponse.Error(401, message));
        }

        private IActionResult CheckApiKey(string key, string where)
        {
            if (string.IsNullOrEmpty(key))
                return Unauthorized(ApiResponse.Error(401, $"Missing API key. Send it in the {where}."));
            if (!AuthHelpers.SecureEquals(key, ApiKey))
                return StatusCode(403, ApiResponse.Error(403, "Invalid API key."));

            return Ok(new { authenticated = true, scheme = "api-key", location = where });
        }

        private IActionResult RequireRoleToken(Func<RoleToken, bool> allowed, string forbiddenMessage)
        {
            var token = AuthHelpers.GetBearer(Request);
            if (token == null || !RoleTokens.TryGetValue(token, out var info))
            {
                Response.Headers.WWWAuthenticate = "Bearer realm=\"apibee\"";
                return Unauthorized(ApiResponse.Error(401, "Send a valid token: Authorization: Bearer admin-token | user-token | readonly-token"));
            }

            if (!allowed(info))
                return StatusCode(403, new { status = 403, error = "Forbidden", message = forbiddenMessage, roles = info.Roles, scopes = info.Scopes });

            return Ok(new { authorized = true, user = info.Name, roles = info.Roles, scopes = info.Scopes });
        }

        private IActionResult DigestChallenge(string message, bool stale)
        {
            var opaque = Md5Hex("apibee-opaque");
            Response.Headers.WWWAuthenticate =
                $"Digest realm=\"{DigestRealm}\", qop=\"auth\", algorithm=MD5, nonce=\"{IssueNonce()}\", opaque=\"{opaque}\"" +
                (stale ? ", stale=true" : string.Empty);
            return Unauthorized(ApiResponse.Error(401, message));
        }

        private enum NonceState { Valid, Stale, Invalid }

        // Stateless nonce: base64("<unix seconds>:<hex hmac>") — verifiable without storing anything.
        private static string IssueNonce()
        {
            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var mac = Convert.ToHexString(HMACSHA256.HashData(NonceKey, Encoding.UTF8.GetBytes(ts)));
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ts}:{mac}"));
        }

        private static NonceState CheckNonce(string nonce)
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(nonce)).Split(':');
                if (decoded.Length != 2 || !long.TryParse(decoded[0], out var ts)) return NonceState.Invalid;

                var mac = Convert.ToHexString(HMACSHA256.HashData(NonceKey, Encoding.UTF8.GetBytes(decoded[0])));
                if (!AuthHelpers.SecureEquals(mac, decoded[1])) return NonceState.Invalid;

                return DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts > DigestNonceLifetimeSeconds
                    ? NonceState.Stale
                    : NonceState.Valid;
            }
            catch (FormatException)
            {
                return NonceState.Invalid;
            }
        }

        private static string Md5Hex(string input) =>
            Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }

    /// <summary>Small helpers shared by the auth test controllers.</summary>
    internal static class AuthHelpers
    {
        /// <summary>Constant-time string comparison for secrets.</summary>
        public static bool SecureEquals(string? a, string? b) =>
            a != null && b != null &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

        public static bool TryParseBasic(HttpRequest request, out string user, out string pass)
        {
            user = pass = string.Empty;
            var header = request.Headers.Authorization.ToString();
            if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) return false;

            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Substring(6).Trim()));
                var sep = decoded.IndexOf(':');
                if (sep < 0) return false;

                user = decoded.Substring(0, sep);
                pass = decoded.Substring(sep + 1);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static string? GetBearer(HttpRequest request)
        {
            var header = request.Headers.Authorization.ToString();
            if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;

            var token = header.Substring(7).Trim();
            return token == "" ? null : token;
        }

        public static string RandomToken(string prefix) =>
            prefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(20)).ToLowerInvariant();
    }
}
