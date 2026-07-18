using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        // -------------------- GET ALL (filters ?userId= ?read=, pagination / sort / search) --------------------
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
