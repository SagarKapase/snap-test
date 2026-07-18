using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        // -------------------- GET ALL (filters ?userId= ?type= ?category=, pagination / sort / search) --------------------
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
