using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;
using snap_test.Models;

namespace snap_test.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TodosController : ControllerBase
    {
        // -------------------- GET ALL (filters ?completed= ?priority= ?userId=, pagination / sort / search) --------------------
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var (items, total, page, perPage, totalPages) =
                    QueryHelper.Apply(TodoStore.Todos, Request.Query);

                Response.Headers["X-Total-Count"] = total.ToString();
                Response.Headers["X-Page"] = page.ToString();
                Response.Headers["X-Per-Page"] = perPage.ToString();
                Response.Headers["X-Total-Pages"] = totalPages.ToString();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch todos", error = ex.Message });
            }
        }

        // -------------------- GET BY ID --------------------
        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var todo = TodoStore.Todos.FirstOrDefault(t => t.Id == id);

                if (todo == null)
                    return NotFound(ApiResponse.Error(404, $"Todo with ID {id} does not exist."));

                return Ok(todo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch todo", error = ex.Message });
            }
        }

        // -------------------- CREATE --------------------
        [HttpPost]
        public IActionResult Create([FromBody] Todo newTodo)
        {
            try
            {
                if (newTodo == null || string.IsNullOrWhiteSpace(newTodo.Title))
                    return BadRequest(ApiResponse.Error(400, "Missing required field: title"));

                newTodo.Id = TodoStore.Todos.Any() ? TodoStore.Todos.Max(t => t.Id) + 1 : 1;

                if (string.IsNullOrWhiteSpace(newTodo.Priority))
                    newTodo.Priority = "medium";

                TodoStore.Todos.Add(newTodo);

                return StatusCode(201, new
                {
                    message = "Todo created successfully",
                    data = newTodo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create todo", error = ex.Message });
            }
        }

        // -------------------- UPDATE --------------------
        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] Todo updatedTodo)
        {
            try
            {
                var existing = TodoStore.Todos.FirstOrDefault(t => t.Id == id);

                if (existing == null)
                    return NotFound(ApiResponse.Error(404, $"Todo with ID {id} does not exist."));

                existing.UserId = updatedTodo.UserId;
                existing.Title = updatedTodo.Title;
                existing.Completed = updatedTodo.Completed;
                existing.Priority = updatedTodo.Priority;
                existing.DueDate = updatedTodo.DueDate;

                return Ok(new
                {
                    message = "Todo updated successfully",
                    data = existing
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update todo", error = ex.Message });
            }
        }

        // -------------------- DELETE --------------------
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var todo = TodoStore.Todos.FirstOrDefault(t => t.Id == id);

                if (todo == null)
                    return NotFound(ApiResponse.Error(404, $"Todo with ID {id} does not exist."));

                TodoStore.Todos.Remove(todo);

                return Ok(new { message = "Todo deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete todo", error = ex.Message });
            }
        }
    }
}
