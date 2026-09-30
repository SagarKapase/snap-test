using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;
using snap_test.Models;

namespace snap_test.Controllers
{
    /// <summary>Shopping carts whose items reference real product IDs. Hardcoded in-memory data: writes last until the app restarts.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CartsController : ControllerBase
    {
        // -------------------- GET ALL (filter ?userId=, pagination / sort) --------------------
        /// <summary>List carts with filtering, sorting, search and pagination.</summary>
        /// <remarks>
        /// Common filters: `userId`. Any other property name also works as an exact, case-insensitive filter.
        /// Paging and sorting: `limit` (1-100), `page` (1-based, needs `limit`), `offset`, `sort` (property name),
        /// `order` (`asc` or `desc`) and `q` (search across title, name, body, description and text fields).
        /// Totals are returned in the X-Total-Count, X-Page, X-Per-Page and X-Total-Pages headers.
        /// </remarks>
        /// <response code="200">The matching carts.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var (items, total, page, perPage, totalPages) =
                    QueryHelper.Apply(CartStore.Carts, Request.Query);

                Response.Headers["X-Total-Count"] = total.ToString();
                Response.Headers["X-Page"] = page.ToString();
                Response.Headers["X-Per-Page"] = perPage.ToString();
                Response.Headers["X-Total-Pages"] = totalPages.ToString();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch carts", error = ex.Message });
            }
        }

        // -------------------- GET BY ID --------------------
        /// <summary>Get a cart by ID.</summary>
        /// <param name="id">Cart ID.</param>
        /// <response code="200">The cart.</response>
        /// <response code="404">The cart does not exist.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var cart = CartStore.Carts.FirstOrDefault(c => c.Id == id);

                if (cart == null)
                    return NotFound(ApiResponse.Error(404, $"Cart with ID {id} does not exist."));

                return Ok(cart);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch cart", error = ex.Message });
            }
        }

        // -------------------- CREATE --------------------
        /// <summary>Create a cart.</summary>
        /// <param name="newCart">The new cart. Any client-supplied ID is replaced.</param>
        /// <response code="201">Created; returns { message, data } with the generated ID.</response>
        /// <response code="400">The request body is missing.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpPost]
        public IActionResult Create([FromBody] Cart newCart)
        {
            try
            {
                if (newCart == null)
                    return BadRequest(ApiResponse.Error(400, "Missing request body"));

                newCart.Id = CartStore.Carts.Any() ? CartStore.Carts.Max(c => c.Id) + 1 : 1;
                newCart.Items ??= new();
                newCart.Total = CalculateTotal(newCart.Items);
                newCart.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                CartStore.Carts.Add(newCart);

                return StatusCode(201, new
                {
                    message = "Cart created successfully",
                    data = newCart
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create cart", error = ex.Message });
            }
        }

        // -------------------- UPDATE (add/remove items) --------------------
        /// <summary>Update a cart.</summary>
        /// <remarks>Use this to add or remove items.</remarks>
        /// <param name="id">Cart ID.</param>
        /// <param name="updatedCart">The new field values.</param>
        /// <response code="200">Updated; returns { message, data }.</response>
        /// <response code="404">The cart does not exist.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] Cart updatedCart)
        {
            try
            {
                var existing = CartStore.Carts.FirstOrDefault(c => c.Id == id);

                if (existing == null)
                    return NotFound(ApiResponse.Error(404, $"Cart with ID {id} does not exist."));

                existing.UserId = updatedCart.UserId != 0 ? updatedCart.UserId : existing.UserId;
                existing.Items = updatedCart.Items ?? existing.Items;
                existing.Total = CalculateTotal(existing.Items);
                existing.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                return Ok(new
                {
                    message = "Cart updated successfully",
                    data = existing
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update cart", error = ex.Message });
            }
        }

        // -------------------- DELETE --------------------
        /// <summary>Delete a cart.</summary>
        /// <param name="id">Cart ID.</param>
        /// <response code="200">Deleted.</response>
        /// <response code="404">The cart does not exist.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var cart = CartStore.Carts.FirstOrDefault(c => c.Id == id);

                if (cart == null)
                    return NotFound(ApiResponse.Error(404, $"Cart with ID {id} does not exist."));

                CartStore.Carts.Remove(cart);

                return Ok(new { message = "Cart deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete cart", error = ex.Message });
            }
        }

        private static decimal CalculateTotal(List<CartItem> items) =>
            Math.Round(items.Sum(i => i.Price * i.Quantity), 2);
    }
}
