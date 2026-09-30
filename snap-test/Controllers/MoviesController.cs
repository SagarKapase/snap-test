using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Models;

namespace snap_test.Controllers
{
    /// <summary>Well-known movies with directors, genres, cast, ratings and box office. Hardcoded in-memory data: writes last until the app restarts.</summary>
    [Route("api/[controller]")]
    public class MoviesController : CrudControllerBase<Movie>
    {
        /// <inheritdoc />
        protected override List<Movie> Store => MovieStore.Movies;
        /// <inheritdoc />
        protected override string ResourceName => "Movie";

        /// <inheritdoc />
        protected override string? Validate(Movie item)
        {
            if (item == null) return "Request body is required.";
            if (string.IsNullOrWhiteSpace(item.Title)) return "Missing required field: title";
            if (string.IsNullOrWhiteSpace(item.Director)) return "Missing required field: director";
            if (item.Year != 0 && (item.Year < 1888 || item.Year > DateTime.UtcNow.Year + 5))
                return "Field 'year' must be between 1888 and five years from now.";
            if (item.Rating < 0 || item.Rating > 10) return "Field 'rating' must be between 0 and 10.";
            if (item.RuntimeMinutes < 0) return "Field 'runtimeMinutes' must be zero or greater.";
            return null;
        }

        // -------------------- GENRES --------------------
        /// <summary>List every genre used by any movie, sorted and without duplicates.</summary>
        /// <response code="200">Genre names.</response>
        [HttpGet("genres")]
        public IActionResult GetGenres()
        {
            lock (Store)
            {
                return Ok(Store.SelectMany(m => m.Genres).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(g => g).ToList());
            }
        }

        // -------------------- TOP RATED --------------------
        /// <summary>List the highest-rated movies.</summary>
        /// <param name="limit">How many to return, clamped to 1-100 (default 10).</param>
        /// <response code="200">Movies sorted by rating, highest first.</response>
        /// <response code="400">limit is not an integer.</response>
        [HttpGet("top")]
        public IActionResult GetTop([FromQuery] int limit = 10)
        {
            limit = Math.Clamp(limit, 1, 100);
            lock (Store)
            {
                return Ok(Store.OrderByDescending(m => m.Rating).ThenBy(m => m.Title).Take(limit).ToList());
            }
        }

        // -------------------- BY YEAR --------------------
        /// <summary>List movies released in a given year.</summary>
        /// <param name="year">Release year, e.g. 1994.</param>
        /// <response code="200">Matching movies (an empty array if none).</response>
        [HttpGet("year/{year:int}")]
        public IActionResult GetByYear(int year)
        {
            lock (Store)
            {
                return Ok(Store.Where(m => m.Year == year).ToList());
            }
        }
    }
}
