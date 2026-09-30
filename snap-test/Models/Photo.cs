using snap_test.Helpers;

namespace snap_test.Models
{
    /// <summary>A photo in an album.</summary>
    public class Photo : IEntity
    {
        /// <summary>Unique ID, assigned by the server.</summary>
        public int Id { get; set; }
        /// <summary>ID of the album it belongs to (positive).</summary>
        public int AlbumId { get; set; }
        /// <summary>Caption (required).</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>Full-size image URL (absolute).</summary>
        public string Url { get; set; } = string.Empty;
        /// <summary>Thumbnail image URL.</summary>
        public string ThumbnailUrl { get; set; } = string.Empty;
    }
}
