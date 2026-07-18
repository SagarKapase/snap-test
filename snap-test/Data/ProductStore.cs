using snap_test.Models;

namespace snap_test.Data
{
    /// <summary>
    /// In-memory seed store for products (20 records across 5 categories).
    /// Writes mutate this static list and reset on app restart (non-persistent, per PRD).
    /// </summary>
    public static class ProductStore
    {
        public static List<Product> Products = new()
        {
            // ---- Electronics (5) ----
            new Product { Id = 1, Title = "Wireless Noise-Cancelling Headphones", Price = 249.99m, Description = "Over-ear headphones with 30hr battery life and active noise cancellation.", Category = "electronics", Image = "https://picsum.photos/seed/prod1/400/400", Rating = new Rating { Rate = 4.3, Count = 127 }, InStock = true, CreatedAt = "2025-06-12T10:30:00Z" },
            new Product { Id = 2, Title = "Smartwatch Series 6", Price = 199.99m, Description = "Fitness tracking, heart-rate monitor, and always-on OLED display.", Category = "electronics", Image = "https://picsum.photos/seed/prod2/400/400", Rating = new Rating { Rate = 4.1, Count = 98 }, InStock = true, CreatedAt = "2025-06-14T09:15:00Z" },
            new Product { Id = 3, Title = "Mechanical Keyboard", Price = 89.99m, Description = "Hot-swappable RGB mechanical keyboard with tactile brown switches.", Category = "electronics", Image = "https://picsum.photos/seed/prod3/400/400", Rating = new Rating { Rate = 4.6, Count = 214 }, InStock = true, CreatedAt = "2025-06-15T14:40:00Z" },
            new Product { Id = 4, Title = "1080p HD Webcam", Price = 59.99m, Description = "Full-HD webcam with auto-focus and a built-in noise-reducing mic.", Category = "electronics", Image = "https://picsum.photos/seed/prod4/400/400", Rating = new Rating { Rate = 3.9, Count = 63 }, InStock = false, CreatedAt = "2025-06-18T11:05:00Z" },
            new Product { Id = 5, Title = "7-Port USB-C Hub", Price = 34.99m, Description = "Aluminium USB-C hub with HDMI, SD card reader, and 100W passthrough.", Category = "electronics", Image = "https://picsum.photos/seed/prod5/400/400", Rating = new Rating { Rate = 4.2, Count = 141 }, InStock = true, CreatedAt = "2025-06-20T16:20:00Z" },

            // ---- Clothing (4) ----
            new Product { Id = 6, Title = "Cotton Crew T-Shirt", Price = 19.99m, Description = "Soft 100% organic cotton crew-neck tee. Available in multiple colours.", Category = "clothing", Image = "https://picsum.photos/seed/prod6/400/400", Rating = new Rating { Rate = 4.0, Count = 302 }, InStock = true, CreatedAt = "2025-06-21T08:00:00Z" },
            new Product { Id = 7, Title = "Fleece Pullover Hoodie", Price = 49.99m, Description = "Midweight fleece hoodie with kangaroo pocket and ribbed cuffs.", Category = "clothing", Image = "https://picsum.photos/seed/prod7/400/400", Rating = new Rating { Rate = 4.4, Count = 187 }, InStock = true, CreatedAt = "2025-06-22T13:30:00Z" },
            new Product { Id = 8, Title = "Running Sneakers", Price = 89.99m, Description = "Lightweight breathable running shoes with responsive foam cushioning.", Category = "clothing", Image = "https://picsum.photos/seed/prod8/400/400", Rating = new Rating { Rate = 4.5, Count = 256 }, InStock = true, CreatedAt = "2025-06-24T10:45:00Z" },
            new Product { Id = 9, Title = "Canvas Backpack", Price = 39.99m, Description = "Water-resistant canvas backpack with padded 15\" laptop sleeve.", Category = "clothing", Image = "https://picsum.photos/seed/prod9/400/400", Rating = new Rating { Rate = 4.2, Count = 119 }, InStock = false, CreatedAt = "2025-06-25T15:10:00Z" },

            // ---- Books (4) ----
            new Product { Id = 10, Title = "Clean Code: A Handbook of Agile Software Craftsmanship", Price = 34.99m, Description = "A must-read on writing readable, maintainable code.", Category = "books", Image = "https://picsum.photos/seed/prod10/400/400", Rating = new Rating { Rate = 4.7, Count = 431 }, InStock = true, CreatedAt = "2025-06-26T09:00:00Z" },
            new Product { Id = 11, Title = "The Design of Everyday Things", Price = 24.99m, Description = "Don Norman's classic on human-centred design.", Category = "books", Image = "https://picsum.photos/seed/prod11/400/400", Rating = new Rating { Rate = 4.6, Count = 289 }, InStock = true, CreatedAt = "2025-06-27T12:25:00Z" },
            new Product { Id = 12, Title = "Project Hail Mary", Price = 27.99m, Description = "A lone astronaut must save humanity in this gripping sci-fi novel.", Category = "books", Image = "https://picsum.photos/seed/prod12/400/400", Rating = new Rating { Rate = 4.8, Count = 512 }, InStock = true, CreatedAt = "2025-06-28T18:40:00Z" },
            new Product { Id = 13, Title = "Zero to One", Price = 21.99m, Description = "Peter Thiel's notes on startups and building the future.", Category = "books", Image = "https://picsum.photos/seed/prod13/400/400", Rating = new Rating { Rate = 4.3, Count = 176 }, InStock = false, CreatedAt = "2025-06-29T11:55:00Z" },

            // ---- Home (4) ----
            new Product { Id = 14, Title = "LED Desk Lamp", Price = 29.99m, Description = "Dimmable LED desk lamp with adjustable arm and USB charging port.", Category = "home", Image = "https://picsum.photos/seed/prod14/400/400", Rating = new Rating { Rate = 4.1, Count = 143 }, InStock = true, CreatedAt = "2025-07-01T08:20:00Z" },
            new Product { Id = 15, Title = "Ceramic Coffee Mug", Price = 12.99m, Description = "350ml stoneware mug with a comfortable handle. Dishwasher safe.", Category = "home", Image = "https://picsum.photos/seed/prod15/400/400", Rating = new Rating { Rate = 4.0, Count = 88 }, InStock = true, CreatedAt = "2025-07-02T14:15:00Z" },
            new Product { Id = 16, Title = "Terracotta Plant Pot", Price = 16.99m, Description = "Hand-finished terracotta pot with drainage tray. 6-inch diameter.", Category = "home", Image = "https://picsum.photos/seed/prod16/400/400", Rating = new Rating { Rate = 4.4, Count = 74 }, InStock = true, CreatedAt = "2025-07-03T10:35:00Z" },
            new Product { Id = 17, Title = "Bamboo Notebook Stand", Price = 22.99m, Description = "Ergonomic bamboo laptop/notebook stand with cable management.", Category = "home", Image = "https://picsum.photos/seed/prod17/400/400", Rating = new Rating { Rate = 4.2, Count = 101 }, InStock = true, CreatedAt = "2025-07-04T16:50:00Z" },

            // ---- Sports (3) ----
            new Product { Id = 18, Title = "Insulated Water Bottle", Price = 24.99m, Description = "1L vacuum-insulated stainless steel bottle. Keeps drinks cold 24h.", Category = "sports", Image = "https://picsum.photos/seed/prod18/400/400", Rating = new Rating { Rate = 4.5, Count = 233 }, InStock = true, CreatedAt = "2025-07-05T09:10:00Z" },
            new Product { Id = 19, Title = "Non-Slip Yoga Mat", Price = 34.99m, Description = "6mm cushioned TPE yoga mat with alignment lines and carry strap.", Category = "sports", Image = "https://picsum.photos/seed/prod19/400/400", Rating = new Rating { Rate = 4.3, Count = 158 }, InStock = true, CreatedAt = "2025-07-06T13:00:00Z" },
            new Product { Id = 20, Title = "Resistance Bands Set", Price = 19.99m, Description = "Set of 5 latex resistance bands with door anchor and carry bag.", Category = "sports", Image = "https://picsum.photos/seed/prod20/400/400", Rating = new Rating { Rate = 4.1, Count = 96 }, InStock = false, CreatedAt = "2025-07-07T17:45:00Z" }
        };
    }
}
