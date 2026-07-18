using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecipesController : ControllerBase
    {
        // -------------------- GET ALL (filters ?cuisine= ?difficulty=, pagination / sort / search) --------------------
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
