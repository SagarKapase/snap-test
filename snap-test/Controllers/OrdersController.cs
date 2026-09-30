using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;
using snap_test.Models;

namespace snap_test.Controllers
{
    /// <summary>Orders with line items, shipping address and a status of pending, processing, shipped, delivered or cancelled. Hardcoded in-memory data: writes last until the app restarts.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        // -------------------- GET ALL (filters ?userId= ?status=, pagination / sort) --------------------
        /// <summary>List orders with filtering, sorting, search and pagination.</summary>
        /// <remarks>
        /// Common filters: `userId`, `status`. Any other property name also works as an exact, case-insensitive filter.
        /// Paging and sorting: `limit` (1-100), `page` (1-based, needs `limit`), `offset`, `sort` (property name),
        /// `order` (`asc` or `desc`) and `q` (search across title, name, body, description and text fields).
        /// Totals are returned in the X-Total-Count, X-Page, X-Per-Page and X-Total-Pages headers.
        /// </remarks>
        /// <response code="200">The matching orders.</response>
        /// <response code="500">Unexpected server error.</response>
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
        /// <summary>Get a order by ID.</summary>
        /// <param name="id">Order ID.</param>
        /// <response code="200">The order.</response>
        /// <response code="404">The order does not exist.</response>
        /// <response code="500">Unexpected server error.</response>
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
        /// <summary>Create a order.</summary>
        /// <param name="newOrder">The new order. Any client-supplied ID is replaced.</param>
        /// <response code="201">Created; returns { message, data } with the generated ID.</response>
        /// <response code="400">items is missing or empty.</response>
        /// <response code="500">Unexpected server error.</response>
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
        /// <summary>Update a order.</summary>
        /// <remarks>Changes status, shippingAddress and deliveredAt when provided. Setting status to delivered stamps deliveredAt if it is empty.</remarks>
        /// <param name="id">Order ID.</param>
        /// <param name="updatedOrder">The new field values.</param>
        /// <response code="200">Updated; returns { message, data }.</response>
        /// <response code="404">The order does not exist.</response>
        /// <response code="500">Unexpected server error.</response>
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
