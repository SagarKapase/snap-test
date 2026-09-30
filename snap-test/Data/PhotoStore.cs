using snap_test.Models;

namespace snap_test.Data
{
    /// <summary>
    /// In-memory seed store for photos (50 records, 5 per album). albumId references AlbumStore.
    /// Writes mutate this static list and reset on app restart (non-persistent, per PRD).
    /// </summary>
    public static class PhotoStore
    {
        public static List<Photo> Photos = new()
        {
            new Photo { Id = 1, AlbumId = 1, Title = "Summer in Lisbon - sunrise #1", Url = "https://picsum.photos/seed/photo1/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo1/150/150" },
            new Photo { Id = 2, AlbumId = 1, Title = "Summer in Lisbon - close-up #2", Url = "https://picsum.photos/seed/photo2/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo2/150/150" },
            new Photo { Id = 3, AlbumId = 1, Title = "Summer in Lisbon - panorama #3", Url = "https://picsum.photos/seed/photo3/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo3/150/150" },
            new Photo { Id = 4, AlbumId = 1, Title = "Summer in Lisbon - candid shot #4", Url = "https://picsum.photos/seed/photo4/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo4/150/150" },
            new Photo { Id = 5, AlbumId = 1, Title = "Summer in Lisbon - detail #5", Url = "https://picsum.photos/seed/photo5/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo5/150/150" },
            new Photo { Id = 6, AlbumId = 2, Title = "Office Dogs - group photo #1", Url = "https://picsum.photos/seed/photo6/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo6/150/150" },
            new Photo { Id = 7, AlbumId = 2, Title = "Office Dogs - night view #2", Url = "https://picsum.photos/seed/photo7/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo7/150/150" },
            new Photo { Id = 8, AlbumId = 2, Title = "Office Dogs - wide angle #3", Url = "https://picsum.photos/seed/photo8/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo8/150/150" },
            new Photo { Id = 9, AlbumId = 2, Title = "Office Dogs - portrait #4", Url = "https://picsum.photos/seed/photo9/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo9/150/150" },
            new Photo { Id = 10, AlbumId = 2, Title = "Office Dogs - landscape #5", Url = "https://picsum.photos/seed/photo10/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo10/150/150" },
            new Photo { Id = 11, AlbumId = 3, Title = "Hiking the Alps - sunrise #1", Url = "https://picsum.photos/seed/photo11/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo11/150/150" },
            new Photo { Id = 12, AlbumId = 3, Title = "Hiking the Alps - close-up #2", Url = "https://picsum.photos/seed/photo12/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo12/150/150" },
            new Photo { Id = 13, AlbumId = 3, Title = "Hiking the Alps - panorama #3", Url = "https://picsum.photos/seed/photo13/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo13/150/150" },
            new Photo { Id = 14, AlbumId = 3, Title = "Hiking the Alps - candid shot #4", Url = "https://picsum.photos/seed/photo14/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo14/150/150" },
            new Photo { Id = 15, AlbumId = 3, Title = "Hiking the Alps - detail #5", Url = "https://picsum.photos/seed/photo15/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo15/150/150" },
            new Photo { Id = 16, AlbumId = 4, Title = "Street Food Tour - group photo #1", Url = "https://picsum.photos/seed/photo16/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo16/150/150" },
            new Photo { Id = 17, AlbumId = 4, Title = "Street Food Tour - night view #2", Url = "https://picsum.photos/seed/photo17/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo17/150/150" },
            new Photo { Id = 18, AlbumId = 4, Title = "Street Food Tour - wide angle #3", Url = "https://picsum.photos/seed/photo18/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo18/150/150" },
            new Photo { Id = 19, AlbumId = 4, Title = "Street Food Tour - portrait #4", Url = "https://picsum.photos/seed/photo19/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo19/150/150" },
            new Photo { Id = 20, AlbumId = 4, Title = "Street Food Tour - landscape #5", Url = "https://picsum.photos/seed/photo20/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo20/150/150" },
            new Photo { Id = 21, AlbumId = 5, Title = "Wedding Day - sunrise #1", Url = "https://picsum.photos/seed/photo21/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo21/150/150" },
            new Photo { Id = 22, AlbumId = 5, Title = "Wedding Day - close-up #2", Url = "https://picsum.photos/seed/photo22/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo22/150/150" },
            new Photo { Id = 23, AlbumId = 5, Title = "Wedding Day - panorama #3", Url = "https://picsum.photos/seed/photo23/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo23/150/150" },
            new Photo { Id = 24, AlbumId = 5, Title = "Wedding Day - candid shot #4", Url = "https://picsum.photos/seed/photo24/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo24/150/150" },
            new Photo { Id = 25, AlbumId = 5, Title = "Wedding Day - detail #5", Url = "https://picsum.photos/seed/photo25/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo25/150/150" },
            new Photo { Id = 26, AlbumId = 6, Title = "Product Launch 2026 - group photo #1", Url = "https://picsum.photos/seed/photo26/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo26/150/150" },
            new Photo { Id = 27, AlbumId = 6, Title = "Product Launch 2026 - night view #2", Url = "https://picsum.photos/seed/photo27/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo27/150/150" },
            new Photo { Id = 28, AlbumId = 6, Title = "Product Launch 2026 - wide angle #3", Url = "https://picsum.photos/seed/photo28/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo28/150/150" },
            new Photo { Id = 29, AlbumId = 6, Title = "Product Launch 2026 - portrait #4", Url = "https://picsum.photos/seed/photo29/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo29/150/150" },
            new Photo { Id = 30, AlbumId = 6, Title = "Product Launch 2026 - landscape #5", Url = "https://picsum.photos/seed/photo30/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo30/150/150" },
            new Photo { Id = 31, AlbumId = 7, Title = "Northern Lights - sunrise #1", Url = "https://picsum.photos/seed/photo31/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo31/150/150" },
            new Photo { Id = 32, AlbumId = 7, Title = "Northern Lights - close-up #2", Url = "https://picsum.photos/seed/photo32/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo32/150/150" },
            new Photo { Id = 33, AlbumId = 7, Title = "Northern Lights - panorama #3", Url = "https://picsum.photos/seed/photo33/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo33/150/150" },
            new Photo { Id = 34, AlbumId = 7, Title = "Northern Lights - candid shot #4", Url = "https://picsum.photos/seed/photo34/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo34/150/150" },
            new Photo { Id = 35, AlbumId = 7, Title = "Northern Lights - detail #5", Url = "https://picsum.photos/seed/photo35/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo35/150/150" },
            new Photo { Id = 36, AlbumId = 8, Title = "Garden Progress - group photo #1", Url = "https://picsum.photos/seed/photo36/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo36/150/150" },
            new Photo { Id = 37, AlbumId = 8, Title = "Garden Progress - night view #2", Url = "https://picsum.photos/seed/photo37/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo37/150/150" },
            new Photo { Id = 38, AlbumId = 8, Title = "Garden Progress - wide angle #3", Url = "https://picsum.photos/seed/photo38/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo38/150/150" },
            new Photo { Id = 39, AlbumId = 8, Title = "Garden Progress - portrait #4", Url = "https://picsum.photos/seed/photo39/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo39/150/150" },
            new Photo { Id = 40, AlbumId = 8, Title = "Garden Progress - landscape #5", Url = "https://picsum.photos/seed/photo40/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo40/150/150" },
            new Photo { Id = 41, AlbumId = 9, Title = "Road Trip: Route 66 - sunrise #1", Url = "https://picsum.photos/seed/photo41/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo41/150/150" },
            new Photo { Id = 42, AlbumId = 9, Title = "Road Trip: Route 66 - close-up #2", Url = "https://picsum.photos/seed/photo42/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo42/150/150" },
            new Photo { Id = 43, AlbumId = 9, Title = "Road Trip: Route 66 - panorama #3", Url = "https://picsum.photos/seed/photo43/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo43/150/150" },
            new Photo { Id = 44, AlbumId = 9, Title = "Road Trip: Route 66 - candid shot #4", Url = "https://picsum.photos/seed/photo44/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo44/150/150" },
            new Photo { Id = 45, AlbumId = 9, Title = "Road Trip: Route 66 - detail #5", Url = "https://picsum.photos/seed/photo45/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo45/150/150" },
            new Photo { Id = 46, AlbumId = 10, Title = "Architecture Walk - group photo #1", Url = "https://picsum.photos/seed/photo46/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo46/150/150" },
            new Photo { Id = 47, AlbumId = 10, Title = "Architecture Walk - night view #2", Url = "https://picsum.photos/seed/photo47/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo47/150/150" },
            new Photo { Id = 48, AlbumId = 10, Title = "Architecture Walk - wide angle #3", Url = "https://picsum.photos/seed/photo48/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo48/150/150" },
            new Photo { Id = 49, AlbumId = 10, Title = "Architecture Walk - portrait #4", Url = "https://picsum.photos/seed/photo49/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo49/150/150" },
            new Photo { Id = 50, AlbumId = 10, Title = "Architecture Walk - landscape #5", Url = "https://picsum.photos/seed/photo50/600/600", ThumbnailUrl = "https://picsum.photos/seed/photo50/150/150" }
        };
    }
}
