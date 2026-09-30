using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using snap_test.Models;
using System.Security.Claims;

namespace snap_test.Controllers
{
    /// <summary>
    /// Admin: an endpoint protected by a JWT with the Admin role.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        //For admin Only
        /// <summary>Greet an authenticated admin.</summary>
        /// <remarks>
        /// Get a token from <c>POST /api/User/Login</c> (<c>Michael</c> / <c>Thompson</c>) and send
        /// <c>Authorization: Bearer &lt;token&gt;</c>. The role check is case-sensitive: tokens from
        /// <c>POST /api/auth/jwt/login</c> carry the lowercase role admin and get 403 here.
        /// </remarks>
        /// <response code="200">Greeting text.</response>
        /// <response code="401">Token missing, invalid or expired.</response>
        /// <response code="403">Valid token without the Admin role.</response>
        [HttpGet("authorize")]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminEndPoint()
        {
            var currentUser = GetCurrentUser();
            return Ok($"Hi, I’m the {currentUser.Role} — how can I help you today?");
        }
        private UserAuth GetCurrentUser()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                var userClaims = identity.Claims;
                return new UserAuth
                {
                    Username = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value,
                    Role = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value
                };
            }
            return null;
        }
    }
}
