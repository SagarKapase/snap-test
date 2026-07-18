using snap_test.Models;

namespace snap_test.Data
{
    /// <summary>
    /// In-memory seed store for orders (12 records). Statuses: pending, processing, shipped, delivered, cancelled.
    /// ProductId references real Products; UserId references existing Users (101-110).
    /// Writes mutate this static list and reset on app restart (non-persistent, per PRD).
    /// </summary>
    public static class OrderStore
    {
        public static List<Order> Orders = new()
        {
            new Order { Id = 1, UserId = 101, Total = 89.99m, Status = "delivered", OrderedAt = "2025-07-05T09:00:00Z", DeliveredAt = "2025-07-09T14:30:00Z",
                Items = new() { new OrderItem { ProductId = 8, Title = "Running Sneakers", Quantity = 1, Price = 89.99m } },
                ShippingAddress = new() { Street = "123 Main St", City = "New York", State = "NY", Zip = "10001", Country = "US" } },
            new Order { Id = 2, UserId = 102, Total = 249.99m, Status = "shipped", OrderedAt = "2025-07-10T11:15:00Z", DeliveredAt = null,
                Items = new() { new OrderItem { ProductId = 1, Title = "Wireless Noise-Cancelling Headphones", Quantity = 1, Price = 249.99m } },
                ShippingAddress = new() { Street = "456 Market St", City = "San Francisco", State = "CA", Zip = "94103", Country = "US" } },
            new Order { Id = 3, UserId = 103, Total = 62.98m, Status = "processing", OrderedAt = "2025-07-12T13:40:00Z", DeliveredAt = null,
                Items = new() { new OrderItem { ProductId = 10, Title = "Clean Code: A Handbook of Agile Software Craftsmanship", Quantity = 1, Price = 34.99m }, new OrderItem { ProductId = 12, Title = "Project Hail Mary", Quantity = 1, Price = 27.99m } },
                ShippingAddress = new() { Street = "789 King St W", City = "Toronto", State = "ON", Zip = "M5V 1N5", Country = "CA" } },
            new Order { Id = 4, UserId = 104, Total = 49.98m, Status = "pending", OrderedAt = "2025-07-14T08:25:00Z", DeliveredAt = null,
                Items = new() { new OrderItem { ProductId = 18, Title = "Insulated Water Bottle", Quantity = 2, Price = 24.99m } },
                ShippingAddress = new() { Street = "12 Carrer de Balmes", City = "Barcelona", State = "CT", Zip = "08007", Country = "ES" } },
            new Order { Id = 5, UserId = 105, Total = 199.99m, Status = "cancelled", OrderedAt = "2025-07-08T16:50:00Z", DeliveredAt = null,
                Items = new() { new OrderItem { ProductId = 2, Title = "Smartwatch Series 6", Quantity = 1, Price = 199.99m } },
                ShippingAddress = new() { Street = "34 Baker Street", City = "London", State = "", Zip = "W1U 3BW", Country = "GB" } },
            new Order { Id = 6, UserId = 106, Total = 59.97m, Status = "delivered", OrderedAt = "2025-07-06T10:10:00Z", DeliveredAt = "2025-07-11T12:00:00Z",
                Items = new() { new OrderItem { ProductId = 6, Title = "Cotton Crew T-Shirt", Quantity = 3, Price = 19.99m } },
                ShippingAddress = new() { Street = "5 Alexanderplatz", City = "Berlin", State = "", Zip = "10178", Country = "DE" } },
            new Order { Id = 7, UserId = 107, Total = 52.98m, Status = "shipped", OrderedAt = "2025-07-13T14:00:00Z", DeliveredAt = null,
                Items = new() { new OrderItem { ProductId = 14, Title = "LED Desk Lamp", Quantity = 1, Price = 29.99m }, new OrderItem { ProductId = 17, Title = "Bamboo Notebook Stand", Quantity = 1, Price = 22.99m } },
                ShippingAddress = new() { Street = "901 Pine St", City = "Seattle", State = "WA", Zip = "98101", Country = "US" } },
            new Order { Id = 8, UserId = 108, Total = 39.99m, Status = "delivered", OrderedAt = "2025-07-04T09:30:00Z", DeliveredAt = "2025-07-08T15:20:00Z",
                Items = new() { new OrderItem { ProductId = 9, Title = "Canvas Backpack", Quantity = 1, Price = 39.99m } },
                ShippingAddress = new() { Street = "22 George St", City = "Sydney", State = "NSW", Zip = "2000", Country = "AU" } },
            new Order { Id = 9, UserId = 109, Total = 89.99m, Status = "processing", OrderedAt = "2025-07-15T11:45:00Z", DeliveredAt = null,
                Items = new() { new OrderItem { ProductId = 3, Title = "Mechanical Keyboard", Quantity = 1, Price = 89.99m } },
                ShippingAddress = new() { Street = "7 Herengracht", City = "Amsterdam", State = "", Zip = "1015 BA", Country = "NL" } },
            new Order { Id = 10, UserId = 110, Total = 54.98m, Status = "pending", OrderedAt = "2025-07-16T10:20:00Z", DeliveredAt = null,
                Items = new() { new OrderItem { ProductId = 19, Title = "Non-Slip Yoga Mat", Quantity = 1, Price = 34.99m }, new OrderItem { ProductId = 20, Title = "Resistance Bands Set", Quantity = 1, Price = 19.99m } },
                ShippingAddress = new() { Street = "3 Grafton St", City = "Dublin", State = "", Zip = "D02 XY45", Country = "IE" } },
            new Order { Id = 11, UserId = 101, Total = 34.99m, Status = "delivered", OrderedAt = "2025-07-02T13:00:00Z", DeliveredAt = "2025-07-06T11:10:00Z",
                Items = new() { new OrderItem { ProductId = 5, Title = "7-Port USB-C Hub", Quantity = 1, Price = 34.99m } },
                ShippingAddress = new() { Street = "123 Main St", City = "New York", State = "NY", Zip = "10001", Country = "US" } },
            new Order { Id = 12, UserId = 102, Total = 24.99m, Status = "shipped", OrderedAt = "2025-07-14T15:30:00Z", DeliveredAt = null,
                Items = new() { new OrderItem { ProductId = 11, Title = "The Design of Everyday Things", Quantity = 1, Price = 24.99m } },
                ShippingAddress = new() { Street = "456 Market St", City = "San Francisco", State = "CA", Zip = "94103", Country = "US" } }
        };
    }
}
