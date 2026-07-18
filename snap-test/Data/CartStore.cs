using snap_test.Models;

namespace snap_test.Data
{
    /// <summary>
    /// In-memory seed store for carts (8 records, 1-4 items each). ProductId references real Products (1-20);
    /// UserId references existing Users (101-110). Totals match the seeded items.
    /// Writes mutate this static list and reset on app restart (non-persistent, per PRD).
    /// </summary>
    public static class CartStore
    {
        public static List<Cart> Carts = new()
        {
            new Cart { Id = 1, UserId = 101, Total = 229.97m, UpdatedAt = "2025-07-16T12:00:00Z", Items = new()
                { new CartItem { ProductId = 3, Quantity = 2, Price = 89.99m }, new CartItem { ProductId = 7, Quantity = 1, Price = 49.99m } } },
            new Cart { Id = 2, UserId = 102, Total = 249.99m, UpdatedAt = "2025-07-16T13:30:00Z", Items = new()
                { new CartItem { ProductId = 1, Quantity = 1, Price = 249.99m } } },
            new Cart { Id = 3, UserId = 103, Total = 115.96m, UpdatedAt = "2025-07-16T14:10:00Z", Items = new()
                { new CartItem { ProductId = 10, Quantity = 1, Price = 34.99m }, new CartItem { ProductId = 11, Quantity = 1, Price = 24.99m }, new CartItem { ProductId = 12, Quantity = 2, Price = 27.99m } } },
            new Cart { Id = 4, UserId = 104, Total = 119.95m, UpdatedAt = "2025-07-16T15:45:00Z", Items = new()
                { new CartItem { ProductId = 18, Quantity = 1, Price = 24.99m }, new CartItem { ProductId = 19, Quantity = 1, Price = 34.99m }, new CartItem { ProductId = 20, Quantity = 3, Price = 19.99m } } },
            new Cart { Id = 5, UserId = 105, Total = 269.97m, UpdatedAt = "2025-07-17T09:20:00Z", Items = new()
                { new CartItem { ProductId = 2, Quantity = 1, Price = 199.99m }, new CartItem { ProductId = 5, Quantity = 2, Price = 34.99m } } },
            new Cart { Id = 6, UserId = 106, Total = 79.96m, UpdatedAt = "2025-07-17T10:05:00Z", Items = new()
                { new CartItem { ProductId = 6, Quantity = 4, Price = 19.99m } } },
            new Cart { Id = 7, UserId = 107, Total = 95.95m, UpdatedAt = "2025-07-17T11:40:00Z", Items = new()
                { new CartItem { ProductId = 14, Quantity = 1, Price = 29.99m }, new CartItem { ProductId = 15, Quantity = 2, Price = 12.99m }, new CartItem { ProductId = 16, Quantity = 1, Price = 16.99m }, new CartItem { ProductId = 17, Quantity = 1, Price = 22.99m } } },
            new Cart { Id = 8, UserId = 108, Total = 129.98m, UpdatedAt = "2025-07-17T13:15:00Z", Items = new()
                { new CartItem { ProductId = 8, Quantity = 1, Price = 89.99m }, new CartItem { ProductId = 9, Quantity = 1, Price = 39.99m } } }
        };
    }
}
