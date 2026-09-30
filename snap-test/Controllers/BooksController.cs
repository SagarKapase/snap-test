using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;
using snap_test.Models;

namespace snap_test.Controllers
{
    /// <summary>Books with real ISBN-13s, genres, ratings and availability. Hardcoded in-memory data: writes last until the app restarts.</summary>
    [Route("api/[controller]")]
    public class BooksController : CrudControllerBase<Book>
    {
        /// <inheritdoc />
        protected override List<Book> Store => BookStore.Books;
        /// <inheritdoc />
        protected override string ResourceName => "Book";

        /// <inheritdoc />
        protected override string? Validate(Book item)
        {
            if (item == null) return "Request body is required.";
            if (string.IsNullOrWhiteSpace(item.Title)) return "Missing required field: title";
            if (string.IsNullOrWhiteSpace(item.Author)) return "Missing required field: author";
            if (item.Pages < 0) return "Field 'pages' must be zero or greater.";
            if (item.Price < 0) return "Field 'price' must be zero or greater.";
            if (item.Rating < 0 || item.Rating > 5) return "Field 'rating' must be between 0 and 5.";
            return null;
        }

        // -------------------- GENRES --------------------
        /// <summary>List genres, sorted and without duplicates.</summary>
        /// <response code="200">Genre names.</response>
        [HttpGet("genres")]
        public IActionResult GetGenres()
        {
            lock (Store)
            {
                return Ok(Store.Select(b => b.Genre).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(g => g).ToList());
            }
        }

        // -------------------- AUTHORS (with book counts) --------------------
        /// <summary>List authors with the number of books by each.</summary>
        /// <response code="200">Objects of the form { author, books }, sorted by author.</response>
        [HttpGet("authors")]
        public IActionResult GetAuthors()
        {
            lock (Store)
            {
                return Ok(Store.GroupBy(b => b.Author)
                    .OrderBy(g => g.Key)
                    .Select(g => new { author = g.Key, books = g.Count() })
                    .ToList());
            }
        }

        // -------------------- BY ISBN (hyphens ignored) --------------------
        /// <summary>Get a book by ISBN.</summary>
        /// <param name="isbn">ISBN-13, with or without hyphens.</param>
        /// <response code="200">The book.</response>
        /// <response code="404">No book with this ISBN does not exist.</response>
        [HttpGet("isbn/{isbn}")]
        public IActionResult GetByIsbn(string isbn)
        {
            var normalized = isbn.Replace("-", "").Trim();
            lock (Store)
            {
                var book = Store.FirstOrDefault(b => b.Isbn == normalized);
                return book == null
                    ? NotFound(ApiResponse.Error(404, $"Book with ISBN {isbn} does not exist."))
                    : Ok(book);
            }
        }
    }
}
