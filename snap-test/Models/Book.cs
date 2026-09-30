using snap_test.Helpers;

namespace snap_test.Models
{
    /// <summary>A book.</summary>
    public class Book : IEntity
    {
        /// <summary>Unique ID, assigned by the server.</summary>
        public int Id { get; set; }
        /// <summary>Title (required).</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>Author (required).</summary>
        public string Author { get; set; } = string.Empty;
        /// <summary>ISBN-13 without hyphens.</summary>
        public string Isbn { get; set; } = string.Empty;
        /// <summary>Genre, e.g. fantasy.</summary>
        public string Genre { get; set; } = string.Empty;
        /// <summary>Year first published.</summary>
        public int PublishedYear { get; set; }
        /// <summary>Page count; zero or greater.</summary>
        public int Pages { get; set; }
        /// <summary>Original language.</summary>
        public string Language { get; set; } = string.Empty;
        /// <summary>Price in USD; zero or greater.</summary>
        public decimal Price { get; set; }
        /// <summary>Average rating from 0 to 5.</summary>
        public double Rating { get; set; }
        /// <summary>Whether the book is in stock.</summary>
        public bool Available { get; set; }
        /// <summary>Free-form tags.</summary>
        public List<string> Tags { get; set; } = new();
    }
}
