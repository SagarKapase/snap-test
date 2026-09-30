using snap_test.Helpers;

namespace snap_test.Models
{
    /// <summary>A movie.</summary>
    public class Movie : IEntity
    {
        /// <summary>Unique ID, assigned by the server.</summary>
        public int Id { get; set; }
        /// <summary>Title (required).</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>Director (required).</summary>
        public string Director { get; set; } = string.Empty;
        /// <summary>Release year (1888 to five years from now, or 0 if unknown).</summary>
        public int Year { get; set; }
        /// <summary>Genres, e.g. Sci-Fi.</summary>
        public List<string> Genres { get; set; } = new();
        /// <summary>Runtime in minutes; zero or greater.</summary>
        public int RuntimeMinutes { get; set; }
        /// <summary>Rating from 0 to 10.</summary>
        public double Rating { get; set; }
        /// <summary>Leading cast members.</summary>
        public List<string> Cast { get; set; } = new();
        /// <summary>Worldwide box office in USD.</summary>
        public long BoxOffice { get; set; }
        /// <summary>Poster image URL.</summary>
        public string Poster { get; set; } = string.Empty;
    }
}
