using Microsoft.AspNetCore.Mvc;

namespace snap_test.Controllers
{
    /// <summary>
    /// Auth test: an endpoint protected by HTTP Basic auth.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthTestController : ControllerBase
    {
        /// <summary>Return protected data behind Basic auth.</summary>
        /// <remarks>
        /// Credentials: <c>Admin</c> / <c>Admin@1234</c>, sent as <c>Authorization: Basic QWRtaW46QWRtaW5AMTIzNA==</c>.
        /// </remarks>
        /// <response code="200">Protected data.</response>
        /// <response code="401">Header missing or wrong credentials (empty body).</response>
        [HttpGet("secure-data")]
        [BasicAuth]
        public IActionResult GetSecureData()
        {
            return Ok(new
            {
                message = "Authentication successful",
                data = "This is protected data"
            });
        }
    }

}
