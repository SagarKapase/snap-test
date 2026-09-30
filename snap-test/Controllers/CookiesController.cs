using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// Cookie inspection, setting (with every attribute), deletion, and a cookie-based login session flow.
    /// </summary>
    [ApiController]
    [Route("api/cookies")]
    public class CookiesController : ControllerBase
    {
        private const string SessionCookie = "apibee_session";
        private const int MaxCookiesPerRequest = 20;
        private const int MaxValueLength = 256;
        private const int MaxSessions = 10_000;
        private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(1);

        private static readonly Regex CookieName = new(@"^[A-Za-z0-9!#$%&'*+\-.^_`|~]{1,64}$", RegexOptions.Compiled);

        // Hardcoded accounts for the session flow.
        private static readonly Dictionary<string, (string Password, object Profile)> Accounts = new(StringComparer.OrdinalIgnoreCase)
        {
            ["test"] = ("test123", new { id = 1, username = "test", name = "Test User", email = "test@apibee.dev", role = "user" }),
            ["admin"] = ("admin123", new { id = 2, username = "admin", name = "Admin User", email = "admin@apibee.dev", role = "admin" })
        };

        private static readonly ConcurrentDictionary<string, (string Username, DateTime ExpiresAt)> Sessions = new();

        /// <summary>Credentials for the cookie session login.</summary>
        public class LoginBody
        {
            /// <summary>Account name: <c>test</c> or <c>admin</c> (case-insensitive).</summary>
            public string Username { get; set; } = string.Empty;
            /// <summary>Account password: <c>test123</c> or <c>admin123</c> (case-sensitive).</summary>
            public string Password { get; set; } = string.Empty;
        }

        // -------------------- LIST COOKIES --------------------
        /// <summary>Returns the cookies the client sent.</summary>
        /// <response code="200">Cookie count and a name-to-value map.</response>
        [HttpGet]
        public IActionResult List() => Ok(new
        {
            count = Request.Cookies.Count,
            cookies = Request.Cookies.ToDictionary(c => c.Key, c => c.Value)
        });

        // -------------------- SET (?name=value&name2=value2) --------------------
        /// <summary>Sets one cookie per query parameter.</summary>
        /// <remarks>
        /// Example: <c>/api/cookies/set?theme=dark&amp;lang=en</c> sets <c>theme</c> and <c>lang</c> with <c>Path=/</c>.
        /// At most 20 cookies; names must be RFC 6265 tokens (1-64 characters) and values at most 256 characters.
        /// </remarks>
        /// <response code="200">The cookies that were set.</response>
        /// <response code="400">No query parameters, too many cookies, or an invalid name or value.</response>
        [HttpGet("set")]
        public IActionResult SetMany()
        {
            if (Request.Query.Count == 0)
                return BadRequest(ApiResponse.Error(400, "Pass cookies as query parameters, e.g. /api/cookies/set?theme=dark&lang=en"));
            if (Request.Query.Count > MaxCookiesPerRequest)
                return BadRequest(ApiResponse.Error(400, $"At most {MaxCookiesPerRequest} cookies per request."));

            var set = new Dictionary<string, string>();
            foreach (var (name, values) in Request.Query)
            {
                var value = values.ToString();
                var invalid = ValidateCookie(name, value);
                if (invalid != null) return BadRequest(ApiResponse.Error(400, invalid));

                Response.Cookies.Append(name, value, new CookieOptions { Path = "/" });
                set[name] = value;
            }

            return Ok(new { message = $"{set.Count} cookie(s) set", set });
        }

        // -------------------- SET ONE (with attributes) --------------------
        /// <summary>Sets one cookie with explicit attributes.</summary>
        /// <param name="name">Cookie name: an RFC 6265 token, 1-64 characters.</param>
        /// <param name="value">Cookie value, at most 256 characters.</param>
        /// <param name="httpOnly">Adds <c>HttpOnly</c>. Default false.</param>
        /// <param name="secure">Adds <c>Secure</c>. Default false.</param>
        /// <param name="sameSite"><c>lax</c>, <c>strict</c>, <c>none</c> or <c>unspecified</c> (default, attribute omitted).</param>
        /// <param name="maxAge">Lifetime in seconds, clamped to 0-31536000. Omit for a session cookie; 0 expires it immediately.</param>
        /// <param name="path">Cookie path. Must start with <c>/</c>, at most 128 characters, no <c>;</c>. Default <c>/</c>.</param>
        /// <remarks>The response includes the exact <c>Set-Cookie</c> header, plus a warning when <c>SameSite=None</c> is used without <c>Secure</c>.</remarks>
        /// <response code="200">The cookie and its attributes.</response>
        /// <response code="400">Invalid name, value, path or sameSite.</response>
        [HttpGet("set/{name}/{value}")]
        public IActionResult SetOne(
            string name, string value,
            [FromQuery] bool httpOnly = false,
            [FromQuery] bool secure = false,
            [FromQuery] string? sameSite = null,
            [FromQuery] int? maxAge = null,
            [FromQuery] string path = "/")
        {
            var invalid = ValidateCookie(name, value);
            if (invalid != null) return BadRequest(ApiResponse.Error(400, invalid));

            if (!path.StartsWith('/') || path.Length > 128 || path.Any(c => char.IsControl(c) || c == ';'))
                return BadRequest(ApiResponse.Error(400, "path must start with '/' and be at most 128 characters."));

            var mode = (sameSite ?? "unspecified").ToLowerInvariant() switch
            {
                "lax" => SameSiteMode.Lax,
                "strict" => SameSiteMode.Strict,
                "none" => SameSiteMode.None,
                "unspecified" => SameSiteMode.Unspecified,
                _ => (SameSiteMode?)null
            };
            if (mode == null)
                return BadRequest(ApiResponse.Error(400, "sameSite must be one of: lax, strict, none, unspecified."));

            var options = new CookieOptions { Path = path, HttpOnly = httpOnly, Secure = secure, SameSite = mode.Value };
            if (maxAge.HasValue)
                options.MaxAge = TimeSpan.FromSeconds(Math.Clamp(maxAge.Value, 0, 31_536_000));

            Response.Cookies.Append(name, value, options);

            return Ok(new
            {
                message = $"Cookie '{name}' set",
                cookie = new
                {
                    name,
                    value,
                    path,
                    httpOnly,
                    secure,
                    sameSite = mode.Value.ToString().ToLowerInvariant(),
                    maxAge = options.MaxAge?.TotalSeconds
                },
                setCookieHeader = Response.Headers.SetCookie.ToString(),
                warning = mode == SameSiteMode.None && !secure ? "Browsers reject SameSite=None cookies without Secure." : null
            });
        }

        // -------------------- DELETE (?names=a,b — omit to delete all) --------------------
        /// <summary>Deletes cookies by name, or every cookie the client sent.</summary>
        /// <param name="names">Comma-separated cookie names, at most 20. Omit to delete all cookies in the request.</param>
        /// <remarks>Accepts GET and DELETE. Each cookie is expired with <c>Path=/</c>.</remarks>
        /// <response code="200">The names that were deleted.</response>
        /// <response code="400">Invalid name or more than 20 names.</response>
        [AcceptVerbs("GET", "DELETE", Route = "delete")]
        public IActionResult Delete([FromQuery] string? names)
        {
            var targets = string.IsNullOrWhiteSpace(names)
                ? Request.Cookies.Keys.ToList()
                : names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            if (targets.Count > MaxCookiesPerRequest)
                return BadRequest(ApiResponse.Error(400, $"At most {MaxCookiesPerRequest} cookies per request."));
            if (targets.Any(n => !CookieName.IsMatch(n)))
                return BadRequest(ApiResponse.Error(400, "Cookie names may only contain RFC 6265 token characters."));

            foreach (var name in targets)
                Response.Cookies.Delete(name, new CookieOptions { Path = "/" });

            return Ok(new { message = $"{targets.Count} cookie(s) deleted", deleted = targets });
        }

        // -------------------- SESSION: LOGIN --------------------
        // Accounts: test / test123 (user), admin / admin123 (admin)
        /// <summary>Logs in and sets the <c>apibee_session</c> cookie.</summary>
        /// <param name="body">Username and password.</param>
        /// <remarks>
        /// Test accounts: <c>test</c> / <c>test123</c> (role user) and <c>admin</c> / <c>admin123</c> (role admin).
        /// The cookie is HttpOnly, SameSite=Lax, Path=/, valid for one hour, and Secure only over HTTPS.
        /// </remarks>
        /// <response code="200">Logged in; the session cookie is set.</response>
        /// <response code="400">Missing or malformed body.</response>
        /// <response code="401">Wrong username or password.</response>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginBody body)
        {
            if (!Accounts.TryGetValue(body.Username ?? string.Empty, out var account) || account.Password != body.Password)
                return Unauthorized(ApiResponse.Error(401, "Invalid username or password. Try test / test123."));

            PurgeSessions();

            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            var expiresAt = DateTime.UtcNow.Add(SessionLifetime);
            Sessions[token] = (body.Username!.ToLowerInvariant(), expiresAt);

            Response.Cookies.Append(SessionCookie, token, new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                MaxAge = SessionLifetime
            });

            return Ok(new
            {
                message = "Logged in. The session cookie is sent automatically on later requests.",
                username = body.Username!.ToLowerInvariant(),
                cookie = SessionCookie,
                expiresAt = expiresAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            });
        }

        // -------------------- SESSION: CURRENT USER --------------------
        /// <summary>Returns the profile of the user who owns the session cookie.</summary>
        /// <remarks>Reads the <c>apibee_session</c> cookie set by <c>POST /api/cookies/login</c>.</remarks>
        /// <response code="200">User profile and session expiry.</response>
        /// <response code="401">No cookie, unknown or expired session.</response>
        [HttpGet("me")]
        public IActionResult Me()
        {
            var token = Request.Cookies[SessionCookie];
            if (string.IsNullOrEmpty(token) || !Sessions.TryGetValue(token, out var session))
                return Unauthorized(ApiResponse.Error(401, $"Not logged in. POST /api/cookies/login first to get the '{SessionCookie}' cookie."));

            if (session.ExpiresAt < DateTime.UtcNow)
            {
                Sessions.TryRemove(token, out _);
                return Unauthorized(ApiResponse.Error(401, "Session expired. Log in again."));
            }

            return Ok(new
            {
                user = Accounts[session.Username].Profile,
                session = new { expiresAt = session.ExpiresAt.ToString("yyyy-MM-ddTHH:mm:ssZ") }
            });
        }

        // -------------------- SESSION: LOGOUT --------------------
        /// <summary>Ends the session and expires the session cookie.</summary>
        /// <remarks>The session is invalidated on the server, so replaying the old cookie fails.</remarks>
        /// <response code="200">Logged out (also when there was no session).</response>
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            var token = Request.Cookies[SessionCookie];
            if (!string.IsNullOrEmpty(token)) Sessions.TryRemove(token, out _);

            Response.Cookies.Delete(SessionCookie, new CookieOptions { Path = "/" });
            return Ok(new { message = "Logged out" });
        }

        private static string? ValidateCookie(string name, string value)
        {
            if (!CookieName.IsMatch(name))
                return $"Invalid cookie name '{name}'. Use 1-64 RFC 6265 token characters.";
            if (value.Length > MaxValueLength)
                return $"Cookie value for '{name}' exceeds {MaxValueLength} characters.";
            return null;
        }

        private static void PurgeSessions()
        {
            var now = DateTime.UtcNow;
            foreach (var (token, session) in Sessions)
                if (session.ExpiresAt < now) Sessions.TryRemove(token, out _);

            if (Sessions.Count >= MaxSessions) Sessions.Clear();
        }
    }
}
