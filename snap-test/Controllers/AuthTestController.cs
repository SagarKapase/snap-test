using Microsoft.AspNetCore.Mvc;

namespace snap_test.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthTestController : ControllerBase
    {
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
