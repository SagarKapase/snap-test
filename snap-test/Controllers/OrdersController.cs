using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;
using snap_test.Models;

namespace snap_test.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        // -------------------- GET ALL (filters ?userId= ?status=, pagination / sort) --------------------
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var (items, total, page, perPage, totalPages) =
                    QueryHelper.Apply(OrderStore.Orders, Request.Query);

                Response.Headers["X-Total-Count"] = total.ToString();
                Response.Headers["X-Page"] = page.ToString();
                Response.Headers["X-Per-Page"] = perPage.ToString();
                Response.Headers["X-Total-Pages"] = totalPages.ToString();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch orders", error = ex.Message });
            }
        }

        // -------------------- GET BY ID --------------------
        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var order = OrderStore.Orders.FirstOrDefault(o => o.Id == id);

                if (order == null)
                    return NotFound(ApiResponse.Error(404, $"Order with ID {id} does not exist."));

                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch order", error = ex.Message });
            }
        }

        // -------------------- CREATE --------------------
        [HttpPost]
        public IActionResult Create([FromBody] Order newOrder)
        {
            try
            {
                if (newOrder == null || newOrder.Items == null || newOrder.Items.Count == 0)
                    return BadRequest(ApiResponse.Error(400, "Missing required field: items"));

                newOrder.Id = OrderStore.Orders.Any() ? OrderStore.Orders.Max(o => o.Id) + 1 : 1;
                newOrder.Total = Math.Round(newOrder.Items.Sum(i => i.Price * i.Quantity), 2);

                if (string.IsNullOrWhiteSpace(newOrder.Status))
                    newOrder.Status = "pending";

                if (string.IsNullOrWhiteSpace(newOrder.OrderedAt))
                    newOrder.OrderedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                OrderStore.Orders.Add(newOrder);

                return StatusCode(201, new
                {
                    message = "Order created successfully",
                    data = newOrder
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create order", error = ex.Message });
            }
        }

        // -------------------- UPDATE (change status) --------------------
        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] Order updatedOrder)
        {
            try
            {
                var existing = OrderStore.Orders.FirstOrDefault(o => o.Id == id);

                if (existing == null)
                    return NotFound(ApiResponse.Error(404, $"Order with ID {id} does not exist."));

                if (!string.IsNullOrWhiteSpace(updatedOrder.Status))
                    existing.Status = updatedOrder.Status;

                if (updatedOrder.ShippingAddress != null)
                    existing.ShippingAddress = updatedOrder.ShippingAddress;

                // Auto-stamp delivery time when an order is marked delivered.
                if (string.Equals(existing.Status, "delivered", StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrWhiteSpace(existing.DeliveredAt))
                {
                    existing.DeliveredAt = updatedOrder.DeliveredAt ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                }
                else if (updatedOrder.DeliveredAt != null)
                {
                    existing.DeliveredAt = updatedOrder.DeliveredAt;
                }

                return Ok(new
                {
                    message = "Order updated successfully",
                    data = existing
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update order", error = ex.Message });
            }
        }
    }
}
