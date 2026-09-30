using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>User notifications (mentions, likes, follows, system and order updates) with read state. Hardcoded in-memory data: writes last until the app restarts.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        // -------------------- GET ALL (filters ?userId= ?read=, pagination / sort / search) --------------------
        /// <summary>List notifications with filtering, sorting, search and pagination.</summary>
        /// <remarks>
        /// Common filters: `userId`, `read`, `type`. Any other property name also works as an exact, case-insensitive filter.
        /// Paging and sorting: `limit` (1-100), `page` (1-based, needs `limit`), `offset`, `sort` (property name),
        /// `order` (`asc` or `desc`) and `q` (search across title, name, body, description and text fields).
        /// Totals are returned in the X-Total-Count, X-Page, X-Per-Page and X-Total-Pages headers.
        /// </remarks>
        /// <response code="200">The matching notifications.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var (items, total, page, perPage, totalPages) =
                    QueryHelper.Apply(NotificationStore.Notifications, Request.Query);

                Response.Headers["X-Total-Count"] = total.ToString();
                Response.Headers["X-Page"] = page.ToString();
                Response.Headers["X-Per-Page"] = perPage.ToString();
                Response.Headers["X-Total-Pages"] = totalPages.ToString();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch notifications", error = ex.Message });
            }
        }

        // -------------------- MARK ALL AS READ (for a user, or all) --------------------
        // Declared before {id:int}/read; "read-all" is a literal segment so there is no route ambiguity.
        /// <summary>Mark all notifications as read, for one user or for everyone.</summary>
        /// <param name="userId">Only this user's notifications (default: all users).</param>
        /// <response code="200">Returns { message, updated } with the number of notifications changed.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpPut("read-all")]
        public IActionResult MarkAllRead([FromQuery] int? userId)
        {
            try
            {
                var target = (userId.HasValue
                    ? NotificationStore.Notifications.Where(n => n.UserId == userId.Value)
                    : NotificationStore.Notifications).ToList();

                var updated = 0;
                foreach (var n in target.Where(n => !n.Read))
                {
                    n.Read = true;
                    updated++;
                }

                return Ok(new { message = "All notifications marked as read", updated });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update notifications", error = ex.Message });
            }
        }

        // -------------------- GET BY ID --------------------
        /// <summary>Get a notification by ID.</summary>
        /// <param name="id">Notification ID.</param>
        /// <response code="200">The notification.</response>
        /// <response code="404">The notification does not exist.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var notification = NotificationStore.Notifications.FirstOrDefault(n => n.Id == id);

                if (notification == null)
                    return NotFound(ApiResponse.Error(404, $"Notification with ID {id} does not exist."));

                return Ok(notification);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch notification", error = ex.Message });
            }
        }

        // -------------------- MARK ONE AS READ --------------------
        /// <summary>Mark a notification as read.</summary>
        /// <param name="id">Notification ID.</param>
        /// <response code="200">Updated; returns { message, data }.</response>
        /// <response code="404">The notification does not exist.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpPut("{id:int}/read")]
        public IActionResult MarkRead(int id)
        {
            try
            {
                var notification = NotificationStore.Notifications.FirstOrDefault(n => n.Id == id);

                if (notification == null)
                    return NotFound(ApiResponse.Error(404, $"Notification with ID {id} does not exist."));

                notification.Read = true;

                return Ok(new
                {
                    message = "Notification marked as read",
                    data = notification
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update notification", error = ex.Message });
            }
        }
    }
}
