using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// JWT: log in for a signed JWT, call endpoints protected by <c>[Authorize]</c> and a role check, rotate
    /// refresh tokens, and fetch an expired token for negative tests.
    /// </summary>
    [ApiController]
    [Route("api/auth/jwt")]
    public class JwtAuthController : ControllerBase
    {
        private const int AccessTokenMinutes = 15;
        private const int MaxRefreshTokens = 5000;

        private record JwtUser(string Username, string Password, string Role, string Name, string Email);

        private static readonly Dictionary<string, JwtUser> Users = new(StringComparer.OrdinalIgnoreCase)
        {
            ["admin"] = new("admin", "admin123", "admin", "Alice Admin", "admin@example.com"),
            ["user"] = new("user", "user123", "user", "Uma User", "user@example.com")
        };

        private static readonly object Sync = new();
        private static readonly Dictionary<string, (string Username, DateTime ExpiresAt)> RefreshTokens = new();

        private readonly IConfiguration _config;

        public JwtAuthController(IConfiguration config) => _config = config;

        /// <summary>Login credentials.</summary>
        public class LoginRequest
        {
            /// <summary>Username (case-insensitive): <c>admin</c> or <c>user</c>.</summary>
            public string Username { get; set; } = string.Empty;
            /// <summary>Password (case-sensitive): <c>admin123</c> or <c>user123</c>.</summary>
            public string Password { get; set; } = string.Empty;
        }

        /// <summary>Refresh request.</summary>
        public class RefreshRequest
        {
            /// <summary>The refreshToken from login or a previous refresh.</summary>
            public string RefreshToken { get; set; } = string.Empty;
        }

        // -------------------- LOGIN --------------------
        /// <summary>Log in and receive a JWT access token and a refresh token.</summary>
        /// <param name="request">Username and password.</param>
        /// <remarks>
        /// Users: <c>admin</c> / <c>admin123</c> (role admin), <c>user</c> / <c>user123</c> (role user).
        /// Access tokens expire after 15 minutes. Send them as <c>Authorization: Bearer &lt;accessToken&gt;</c>.
        /// </remarks>
        /// <response code="200">accessToken, tokenType, expiresIn, refreshToken and user.</response>
        /// <response code="400">username or password missing.</response>
        /// <response code="401">Wrong username or password.</response>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(ApiResponse.Error(400, "username and password are required."));

            if (!Users.TryGetValue(request.Username, out var user) || !AuthHelpers.SecureEquals(request.Password, user.Password))
                return Unauthorized(ApiResponse.Error(401, "Invalid username or password."));

            return Ok(IssueTokens(user));
        }

        // -------------------- REFRESH (rotates the refresh token) --------------------
        /// <summary>Exchange a refresh token for new tokens.</summary>
        /// <param name="request">The refresh token to use.</param>
        /// <remarks>Refresh tokens are single use: each call returns a new one and the old one stops working.</remarks>
        /// <response code="200">New access and refresh tokens.</response>
        /// <response code="400">refreshToken missing.</response>
        /// <response code="401">Refresh token invalid, expired or already used.</response>
        [HttpPost("refresh")]
        public IActionResult Refresh([FromBody] RefreshRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest(ApiResponse.Error(400, "refreshToken is required."));

            (string Username, DateTime ExpiresAt) entry;
            bool found;
            lock (Sync)
            {
                found = RefreshTokens.TryGetValue(request.RefreshToken, out entry);
                if (found) RefreshTokens.Remove(request.RefreshToken);
            }

            if (!found || entry.ExpiresAt < DateTime.UtcNow || !Users.TryGetValue(entry.Username, out var user))
                return Unauthorized(ApiResponse.Error(401, "Refresh token is invalid, expired or already used."));

            return Ok(IssueTokens(user));
        }

        // -------------------- ME (any valid token) --------------------
        /// <summary>Return the claims of the current JWT.</summary>
        /// <remarks>Header: <c>Authorization: Bearer &lt;accessToken&gt;</c> from <c>POST /api/auth/jwt/login</c>.</remarks>
        /// <response code="200">Username, role, profile and all claims.</response>
        /// <response code="401">Token missing, invalid or expired.</response>
        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var username = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            Users.TryGetValue(username, out var user);

            return Ok(new
            {
                username,
                role = User.FindFirstValue(ClaimTypes.Role),
                name = user?.Name,
                email = user?.Email,
                claims = User.Claims.Select(c => new { type = c.Type, value = c.Value })
            });
        }

        // -------------------- ADMIN (role = admin; user token gets 403) --------------------
        /// <summary>Allow only JWTs with the admin role.</summary>
        /// <remarks>Log in as <c>admin</c> / <c>admin123</c>. A token for <c>user</c> is authenticated but gets 403.</remarks>
        /// <response code="200">Token has the admin role.</response>
        /// <response code="401">Token missing, invalid or expired.</response>
        /// <response code="403">Valid token without the admin role.</response>
        [HttpGet("admin")]
        [Authorize(Roles = "admin")]
        public IActionResult Admin() => Ok(new
        {
            message = "Welcome, admin. You passed the role check.",
            username = User.FindFirstValue(ClaimTypes.NameIdentifier)
        });

        // -------------------- EXPIRED TOKEN (for negative tests) --------------------
        /// <summary>Return a correctly signed JWT that expired an hour ago.</summary>
        /// <remarks>Use it against <c>GET /api/auth/jwt/me</c> to test how a client handles 401 for expired tokens.</remarks>
        /// <response code="200">The expired token.</response>
        [HttpGet("expired")]
        public IActionResult Expired()
        {
            var now = DateTime.UtcNow;
            // Expired an hour ago — well past the JwtBearer default 5-minute clock skew.
            var token = CreateJwt(Users["user"], notBefore: now.AddHours(-2), expires: now.AddHours(-1));

            return Ok(new
            {
                accessToken = token,
                tokenType = "Bearer",
                expiredAt = now.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                message = "Correctly signed but expired. GET /api/auth/jwt/me with it should return 401."
            });
        }

        // -------------------- helpers --------------------
        private object IssueTokens(JwtUser user)
        {
            var now = DateTime.UtcNow;
            var accessToken = CreateJwt(user, now, now.AddMinutes(AccessTokenMinutes));
            var refreshToken = AuthHelpers.RandomToken("jrt_");

            lock (Sync)
            {
                foreach (var k in RefreshTokens.Where(kv => kv.Value.ExpiresAt < now).Select(kv => kv.Key).ToList())
                    RefreshTokens.Remove(k);
                if (RefreshTokens.Count >= MaxRefreshTokens)
                    foreach (var k in RefreshTokens.OrderBy(kv => kv.Value.ExpiresAt).Take(RefreshTokens.Count - MaxRefreshTokens + 1).Select(kv => kv.Key).ToList())
                        RefreshTokens.Remove(k);

                RefreshTokens[refreshToken] = (user.Username, now.AddDays(7));
            }

            return new
            {
                accessToken,
                tokenType = "Bearer",
                expiresIn = AccessTokenMinutes * 60,
                refreshToken,
                user = new { username = user.Username, role = user.Role, name = user.Name }
            };
        }

        // Same claim shape as UserController.GenerateToken (NameIdentifier + Role).
        private string CreateJwt(JwtUser user, DateTime notBefore, DateTime expires)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(_config["Jwt:Issuer"], _config["Jwt:Audience"], claims,
                notBefore: notBefore, expires: expires, signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
