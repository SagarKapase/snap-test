using Microsoft.AspNetCore.Mvc;
using snap_test.Models;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// XML users: CRUD over 4 hardcoded users (IDs 101-104) that accepts and returns application/xml.
/// </summary>
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
    /// <summary>List all users as XML.</summary>
    /// <response code="200">All users.</response>
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
    /// <summary>Get a user by ID as XML.</summary>
    /// <param name="id">User ID (101-104 for the seed users).</param>
    /// <response code="200">The user.</response>
    /// <response code="404">No user with that ID.</response>
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
    /// <summary>Add a user from an XML body.</summary>
    /// <param name="request">A <c>&lt;UserRequest&gt;</c> element with Name, Job and City.</param>
    /// <remarks>
    /// Header: <c>Content-Type: application/xml</c>. Example body:
    /// <c>&lt;UserRequest&gt;&lt;Name&gt;Ana&lt;/Name&gt;&lt;Job&gt;Pilot&lt;/Job&gt;&lt;City&gt;Lisbon&lt;/City&gt;&lt;/UserRequest&gt;</c>
    /// </remarks>
    /// <response code="200">The created user, with a random ID.</response>
    /// <response code="400">Body missing or not valid XML.</response>
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
    /// <summary>Update a user's name, job and city from an XML body.</summary>
    /// <param name="id">User ID.</param>
    /// <param name="request">A <c>&lt;UserRequest&gt;</c> element with Name, Job and City.</param>
    /// <response code="200">The updated user.</response>
    /// <response code="404">No user with that ID.</response>
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
    /// <summary>Delete a user.</summary>
    /// <param name="id">User ID.</param>
    /// <response code="200">User deleted.</response>
    /// <response code="404">No user with that ID.</response>
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
