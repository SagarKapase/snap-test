using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;
using snap_test.Models;

namespace snap_test.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        // -------------------- GET ALL (pagination / sort / filter / search) --------------------
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
