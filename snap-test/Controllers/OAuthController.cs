using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// OAuth 2.0: an in-memory authorization server for testing client flows. Supports the authorization code grant
    /// with PKCE, client credentials, password and refresh token grants, plus a protected resource, userinfo,
    /// introspection (RFC 7662) and revocation (RFC 7009). Authorization is auto-approved as user apibee.
    /// </summary>
    [ApiController]
    [Route("api/auth/oauth")]
    public class OAuthController : ControllerBase
    {
        private const int AccessTokenLifetimeSeconds = 3600;
        private const int CodeLifetimeSeconds = 600;
        private const int MaxStoreSize = 5000;

        private record OAuthClient(string Id, string? Secret, string[] Scopes);
        private record OAuthUser(string Username, string Password, string Name, string Email);

        private class AuthCode
        {
            public string ClientId = "";
            public string? RedirectUri;
            public string Scope = "";
            public string Username = "";
            public string? Challenge;
            public string Method = "plain";
            public DateTime ExpiresAt;
        }

        private class IssuedToken
        {
            public string ClientId = "";
            public string? Username;
            public string Scope = "";
            public string Type = "access_token";
            public DateTime IssuedAt;
            public DateTime ExpiresAt;
        }

        private static readonly Dictionary<string, OAuthClient> Clients = new()
        {
            ["apibee-client"] = new("apibee-client", "apibee-secret", new[] { "read", "write", "profile", "email" }),
            ["apibee-public"] = new("apibee-public", null, new[] { "read", "profile", "email" })
        };

        private static readonly Dictionary<string, OAuthUser> Users = new()
        {
            ["apibee"] = new("apibee", "password123", "APIBee Tester", "tester@apibee.dev"),
            ["jane"] = new("jane", "jane123", "Jane Doe", "jane.doe@apibee.dev")
        };

        // Exact-match redirect URIs, plus any port on localhost / 127.0.0.1 (see IsAllowedRedirect).
        private static readonly HashSet<string> AllowedRedirects = new(StringComparer.OrdinalIgnoreCase)
        {
            "https://oauth.pstmn.io/v1/callback",
            "https://oauth.pstmn.io/v1/browser-callback",
            "https://www.getpostman.com/oauth2/callback"
        };

        private static readonly object Sync = new();
        private static readonly Dictionary<string, AuthCode> Codes = new();
        private static readonly Dictionary<string, IssuedToken> Tokens = new();

        // -------------------- AUTHORIZE (auto-approves as "apibee") --------------------
        /// <summary>Start the authorization code flow and issue a code (auto-approved as user apibee).</summary>
        /// <param name="responseType">Must be <c>code</c>.</param>
        /// <param name="clientId"><c>apibee-client</c> (confidential) or <c>apibee-public</c> (public; PKCE required).</param>
        /// <param name="redirectUri">Where to send the code. Only http(s) URIs on localhost or 127.0.0.1 (any port) and the Postman callbacks are redirected to; any other value gets the code back as JSON.</param>
        /// <param name="scope">Space-separated subset of the client's scopes (apibee-client: read write profile email; apibee-public: read profile email). Defaults to all of them.</param>
        /// <param name="state">Opaque value echoed back unchanged.</param>
        /// <param name="codeChallenge">PKCE challenge: <c>BASE64URL(SHA256(code_verifier))</c> for S256, or the verifier itself for plain.</param>
        /// <param name="codeChallengeMethod"><c>S256</c> or <c>plain</c> (default plain).</param>
        /// <remarks>Codes are single use and expire after 10 minutes. Exchange them at <c>POST /api/auth/oauth/token</c>.</remarks>
        /// <response code="302">Redirect to redirect_uri with <c>code</c> and <c>state</c>.</response>
        /// <response code="200">Code returned as JSON (no redirect_uri, or one that isn't allowlisted).</response>
        /// <response code="400">invalid_client, unsupported_response_type, invalid_request or invalid_scope.</response>
        [HttpGet("authorize")]
        public IActionResult Authorize(
            [FromQuery(Name = "response_type")] string? responseType,
            [FromQuery(Name = "client_id")] string? clientId,
            [FromQuery(Name = "redirect_uri")] string? redirectUri,
            [FromQuery] string? scope,
            [FromQuery] string? state,
            [FromQuery(Name = "code_challenge")] string? codeChallenge,
            [FromQuery(Name = "code_challenge_method")] string? codeChallengeMethod)
        {
            if (clientId == null || !Clients.TryGetValue(clientId, out var client))
                return OAuthError(400, "invalid_client", "Unknown client_id. Use apibee-client or apibee-public.");

            if (responseType != "code")
                return OAuthError(400, "unsupported_response_type", "Only response_type=code is supported.");

            var method = string.IsNullOrEmpty(codeChallengeMethod) ? "plain" : codeChallengeMethod;
            if (codeChallenge != null && method != "S256" && method != "plain")
                return OAuthError(400, "invalid_request", "code_challenge_method must be S256 or plain.");

            if (client.Secret == null && string.IsNullOrEmpty(codeChallenge))
                return OAuthError(400, "invalid_request", "Public clients must use PKCE (code_challenge).");

            var grantedScope = NormalizeScope(scope, client);
            if (grantedScope == null)
                return OAuthError(400, "invalid_scope", $"Allowed scopes for this client: {string.Join(' ', client.Scopes)}");

            var code = AuthHelpers.RandomToken("code_");
            lock (Sync)
            {
                Prune();
                Codes[code] = new AuthCode
                {
                    ClientId = client.Id,
                    RedirectUri = redirectUri,
                    Scope = grantedScope,
                    Username = "apibee",
                    Challenge = codeChallenge,
                    Method = method,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(CodeLifetimeSeconds)
                };
            }

            if (redirectUri != null && (IsAllowedRedirect(redirectUri) || IsOwnSwaggerRedirect(redirectUri)))
            {
                var sep = redirectUri.Contains('?') ? "&" : "?";
                var location = $"{redirectUri}{sep}code={Uri.EscapeDataString(code)}" +
                               (state != null ? $"&state={Uri.EscapeDataString(state)}" : string.Empty);
                return Redirect(location);
            }

            return Ok(new
            {
                code,
                state,
                redirect_uri = redirectUri,
                expires_in = CodeLifetimeSeconds,
                message = redirectUri == null
                    ? "No redirect_uri given — exchange this code at POST /api/auth/oauth/token."
                    : "redirect_uri is not on the allowlist (localhost, 127.0.0.1, Postman callbacks), so the code is returned as JSON."
            });
        }

        // -------------------- TOKEN (form-urlencoded or JSON) --------------------
        /// <summary>Exchange a grant for an access token.</summary>
        /// <remarks>
        /// Body: <c>application/x-www-form-urlencoded</c> or JSON.
        /// Client auth: <c>Authorization: Basic base64(client_id:client_secret)</c>, or <c>client_id</c> and <c>client_secret</c> in the body.
        /// Clients: <c>apibee-client</c> / <c>apibee-secret</c> (confidential), <c>apibee-public</c> (public, no secret).
        /// Users: <c>apibee</c> / <c>password123</c>, <c>jane</c> / <c>jane123</c>.
        /// <para>grant_type values:</para>
        /// <para><c>client_credentials</c>: optional <c>scope</c>; confidential client only; no refresh token.</para>
        /// <para><c>password</c>: <c>username</c>, <c>password</c>, optional <c>scope</c>.</para>
        /// <para><c>authorization_code</c>: <c>code</c>, <c>redirect_uri</c> (if one was sent to /authorize), <c>code_verifier</c> (if PKCE was used).</para>
        /// <para><c>refresh_token</c>: <c>refresh_token</c>. Refresh tokens rotate: each use returns a new one and the old one stops working.</para>
        /// <para>Access tokens expire after 3600 seconds.</para>
        /// </remarks>
        /// <response code="200">access_token, token_type, expires_in, refresh_token (not for client_credentials) and scope.</response>
        /// <response code="400">invalid_request, invalid_grant, invalid_scope, unauthorized_client or unsupported_grant_type.</response>
        /// <response code="401">invalid_client: unknown client or wrong secret.</response>
        [HttpPost("token")]
        public async Task<IActionResult> Token()
        {
            Response.Headers.CacheControl = "no-store";
            Response.Headers.Pragma = "no-cache";

            var p = await ReadParamsAsync();
            string? P(string key) => p.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v) ? v : null;

            // Client authentication: HTTP Basic header takes precedence over body params.
            var usedBasic = AuthHelpers.TryParseBasic(Request, out var basicId, out var basicSecret);
            var clientId = usedBasic ? Uri.UnescapeDataString(basicId) : P("client_id");
            var clientSecret = usedBasic ? Uri.UnescapeDataString(basicSecret) : P("client_secret");

            if (clientId == null || !Clients.TryGetValue(clientId, out var client))
                return InvalidClient(usedBasic, "Unknown or missing client_id.");

            if (client.Secret != null && !AuthHelpers.SecureEquals(clientSecret, client.Secret))
                return InvalidClient(usedBasic, "Invalid client_secret.");

            switch (P("grant_type"))
            {
                case "client_credentials":
                {
                    if (client.Secret == null)
                        return OAuthError(400, "unauthorized_client", "Public clients cannot use client_credentials.");

                    var scope = NormalizeScope(P("scope"), client);
                    if (scope == null) return OAuthError(400, "invalid_scope", $"Allowed scopes: {string.Join(' ', client.Scopes)}");

                    return Ok(IssueTokens(client.Id, null, scope, withRefresh: false));
                }

                case "password":
                {
                    var username = P("username");
                    var password = P("password");
                    if (username == null || password == null)
                        return OAuthError(400, "invalid_request", "username and password are required.");

                    if (!Users.TryGetValue(username, out var user) || !AuthHelpers.SecureEquals(password, user.Password))
                        return OAuthError(400, "invalid_grant", "Invalid username or password.");

                    var scope = NormalizeScope(P("scope"), client);
                    if (scope == null) return OAuthError(400, "invalid_scope", $"Allowed scopes: {string.Join(' ', client.Scopes)}");

                    return Ok(IssueTokens(client.Id, user.Username, scope, withRefresh: true));
                }

                case "authorization_code":
                {
                    var code = P("code");
                    if (code == null) return OAuthError(400, "invalid_request", "code is required.");

                    AuthCode? entry;
                    lock (Sync)
                    {
                        // Codes are single use: remove on first presentation, valid or not.
                        if (Codes.TryGetValue(code, out entry)) Codes.Remove(code);
                    }

                    if (entry == null || entry.ExpiresAt < DateTime.UtcNow)
                        return OAuthError(400, "invalid_grant", "Authorization code is invalid, expired or already used.");
                    if (entry.ClientId != client.Id)
                        return OAuthError(400, "invalid_grant", "Authorization code was issued to a different client.");
                    if (entry.RedirectUri != null && entry.RedirectUri != P("redirect_uri"))
                        return OAuthError(400, "invalid_grant", "redirect_uri does not match the one used at /authorize.");

                    if (entry.Challenge != null)
                    {
                        var verifier = P("code_verifier");
                        if (verifier == null)
                            return OAuthError(400, "invalid_request", "code_verifier is required (PKCE was used at /authorize).");

                        var computed = entry.Method == "S256"
                            ? Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
                            : verifier;

                        if (!AuthHelpers.SecureEquals(computed, entry.Challenge))
                            return OAuthError(400, "invalid_grant", "PKCE verification failed: code_verifier does not match code_challenge.");
                    }

                    return Ok(IssueTokens(client.Id, entry.Username, entry.Scope, withRefresh: true));
                }

                case "refresh_token":
                {
                    var refresh = P("refresh_token");
                    if (refresh == null) return OAuthError(400, "invalid_request", "refresh_token is required.");

                    IssuedToken? old;
                    lock (Sync)
                    {
                        // Rotation: the presented refresh token is consumed.
                        if (Tokens.TryGetValue(refresh, out old) && old.Type == "refresh_token") Tokens.Remove(refresh);
                        else old = null;
                    }

                    if (old == null || old.ExpiresAt < DateTime.UtcNow)
                        return OAuthError(400, "invalid_grant", "Refresh token is invalid, expired or already used.");
                    if (old.ClientId != client.Id)
                        return OAuthError(400, "invalid_grant", "Refresh token was issued to a different client.");

                    return Ok(IssueTokens(client.Id, old.Username, old.Scope, withRefresh: true));
                }

                case null:
                    return OAuthError(400, "invalid_request", "grant_type is required.");

                default:
                    return OAuthError(400, "unsupported_grant_type",
                        "Supported: authorization_code, client_credentials, password, refresh_token.");
            }
        }

        // -------------------- PROTECTED RESOURCE --------------------
        /// <summary>Return a resource that requires an OAuth access token.</summary>
        /// <remarks>Header: <c>Authorization: Bearer &lt;access_token&gt;</c> from <c>POST /api/auth/oauth/token</c>.</remarks>
        /// <response code="200">Token valid; returns its client, subject, scope and remaining lifetime.</response>
        /// <response code="401">Token missing, invalid, revoked or expired.</response>
        [HttpGet("protected")]
        public IActionResult Protected()
        {
            var (token, fail) = RequireAccessToken();
            if (fail != null) return fail;

            return Ok(new
            {
                message = "You accessed an OAuth-protected resource.",
                client_id = token!.ClientId,
                sub = token.Username ?? token.ClientId,
                scope = token.Scope,
                expires_in = (int)(token.ExpiresAt - DateTime.UtcNow).TotalSeconds
            });
        }

        // -------------------- USERINFO --------------------
        /// <summary>Return the profile of the user behind an access token.</summary>
        /// <remarks>Header: <c>Authorization: Bearer &lt;access_token&gt;</c>. The email is only included when the token has the email scope.</remarks>
        /// <response code="200">User profile.</response>
        /// <response code="401">Token missing, invalid, revoked or expired.</response>
        /// <response code="403">client_credentials tokens have no user.</response>
        [HttpGet("userinfo")]
        public IActionResult UserInfo()
        {
            var (token, fail) = RequireAccessToken();
            if (fail != null) return fail;

            if (token!.Username == null || !Users.TryGetValue(token.Username, out var user))
                return StatusCode(403, new { error = "insufficient_scope", error_description = "client_credentials tokens have no user." });

            var scopes = token.Scope.Split(' ');
            return Ok(new
            {
                sub = user.Username,
                name = user.Name,
                preferred_username = user.Username,
                email = scopes.Contains("email") ? user.Email : null,
                email_verified = scopes.Contains("email") ? true : (bool?)null
            });
        }

        // -------------------- INTROSPECT (RFC 7662) --------------------
        /// <summary>Report whether a token is active (RFC 7662).</summary>
        /// <remarks>Body (form or JSON): <c>token</c>. Unknown, expired and revoked tokens return <c>{"active": false}</c>.</remarks>
        /// <response code="200">Token status and, when active, its scope, client, subject and expiry.</response>
        [HttpPost("introspect")]
        public async Task<IActionResult> Introspect()
        {
            var p = await ReadParamsAsync();
            p.TryGetValue("token", out var value);

            IssuedToken? token = null;
            if (!string.IsNullOrEmpty(value))
                lock (Sync) Tokens.TryGetValue(value, out token);

            if (token == null || token.ExpiresAt < DateTime.UtcNow)
                return Ok(new { active = false });

            return Ok(new
            {
                active = true,
                scope = token.Scope,
                client_id = token.ClientId,
                username = token.Username,
                sub = token.Username ?? token.ClientId,
                token_type = token.Type == "access_token" ? "Bearer" : "refresh_token",
                iat = new DateTimeOffset(token.IssuedAt).ToUnixTimeSeconds(),
                exp = new DateTimeOffset(token.ExpiresAt).ToUnixTimeSeconds()
            });
        }

        // -------------------- REVOKE (RFC 7009: always 200) --------------------
        /// <summary>Revoke an access or refresh token (RFC 7009).</summary>
        /// <remarks>Body (form or JSON): <c>token</c>. Returns 200 even for unknown tokens.</remarks>
        /// <response code="200">Revoked, or already unknown.</response>
        /// <response code="400">token missing.</response>
        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke()
        {
            var p = await ReadParamsAsync();
            if (!p.TryGetValue("token", out var value) || string.IsNullOrEmpty(value))
                return OAuthError(400, "invalid_request", "token is required.");

            bool removed;
            lock (Sync) removed = Tokens.Remove(value);

            return Ok(new { revoked = removed, message = removed ? "Token revoked." : "Token unknown or already revoked." });
        }

        // -------------------- helpers --------------------
        private object IssueTokens(string clientId, string? username, string scope, bool withRefresh)
        {
            var now = DateTime.UtcNow;
            var access = AuthHelpers.RandomToken("oat_");
            string? refresh = withRefresh ? AuthHelpers.RandomToken("ort_") : null;

            lock (Sync)
            {
                Prune();
                Tokens[access] = new IssuedToken
                {
                    ClientId = clientId, Username = username, Scope = scope,
                    IssuedAt = now, ExpiresAt = now.AddSeconds(AccessTokenLifetimeSeconds)
                };
                if (refresh != null)
                    Tokens[refresh] = new IssuedToken
                    {
                        ClientId = clientId, Username = username, Scope = scope, Type = "refresh_token",
                        IssuedAt = now, ExpiresAt = now.AddDays(1)
                    };
            }

            var response = new Dictionary<string, object>
            {
                ["access_token"] = access,
                ["token_type"] = "Bearer",
                ["expires_in"] = AccessTokenLifetimeSeconds
            };
            if (refresh != null) response["refresh_token"] = refresh;
            response["scope"] = scope;
            return response;
        }

        private (IssuedToken? token, IActionResult? fail) RequireAccessToken()
        {
            var value = AuthHelpers.GetBearer(Request);
            IssuedToken? token = null;
            if (value != null)
                lock (Sync) Tokens.TryGetValue(value, out token);

            if (token == null || token.Type != "access_token" || token.ExpiresAt < DateTime.UtcNow)
            {
                Response.Headers.WWWAuthenticate = value == null
                    ? "Bearer realm=\"apibee\""
                    : "Bearer realm=\"apibee\", error=\"invalid_token\", error_description=\"The access token is invalid or expired\"";
                return (null, Unauthorized(new { error = "invalid_token", error_description = "Missing, invalid, revoked or expired access token." }));
            }

            return (token, null);
        }

        // Requested scopes must be a subset of the client's; empty request => all of them.
        private static string? NormalizeScope(string? requested, OAuthClient client)
        {
            if (string.IsNullOrWhiteSpace(requested)) return string.Join(' ', client.Scopes);

            var scopes = requested.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
            return scopes.All(client.Scopes.Contains) ? string.Join(' ', scopes) : null;
        }

        // Swagger UI on this same host (/swagger/oauth2-redirect.html) - same origin, so not an open redirect.
        private bool IsOwnSwaggerRedirect(string uri) =>
            string.Equals(uri, $"{Request.Scheme}://{Request.Host}/swagger/oauth2-redirect.html", StringComparison.OrdinalIgnoreCase);

        private static bool IsAllowedRedirect(string uri)
        {
            if (AllowedRedirects.Contains(uri)) return true;
            return Uri.TryCreate(uri, UriKind.Absolute, out var u) &&
                   (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps) &&
                   (u.Host == "localhost" || u.Host == "127.0.0.1");
        }

        private async Task<Dictionary<string, string>> ReadParamsAsync()
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);

            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync();
                foreach (var kv in form) result[kv.Key] = kv.Value.ToString();
            }
            else if (Request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
            {
                try
                {
                    using var doc = await JsonDocument.ParseAsync(Request.Body);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                        foreach (var prop in doc.RootElement.EnumerateObject())
                            result[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                                ? prop.Value.GetString() ?? ""
                                : prop.Value.GetRawText();
                }
                catch (JsonException)
                {
                    // Malformed JSON: treat as no parameters; the grant checks report what is missing.
                }
            }

            return result;
        }

        // Drop expired entries and cap store sizes (oldest first) so memory stays bounded.
        private static void Prune()
        {
            var now = DateTime.UtcNow;
            foreach (var k in Codes.Where(kv => kv.Value.ExpiresAt < now).Select(kv => kv.Key).ToList()) Codes.Remove(k);
            foreach (var k in Tokens.Where(kv => kv.Value.ExpiresAt < now).Select(kv => kv.Key).ToList()) Tokens.Remove(k);

            if (Codes.Count >= MaxStoreSize)
                foreach (var k in Codes.OrderBy(kv => kv.Value.ExpiresAt).Take(Codes.Count - MaxStoreSize + 1).Select(kv => kv.Key).ToList())
                    Codes.Remove(k);
            if (Tokens.Count >= MaxStoreSize)
                foreach (var k in Tokens.OrderBy(kv => kv.Value.IssuedAt).Take(Tokens.Count - MaxStoreSize + 2).Select(kv => kv.Key).ToList())
                    Tokens.Remove(k);
        }

        private IActionResult InvalidClient(bool usedBasic, string description)
        {
            if (usedBasic) Response.Headers.WWWAuthenticate = "Basic realm=\"apibee\"";
            return OAuthError(401, "invalid_client", description);
        }

        private IActionResult OAuthError(int status, string error, string description) =>
            StatusCode(status, new { error, error_description = description });

        private static string Base64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
