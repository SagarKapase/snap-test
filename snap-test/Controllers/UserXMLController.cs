using Microsoft.AspNetCore.Mvc;
using snap_test.Models;
using System;
using System.Collections.Generic;
using System.Linq;

[ApiController]
[Route("api/xml/[controller]")]
[Produces("application/xml")]
[Consumes("application/xml")]
public class UserXMLController : ControllerBase
{
    // Hardcoded international users
    private static List<UserXmlResponse> users = new List<UserXmlResponse>
    {
        new UserXmlResponse { Id = 101, Name = "Hiroshi Tanaka", Job = "Engineer", City = "Tokyo" },
        new UserXmlResponse { Id = 102, Name = "Maria Gonzales", Job = "Doctor", City = "Madrid" },
        new UserXmlResponse { Id = 103, Name = "Ahmed Al-Farsi", Job = "Teacher", City = "Dubai" },
        new UserXmlResponse { Id = 104, Name = "Elena Petrova", Job = "Designer", City = "Moscow" }
    };

    // GET ALL
    [HttpGet("all")]
    public IActionResult GetAll()
    {
        try
        {
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    // GET BY ID
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        try
        {
            var user = users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound($"User with ID {id} not found");

            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    // CREATE USER
    [HttpPost("create")]
    public IActionResult Create([FromBody] UserXmlRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest("Invalid XML Request");

            var newUser = new UserXmlResponse
            {
                Id = new Random().Next(1000, 9999),
                Name = request.Name,
                Job = request.Job,
                City = request.City
            };

            users.Add(newUser);

            return Ok(newUser); // returns XML
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    // UPDATE USER
    [HttpPut("update/{id}")]
    public IActionResult Update(int id, [FromBody] UserXmlRequest request)
    {
        try
        {
            var user = users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound($"User with ID {id} not found");

            user.Name = request.Name;
            user.Job = request.Job;
            user.City = request.City;

            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    // DELETE USER
    [HttpDelete("delete/{id}")]
    public IActionResult Delete(int id)
    {
        try
        {
            var user = users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound($"User with ID {id} not found");

            users.Remove(user);

            return Ok($"User with ID {id} deleted");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }
}
