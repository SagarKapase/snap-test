using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>Read-only quotes in programming, motivation, wisdom, humor and design categories.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class QuotesController : ControllerBase
    {
        // -------------------- GET ALL (filter ?category=, pagination / sort / search) --------------------
        /// <summary>List quotes with filtering, sorting, search and pagination.</summary>
        /// <remarks>
        /// Common filters: `category`, `author`. Any other property name also works as an exact, case-insensitive filter.
        /// Paging and sorting: `limit` (1-100), `page` (1-based, needs `limit`), `offset`, `sort` (property name),
        /// `order` (`asc` or `desc`) and `q` (search across title, name, body, description and text fields).
        /// Totals are returned in the X-Total-Count, X-Page, X-Per-Page and X-Total-Pages headers.
        /// </remarks>
        /// <response code="200">The matching quotes.</response>
        /// <response code="500">Unexpected server error.</response>
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
        /// <summary>Get a random quote.</summary>
        /// <response code="200">A random quote.</response>
        /// <response code="404">There are no quotes.</response>
        /// <response code="500">Unexpected server error.</response>
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
        /// <summary>Get a quote by ID.</summary>
        /// <param name="id">Quote ID.</param>
        /// <response code="200">The quote.</response>
        /// <response code="404">The quote does not exist.</response>
        /// <response code="500">Unexpected server error.</response>
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
