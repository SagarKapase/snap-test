using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;
using snap_test.Models;

namespace snap_test.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        // -------------------- GET ALL (pagination / sort / filter / search) --------------------
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var (items, total, page, perPage, totalPages) =
                    QueryHelper.Apply(PostStore.Posts, Request.Query);

                Response.Headers["X-Total-Count"] = total.ToString();
                Response.Headers["X-Page"] = page.ToString();
                Response.Headers["X-Per-Page"] = perPage.ToString();
                Response.Headers["X-Total-Pages"] = totalPages.ToString();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch posts", error = ex.Message });
            }
        }

        // -------------------- GET BY ID --------------------
        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var post = PostStore.Posts.FirstOrDefault(p => p.Id == id);

                if (post == null)
                    return NotFound(ApiResponse.Error(404, $"Post with ID {id} does not exist."));

                return Ok(post);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch post", error = ex.Message });
            }
        }

        // -------------------- GET COMMENTS FOR A POST (nested) --------------------
        [HttpGet("{id:int}/comments")]
        public IActionResult GetComments(int id)
        {
            try
            {
                var post = PostStore.Posts.FirstOrDefault(p => p.Id == id);

                if (post == null)
                    return NotFound(ApiResponse.Error(404, $"Post with ID {id} does not exist."));

                var comments = CommentStore.Comments.Where(c => c.PostId == id).ToList();
                return Ok(comments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch comments", error = ex.Message });
            }
        }

        // -------------------- GET POSTS BY USER --------------------
        [HttpGet("user/{userId:int}")]
        public IActionResult GetByUser(int userId)
        {
            try
            {
                var posts = PostStore.Posts.Where(p => p.UserId == userId).ToList();
                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch posts", error = ex.Message });
            }
        }

        // -------------------- CREATE --------------------
        [HttpPost]
        public IActionResult Create([FromBody] Post newPost)
        {
            try
            {
                if (newPost == null || string.IsNullOrWhiteSpace(newPost.Title))
                    return BadRequest(ApiResponse.Error(400, "Missing required field: title"));

                newPost.Id = PostStore.Posts.Any() ? PostStore.Posts.Max(p => p.Id) + 1 : 1;

                if (string.IsNullOrWhiteSpace(newPost.PublishedAt))
                    newPost.PublishedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                PostStore.Posts.Add(newPost);

                return StatusCode(201, new
                {
                    message = "Post created successfully",
                    data = newPost
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create post", error = ex.Message });
            }
        }

        // -------------------- UPDATE --------------------
        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] Post updatedPost)
        {
            try
            {
                var existing = PostStore.Posts.FirstOrDefault(p => p.Id == id);

                if (existing == null)
                    return NotFound(ApiResponse.Error(404, $"Post with ID {id} does not exist."));

                existing.UserId = updatedPost.UserId;
                existing.Title = updatedPost.Title;
                existing.Body = updatedPost.Body;
                existing.Tags = updatedPost.Tags ?? existing.Tags;
                existing.Likes = updatedPost.Likes;

                return Ok(new
                {
                    message = "Post updated successfully",
                    data = existing
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update post", error = ex.Message });
            }
        }

        // -------------------- DELETE --------------------
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var post = PostStore.Posts.FirstOrDefault(p => p.Id == id);

                if (post == null)
                    return NotFound(ApiResponse.Error(404, $"Post with ID {id} does not exist."));

                PostStore.Posts.Remove(post);

                return Ok(new { message = "Post deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete post", error = ex.Message });
            }
        }
    }
}
