using Microsoft.AspNetCore.Mvc;
using snap_test.Models;

namespace snap_test.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        // Hardcoded in-memory list
        private static List<User> users = new List<User>
        {
            new User { UserId = Guid.NewGuid(), Name = "Sagar", Email = "sagar@example.com", Job = "Developer", City = "Pune" },
            new User { UserId = Guid.NewGuid(), Name = "Gayatri", Email = "Gayatri@example.com", Job = "Developer", City = "Baner" },
            new User { UserId = Guid.NewGuid(), Name = "Rutik", Email = "Rutik@example.com", Job = "Developer", City = "Mumbai" },
            new User { UserId = Guid.NewGuid(), Name = "Prasanna", Email = "Prasanna@example.com", Job = "Tester", City = "Nanded" },
            new User { UserId = Guid.NewGuid(), Name = "Satish", Email = "Satish@example.com", Job = "Tester", City = "Kolhapur" },
            new User { UserId = Guid.NewGuid(), Name = "Abhijit", Email = "Abhijit@example.com", Job = "Developer", City = "Mumbai" },
            new User { UserId = Guid.NewGuid(), Name = "Brajesh", Email = "Brajesh@example.com", Job = "CEO", City = "Pune" },
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
    }
}
