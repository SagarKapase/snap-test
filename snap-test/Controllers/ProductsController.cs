using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;
using snap_test.Models;

namespace snap_test.Controllers
{
    /// <summary>E-commerce products with categories, prices, ratings and stock. Hardcoded in-memory data: writes last until the app restarts.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        // -------------------- GET ALL (pagination / sort / filter / search) --------------------
        /// <summary>List products with filtering, sorting, search and pagination.</summary>
        /// <remarks>
        /// Common filters: `category`, `inStock`. Any other property name also works as an exact, case-insensitive filter.
        /// Paging and sorting: `limit` (1-100), `page` (1-based, needs `limit`), `offset`, `sort` (property name),
        /// `order` (`asc` or `desc`) and `q` (search across title, name, body, description and text fields).
        /// Totals are returned in the X-Total-Count, X-Page, X-Per-Page and X-Total-Pages headers.
        /// </remarks>
        /// <response code="200">The matching products.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var (items, total, page, perPage, totalPages) =
                    QueryHelper.Apply(ProductStore.Products, Request.Query);

                Response.Headers["X-Total-Count"] = total.ToString();
                Response.Headers["X-Page"] = page.ToString();
                Response.Headers["X-Per-Page"] = perPage.ToString();
                Response.Headers["X-Total-Pages"] = totalPages.ToString();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch products", error = ex.Message });
            }
        }

        // -------------------- GET CATEGORIES --------------------
        /// <summary>List product categories without duplicates.</summary>
        /// <response code="200">Category names.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpGet("categories")]
        public IActionResult GetCategories()
        {
            try
            {
                var categories = ProductStore.Products
                    .Select(p => p.Category)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch categories", error = ex.Message });
            }
        }

        // -------------------- GET BY CATEGORY --------------------
        /// <summary>List the products in a category.</summary>
        /// <param name="name">Category name, case-insensitive, e.g. electronics.</param>
        /// <response code="200">Matching products (an empty array for an unknown category).</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpGet("category/{name}")]
        public IActionResult GetByCategory(string name)
        {
            try
            {
                var products = ProductStore.Products
                    .Where(p => string.Equals(p.Category, name, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch products", error = ex.Message });
            }
        }

        // -------------------- GET BY ID --------------------
        /// <summary>Get a product by ID.</summary>
        /// <param name="id">Product ID.</param>
        /// <response code="200">The product.</response>
        /// <response code="404">The product does not exist.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var product = ProductStore.Products.FirstOrDefault(p => p.Id == id);

                if (product == null)
                    return NotFound(ApiResponse.Error(404, $"Product with ID {id} does not exist."));

                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch product", error = ex.Message });
            }
        }

        // -------------------- CREATE --------------------
        /// <summary>Create a product.</summary>
        /// <param name="newProduct">The new product. Any client-supplied ID is replaced.</param>
        /// <response code="201">Created; returns { message, data } with the generated ID.</response>
        /// <response code="400">title is missing.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpPost]
        public IActionResult Create([FromBody] Product newProduct)
        {
            try
            {
                if (newProduct == null || string.IsNullOrWhiteSpace(newProduct.Title))
                    return BadRequest(ApiResponse.Error(400, "Missing required field: title"));

                newProduct.Id = ProductStore.Products.Any()
                    ? ProductStore.Products.Max(p => p.Id) + 1
                    : 1;

                if (string.IsNullOrWhiteSpace(newProduct.CreatedAt))
                    newProduct.CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                ProductStore.Products.Add(newProduct);

                return StatusCode(201, new
                {
                    message = "Product created successfully",
                    data = newProduct
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create product", error = ex.Message });
            }
        }

        // -------------------- UPDATE --------------------
        /// <summary>Update a product.</summary>
        /// <remarks>Replaces title, price, description, category, image and inStock; rating is kept when omitted.</remarks>
        /// <param name="id">Product ID.</param>
        /// <param name="updatedProduct">The new field values.</param>
        /// <response code="200">Updated; returns { message, data }.</response>
        /// <response code="404">The product does not exist.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] Product updatedProduct)
        {
            try
            {
                var existing = ProductStore.Products.FirstOrDefault(p => p.Id == id);

                if (existing == null)
                    return NotFound(ApiResponse.Error(404, $"Product with ID {id} does not exist."));

                existing.Title = updatedProduct.Title;
                existing.Price = updatedProduct.Price;
                existing.Description = updatedProduct.Description;
                existing.Category = updatedProduct.Category;
                existing.Image = updatedProduct.Image;
                existing.Rating = updatedProduct.Rating ?? existing.Rating;
                existing.InStock = updatedProduct.InStock;

                return Ok(new
                {
                    message = "Product updated successfully",
                    data = existing
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update product", error = ex.Message });
            }
        }

        // -------------------- DELETE --------------------
        /// <summary>Delete a product.</summary>
        /// <param name="id">Product ID.</param>
        /// <response code="200">Deleted.</response>
        /// <response code="404">The product does not exist.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var product = ProductStore.Products.FirstOrDefault(p => p.Id == id);

                if (product == null)
                    return NotFound(ApiResponse.Error(404, $"Product with ID {id} does not exist."));

                ProductStore.Products.Remove(product);

                return Ok(new { message = "Product deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete product", error = ex.Message });
            }
        }
    }
}
