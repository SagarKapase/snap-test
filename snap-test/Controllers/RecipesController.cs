using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>Read-only recipes with ingredients, instructions, cuisine and difficulty.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RecipesController : ControllerBase
    {
        // -------------------- GET ALL (filters ?cuisine= ?difficulty=, pagination / sort / search) --------------------
        /// <summary>List recipes with filtering, sorting, search and pagination.</summary>
        /// <remarks>
        /// Common filters: `cuisine`, `difficulty`. Any other property name also works as an exact, case-insensitive filter.
        /// Paging and sorting: `limit` (1-100), `page` (1-based, needs `limit`), `offset`, `sort` (property name),
        /// `order` (`asc` or `desc`) and `q` (search across title, name, body, description and text fields).
        /// Totals are returned in the X-Total-Count, X-Page, X-Per-Page and X-Total-Pages headers.
        /// </remarks>
        /// <response code="200">The matching recipes.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var (items, total, page, perPage, totalPages) =
                    QueryHelper.Apply(RecipeStore.Recipes, Request.Query);

                Response.Headers["X-Total-Count"] = total.ToString();
                Response.Headers["X-Page"] = page.ToString();
                Response.Headers["X-Per-Page"] = perPage.ToString();
                Response.Headers["X-Total-Pages"] = totalPages.ToString();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch recipes", error = ex.Message });
            }
        }

        // -------------------- GET RANDOM --------------------
        /// <summary>Get a random recipe.</summary>
        /// <response code="200">A random recipe.</response>
        /// <response code="404">There are no recipes.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpGet("random")]
        public IActionResult GetRandom()
        {
            try
            {
                if (RecipeStore.Recipes.Count == 0)
                    return NotFound(ApiResponse.Error(404, "No recipes available."));

                var recipe = RecipeStore.Recipes[Random.Shared.Next(RecipeStore.Recipes.Count)];
                return Ok(recipe);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch recipe", error = ex.Message });
            }
        }

        // -------------------- GET CUISINES --------------------
        /// <summary>List cuisines without duplicates.</summary>
        /// <response code="200">Cuisine names.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpGet("cuisines")]
        public IActionResult GetCuisines()
        {
            try
            {
                var cuisines = RecipeStore.Recipes
                    .Select(r => r.Cuisine)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return Ok(cuisines);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch cuisines", error = ex.Message });
            }
        }

        // -------------------- GET BY ID --------------------
        /// <summary>Get a recipe by ID.</summary>
        /// <param name="id">Recipe ID.</param>
        /// <response code="200">The recipe.</response>
        /// <response code="404">The recipe does not exist.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var recipe = RecipeStore.Recipes.FirstOrDefault(r => r.Id == id);

                if (recipe == null)
                    return NotFound(ApiResponse.Error(404, $"Recipe with ID {id} does not exist."));

                return Ok(recipe);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch recipe", error = ex.Message });
            }
        }
    }
}
