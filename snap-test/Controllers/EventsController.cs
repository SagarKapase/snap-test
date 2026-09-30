using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Helpers;
using snap_test.Models;

namespace snap_test.Controllers
{
    /// <summary>Conferences, concerts, workshops and other events, some sold out and some free. Hardcoded in-memory data: writes last until the app restarts.</summary>
    [Route("api/[controller]")]
    public class EventsController : CrudControllerBase<Event>
    {
        /// <inheritdoc />
        protected override List<Event> Store => EventStore.Events;
        /// <inheritdoc />
        protected override string ResourceName => "Event";

        /// <inheritdoc />
        protected override string? Validate(Event item)
        {
            if (item == null) return "Request body is required.";
            if (string.IsNullOrWhiteSpace(item.Name)) return "Missing required field: name";
            if (!TryParse(item.StartsAt, out var starts)) return "Field 'startsAt' must be an ISO 8601 date-time.";
            if (!string.IsNullOrEmpty(item.EndsAt))
            {
                if (!TryParse(item.EndsAt, out var ends)) return "Field 'endsAt' must be an ISO 8601 date-time.";
                if (ends < starts) return "Field 'endsAt' must be after 'startsAt'.";
            }
            if (item.Price < 0) return "Field 'price' must be zero or greater.";
            if (item.Capacity < 0 || item.TicketsSold < 0) return "Fields 'capacity' and 'ticketsSold' must be zero or greater.";
            if (item.TicketsSold > item.Capacity) return "Field 'ticketsSold' cannot exceed 'capacity'.";
            return null;
        }

        // -------------------- UPCOMING (?from= ISO date, default now) --------------------
        /// <summary>List events starting on or after a date, soonest first.</summary>
        /// <param name="from">ISO 8601 date or date-time (default: now, UTC).</param>
        /// <response code="200">Matching events sorted by start time.</response>
        /// <response code="400">from is not a valid date.</response>
        [HttpGet("upcoming")]
        public IActionResult GetUpcoming([FromQuery] string? from = null)
        {
            var since = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(from) && !TryParse(from, out since))
                return BadRequest(ApiResponse.Error(400, "Query parameter 'from' must be an ISO 8601 date, e.g. ?from=2026-10-01"));

            lock (Store)
            {
                return Ok(Store
                    .Where(e => TryParse(e.StartsAt, out var s) && s >= since)
                    .OrderBy(e => e.StartsAt, StringComparer.Ordinal)
                    .ToList());
            }
        }

        /// <summary>Parse an ISO 8601 date as UTC.</summary>
        /// <param name="value">Text to parse.</param>
        /// <param name="result">The parsed UTC time.</param>
        /// <returns>True when the text is a valid date.</returns>
        private static bool TryParse(string? value, out DateTime result) =>
            DateTime.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out result);
    }
}
