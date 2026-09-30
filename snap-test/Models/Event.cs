using snap_test.Helpers;

namespace snap_test.Models
{
    /// <summary>A ticketed event.</summary>
    public class Event : IEntity
    {
        /// <summary>Unique ID, assigned by the server.</summary>
        public int Id { get; set; }
        /// <summary>Event name (required).</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Category, e.g. conference.</summary>
        public string Category { get; set; } = string.Empty;
        /// <summary>Venue name.</summary>
        public string Venue { get; set; } = string.Empty;
        /// <summary>City.</summary>
        public string City { get; set; } = string.Empty;
        /// <summary>Start time, ISO 8601 UTC (required).</summary>
        public string StartsAt { get; set; } = string.Empty;
        /// <summary>End time, ISO 8601 UTC; must not be before StartsAt.</summary>
        public string EndsAt { get; set; } = string.Empty;
        /// <summary>Ticket price in USD; 0 means free.</summary>
        public decimal Price { get; set; }
        /// <summary>Maximum attendees.</summary>
        public int Capacity { get; set; }
        /// <summary>Tickets sold; cannot exceed Capacity.</summary>
        public int TicketsSold { get; set; }
        /// <summary>Organizer name.</summary>
        public string Organizer { get; set; } = string.Empty;
        /// <summary>Free-form tags.</summary>
        public List<string> Tags { get; set; } = new();
    }
}
