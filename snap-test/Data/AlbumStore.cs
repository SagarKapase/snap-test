using snap_test.Models;

namespace snap_test.Data
{
    /// <summary>
    /// In-memory seed store for albums (10 records, JSONPlaceholder style). userId references existing Users (101-110); each album has 5 photos in PhotoStore.
    /// Writes mutate this static list and reset on app restart (non-persistent, per PRD).
    /// </summary>
    public static class AlbumStore
    {
        public static List<Album> Albums = new()
        {
            new Album { Id = 1, UserId = 101, Title = "Summer in Lisbon" },
            new Album { Id = 2, UserId = 101, Title = "Office Dogs" },
            new Album { Id = 3, UserId = 102, Title = "Hiking the Alps" },
            new Album { Id = 4, UserId = 103, Title = "Street Food Tour" },
            new Album { Id = 5, UserId = 104, Title = "Wedding Day" },
            new Album { Id = 6, UserId = 105, Title = "Product Launch 2026" },
            new Album { Id = 7, UserId = 106, Title = "Northern Lights" },
            new Album { Id = 8, UserId = 107, Title = "Garden Progress" },
            new Album { Id = 9, UserId = 108, Title = "Road Trip: Route 66" },
            new Album { Id = 10, UserId = 110, Title = "Architecture Walk" }
        };
    }
}
