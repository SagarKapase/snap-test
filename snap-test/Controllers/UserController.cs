using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using snap_test.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace snap_test.Controllers
{
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
            new User { UserId = Guid.NewGuid(), Name = "Michael Thompson", Email = "michael.thompson@company.com", Job = "Senior Software Engineer", City = "New York" },
            new User { UserId = Guid.NewGuid(), Name = "Emma Johnson", Email = "emma.johnson@company.com", Job = "Product Manager", City = "San Francisco" },
            new User { UserId = Guid.NewGuid(), Name = "Liam Brown", Email = "liam.brown@company.com", Job = "DevOps Engineer", City = "Toronto" },
            new User { UserId = Guid.NewGuid(), Name = "Olivia Martinez", Email = "olivia.martinez@company.com", Job = "UI/UX Designer", City = "Barcelona" },
            new User { UserId = Guid.NewGuid(), Name = "Noah Wilson", Email = "noah.wilson@company.com", Job = "Backend Developer", City = "London" },
            new User { UserId = Guid.NewGuid(), Name = "Sophia Miller", Email = "sophia.miller@company.com", Job = "Quality Assurance Engineer", City = "Berlin" },
            new User { UserId = Guid.NewGuid(), Name = "James Anderson", Email = "james.anderson@company.com", Job = "Cloud Architect", City = "Seattle" },
            new User { UserId = Guid.NewGuid(), Name = "Ava Davis", Email = "ava.davis@company.com", Job = "Data Analyst", City = "Sydney" },
            new User { UserId = Guid.NewGuid(), Name = "Benjamin Harris", Email = "benjamin.harris@company.com", Job = "Cybersecurity Specialist", City = "Amsterdam" },
            new User { UserId = Guid.NewGuid(), Name = "Mia Robinson", Email = "mia.robinson@company.com", Job = "Business Analyst", City = "Dublin" }
        };

        // -------------------- GET ALL --------------------
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
        [HttpGet("user/{id}")]
        public IActionResult GetById(Guid id)
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
        [HttpPost("addUser")]
        public IActionResult Create([FromBody] User newUser)
        {
            try
            {
                newUser.UserId = Guid.NewGuid();
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
        [HttpPut("update/{id}")]
        public IActionResult Update(Guid id, [FromBody] User updatedUser)
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
        [HttpDelete("delete/{id}")]
        public IActionResult Delete(Guid id)
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
