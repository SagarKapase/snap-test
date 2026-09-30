using snap_test.Helpers;

namespace snap_test.Models
{
    /// <summary>A photo album.</summary>
    public class Album : IEntity
    {
        /// <summary>Unique ID, assigned by the server.</summary>
        public int Id { get; set; }
        /// <summary>Owner user ID (positive; 101-110 in the seed data).</summary>
        public int UserId { get; set; }
        /// <summary>Album title (required).</summary>
        public string Title { get; set; } = string.Empty;
    }
}
