using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;
using snap_test.Models;

namespace snap_test.Controllers
{
    /// <summary>Comments on posts. Hardcoded in-memory data: writes last until the app restarts.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        // -------------------- GET ALL (filter ?postId=, pagination / sort / search) --------------------
        /// <summary>List comments with filtering, sorting, search and pagination.</summary>
        /// <remarks>
        /// Common filters: `postId`, `userId`. Any other property name also works as an exact, case-insensitive filter.
        /// Paging and sorting: `limit` (1-100), `page` (1-based, needs `limit`), `offset`, `sort` (property name),
        /// `order` (`asc` or `desc`) and `q` (search across title, name, body, description and text fields).
        /// Totals are returned in the X-Total-Count, X-Page, X-Per-Page and X-Total-Pages headers.
        /// </remarks>
        /// <response code="200">The matching comments.</response>
        /// <response code="500">Unexpected server error.</response>
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
        /// <summary>Get a comment by ID.</summary>
        /// <param name="id">Comment ID.</param>
        /// <response code="200">The comment.</response>
        /// <response code="404">The comment does not exist.</response>
        /// <response code="500">Unexpected server error.</response>
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
        /// <summary>Create a comment.</summary>
        /// <param name="newComment">The new comment. Any client-supplied ID is replaced.</param>
        /// <response code="201">Created; returns { message, data } with the generated ID.</response>
        /// <response code="400">body is missing.</response>
        /// <response code="500">Unexpected server error.</response>
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
        /// <summary>Delete a comment.</summary>
        /// <param name="id">Comment ID.</param>
        /// <response code="200">Deleted.</response>
        /// <response code="404">The comment does not exist.</response>
        /// <response code="500">Unexpected server error.</response>
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
