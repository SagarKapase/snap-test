using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>Read-only bank transactions (income and expenses) with merchant, category and running balance.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        // -------------------- GET ALL (filters ?userId= ?type= ?category=, pagination / sort / search) --------------------
        /// <summary>List transactions with filtering, sorting, search and pagination.</summary>
        /// <remarks>
        /// Common filters: `userId`, `type`, `category`. Any other property name also works as an exact, case-insensitive filter.
        /// Paging and sorting: `limit` (1-100), `page` (1-based, needs `limit`), `offset`, `sort` (property name),
        /// `order` (`asc` or `desc`) and `q` (search across title, name, body, description and text fields).
        /// Totals are returned in the X-Total-Count, X-Page, X-Per-Page and X-Total-Pages headers.
        /// </remarks>
        /// <response code="200">The matching transactions.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var (items, total, page, perPage, totalPages) =
                    QueryHelper.Apply(TransactionStore.Transactions, Request.Query);

                Response.Headers["X-Total-Count"] = total.ToString();
                Response.Headers["X-Page"] = page.ToString();
                Response.Headers["X-Per-Page"] = perPage.ToString();
                Response.Headers["X-Total-Pages"] = totalPages.ToString();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch transactions", error = ex.Message });
            }
        }

        // -------------------- SUMMARY (aggregate income / expenses / balance) --------------------
        // Declared before {id:int}; "summary" is a literal segment so there is no route ambiguity.
        /// <summary>Get total income, expenses and balance.</summary>
        /// <remarks>Income is the sum of positive amounts; expenses are reported as a positive total of negative amounts.</remarks>
        /// <param name="userId">Only this user's transactions (default: all users).</param>
        /// <response code="200">Returns { userId, totalIncome, totalExpenses, balance, transactionCount }.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpGet("summary")]
        public IActionResult GetSummary([FromQuery] int? userId)
        {
            try
            {
                var txns = (userId.HasValue
                    ? TransactionStore.Transactions.Where(t => t.UserId == userId.Value)
                    : TransactionStore.Transactions).ToList();

                var totalIncome = txns.Where(t => t.Amount > 0).Sum(t => t.Amount);
                var totalExpenses = txns.Where(t => t.Amount < 0).Sum(t => t.Amount); // negative

                return Ok(new
                {
                    userId,
                    totalIncome = Math.Round(totalIncome, 2),
                    totalExpenses = Math.Round(Math.Abs(totalExpenses), 2),
                    balance = Math.Round(totalIncome + totalExpenses, 2),
                    transactionCount = txns.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to build summary", error = ex.Message });
            }
        }

        // -------------------- GET BY ID --------------------
        /// <summary>Get a transaction by ID.</summary>
        /// <param name="id">Transaction ID.</param>
        /// <response code="200">The transaction.</response>
        /// <response code="404">The transaction does not exist.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var transaction = TransactionStore.Transactions.FirstOrDefault(t => t.Id == id);

                if (transaction == null)
                    return NotFound(ApiResponse.Error(404, $"Transaction with ID {id} does not exist."));

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch transaction", error = ex.Message });
            }
        }
    }
}
