using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;
using snap_test.Models;

namespace snap_test.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        // -------------------- GET ALL (filter ?postId=, pagination / sort / search) --------------------
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var (items, total, page, perPage, totalPages) =
                    QueryHelper.Apply(CommentStore.Comments, Request.Query);

                Response.Headers["X-Total-Count"] = total.ToString();
                Response.Headers["X-Page"] = page.ToString();
                Response.Headers["X-Per-Page"] = perPage.ToString();
                Response.Headers["X-Total-Pages"] = totalPages.ToString();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch comments", error = ex.Message });
            }
        }

        // -------------------- GET BY ID --------------------
        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var comment = CommentStore.Comments.FirstOrDefault(c => c.Id == id);

                if (comment == null)
                    return NotFound(ApiResponse.Error(404, $"Comment with ID {id} does not exist."));

                return Ok(comment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch comment", error = ex.Message });
            }
        }

        // -------------------- CREATE --------------------
        [HttpPost]
        public IActionResult Create([FromBody] Comment newComment)
        {
            try
            {
                if (newComment == null || string.IsNullOrWhiteSpace(newComment.Body))
                    return BadRequest(ApiResponse.Error(400, "Missing required field: body"));

                newComment.Id = CommentStore.Comments.Any() ? CommentStore.Comments.Max(c => c.Id) + 1 : 1;

                if (string.IsNullOrWhiteSpace(newComment.CreatedAt))
                    newComment.CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                CommentStore.Comments.Add(newComment);

                return StatusCode(201, new
                {
                    message = "Comment created successfully",
                    data = newComment
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create comment", error = ex.Message });
            }
        }

        // -------------------- DELETE --------------------
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var comment = CommentStore.Comments.FirstOrDefault(c => c.Id == id);

                if (comment == null)
                    return NotFound(ApiResponse.Error(404, $"Comment with ID {id} does not exist."));

                CommentStore.Comments.Remove(comment);

                return Ok(new { message = "Comment deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete comment", error = ex.Message });
            }
        }
    }
}
