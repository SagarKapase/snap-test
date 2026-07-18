using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuotesController : ControllerBase
    {
        // -------------------- GET ALL (filter ?category=, pagination / sort / search) --------------------
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var (items, total, page, perPage, totalPages) =
                    QueryHelper.Apply(QuoteStore.Quotes, Request.Query);

                Response.Headers["X-Total-Count"] = total.ToString();
                Response.Headers["X-Page"] = page.ToString();
                Response.Headers["X-Per-Page"] = perPage.ToString();
                Response.Headers["X-Total-Pages"] = totalPages.ToString();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch quotes", error = ex.Message });
            }
        }

        // -------------------- GET RANDOM --------------------
        [HttpGet("random")]
        public IActionResult GetRandom()
        {
            try
            {
                if (QuoteStore.Quotes.Count == 0)
                    return NotFound(ApiResponse.Error(404, "No quotes available."));

                var quote = QuoteStore.Quotes[Random.Shared.Next(QuoteStore.Quotes.Count)];
                return Ok(quote);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch quote", error = ex.Message });
            }
        }

        // -------------------- GET BY ID --------------------
        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var quote = QuoteStore.Quotes.FirstOrDefault(q => q.Id == id);

                if (quote == null)
                    return NotFound(ApiResponse.Error(404, $"Quote with ID {id} does not exist."));

                return Ok(quote);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch quote", error = ex.Message });
            }
        }
    }
}
