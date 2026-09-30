using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using snap_test.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace snap_test.Controllers
{
    /// <summary>
    /// Users: the original JSON CRUD over 10 hardcoded users (IDs 101-110), plus the JWT login used by the Admin endpoint.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _config;

        public UserController(IConfiguration config)
        {
            _config = config;
        }
        // Hardcoded in-memory list
        //private static List<User> users = new List<User>
        //{
        //    new User { UserId = Guid.NewGuid(), Name = "Sagar", Email = "sagar@example.com", Job = "Developer", City = "Pune" },
        //    new User { UserId = Guid.NewGuid(), Name = "Gayatri", Email = "Gayatri@example.com", Job = "Developer", City = "Baner" },
        //    new User { UserId = Guid.NewGuid(), Name = "Rutik", Email = "Rutik@example.com", Job = "Developer", City = "Mumbai" },
        //    new User { UserId = Guid.NewGuid(), Name = "Prasanna", Email = "Prasanna@example.com", Job = "Tester", City = "Nanded" },
        //    new User { UserId = Guid.NewGuid(), Name = "Satish", Email = "Satish@example.com", Job = "Tester", City = "Kolhapur" },
        //    new User { UserId = Guid.NewGuid(), Name = "Abhijit", Email = "Abhijit@example.com", Job = "Developer", City = "Mumbai" },
        //    new User { UserId = Guid.NewGuid(), Name = "Brajesh", Email = "Brajesh@example.com", Job = "CEO", City = "Pune" },
        //};
        private static List<User> users = new List<User>
        {
            new User { UserId = 101, Name = "Michael Thompson", Email = "michael.thompson@company.com", Job = "Senior Software Engineer", City = "New York" },
            new User { UserId = 102, Name = "Emma Johnson", Email = "emma.johnson@company.com", Job = "Product Manager", City = "San Francisco" },
            new User { UserId = 103, Name = "Liam Brown", Email = "liam.brown@company.com", Job = "DevOps Engineer", City = "Toronto" },
            new User { UserId = 104, Name = "Olivia Martinez", Email = "olivia.martinez@company.com", Job = "UI/UX Designer", City = "Barcelona" },
            new User { UserId = 105, Name = "Noah Wilson", Email = "noah.wilson@company.com", Job = "Backend Developer", City = "London" },
            new User { UserId = 106, Name = "Sophia Miller", Email = "sophia.miller@company.com", Job = "Quality Assurance Engineer", City = "Berlin" },
            new User { UserId = 107, Name = "James Anderson", Email = "james.anderson@company.com", Job = "Cloud Architect", City = "Seattle" },
            new User { UserId = 108, Name = "Ava Davis", Email = "ava.davis@company.com", Job = "Data Analyst", City = "Sydney" },
            new User { UserId = 109, Name = "Benjamin Harris", Email = "benjamin.harris@company.com", Job = "Cybersecurity Specialist", City = "Amsterdam" },
            new User { UserId = 110, Name = "Mia Robinson", Email = "mia.robinson@company.com", Job = "Business Analyst", City = "Dublin" }
        };

        // -------------------- GET ALL --------------------
        /// <summary>List all users.</summary>
        /// <response code="200">All users.</response>
        [HttpGet("getAllUsers")]
        public IActionResult GetAll()
        {
            try
            {
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch users", error = ex.Message });
            }
        }

        // -------------------- GET BY ID --------------------
        /// <summary>Get a user by ID.</summary>
        /// <param name="id">User ID (101-110 for the seed users).</param>
        /// <response code="200">The user.</response>
        /// <response code="404">No user with that ID.</response>
        [HttpGet("user/{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var user = users.FirstOrDefault(x => x.UserId == id);

                if (user == null)
                    return NotFound(new { message = "User not found" });

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch user", error = ex.Message });
            }
        }

        // -------------------- CREATE --------------------
        /// <summary>Add a user.</summary>
        /// <param name="newUser">The user to add. Its userId is ignored and replaced with a random ID.</param>
        /// <response code="201">User created.</response>
        [HttpPost("addUser")]
        public IActionResult Create([FromBody] User newUser)
        {
            try
            {
                // Generate random 6-digit user ID
                Random rnd = new Random();
                newUser.UserId = rnd.Next(100, 999);
                users.Add(newUser);

                return StatusCode(201, new
                {
                    message = "User created successfully",
                    data = newUser
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create user", error = ex.Message });
            }
        }

        // -------------------- UPDATE --------------------
        /// <summary>Update a user's name, email, job and city.</summary>
        /// <param name="id">User ID.</param>
        /// <param name="updatedUser">New name, email, job and city.</param>
        /// <response code="200">User updated.</response>
        /// <response code="404">No user with that ID.</response>
        [HttpPut("update/{id}")]
        public IActionResult Update(int id, [FromBody] User updatedUser)
        {
            try
            {
                var existing = users.FirstOrDefault(x => x.UserId == id);

                if (existing == null)
                    return NotFound(new { message = "User not found" });

                existing.Name = updatedUser.Name;
                existing.Email = updatedUser.Email;
                existing.Job = updatedUser.Job;
                existing.City = updatedUser.City;

                return Ok(new
                {
                    message = "User updated successfully",
                    data = existing
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update user", error = ex.Message });
            }
        }

        // -------------------- DELETE --------------------
        /// <summary>Delete a user.</summary>
        /// <param name="id">User ID.</param>
        /// <response code="200">User deleted.</response>
        /// <response code="404">No user with that ID.</response>
        [HttpDelete("delete/{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var user = users.FirstOrDefault(x => x.UserId == id);

                if (user == null)
                    return NotFound(new { message = "User not found" });

                users.Remove(user);

                return Ok(new
                {
                    message = "User deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete user", error = ex.Message });
            }
        }

        //-------------------- Auth -------------------------
        /// <summary>Log in and receive a JWT for the Admin endpoint.</summary>
        /// <param name="userLogin">Username and password.</param>
        /// <remarks>
        /// Credentials: <c>Michael</c> / <c>Thompson</c> (role Admin). The token expires after 15 minutes.
        /// Send it as <c>Authorization: Bearer &lt;token&gt;</c> to <c>GET /api/Admin/authorize</c>.
        /// </remarks>
        /// <response code="200">Token and login time.</response>
        /// <response code="404">Unknown username or wrong password (plain-text body).</response>
        [AllowAnonymous]
        [HttpPost("Login")]
        public ActionResult Login([FromBody] UserLogin userLogin)
        {
            var user = Authenticate(userLogin);

            if (user != null)
            {
                var token = GenerateToken(user);

                return Ok(new
                {
                    Token = token,
                    LoginTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }

            return NotFound("user not found");
        }


        // To generate token
        private string GenerateToken(UserAuth user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier,user.Username),
                new Claim(ClaimTypes.Role,user.Role)
            };
            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
                _config["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(15),
                signingCredentials: credentials);


            return new JwtSecurityTokenHandler().WriteToken(token);

        }
        //To authenticate user
        private UserAuth Authenticate(UserLogin userLogin)
        {
            var currentUser = UserConstants.Users.FirstOrDefault(x => x.Username.ToLower() ==
                userLogin.Username.ToLower() && x.Password == userLogin.Password);
            if (currentUser != null)
            {
                return currentUser;
            }
            return null;
        }
    }
}
