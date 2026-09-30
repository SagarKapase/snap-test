using snap_test.Models;

namespace snap_test.Data
{
    /// <summary>
    /// In-memory seed store for payments (12 records). Used by the Idempotency-Key demo.
    /// Writes mutate this static list and reset on app restart (non-persistent, per PRD).
    /// </summary>
    public static class PaymentStore
    {
        public static List<Payment> Payments = new()
        {
            new Payment { Id = 1, UserId = 101, OrderId = 1, Amount = 249.99m, Currency = "USD", Method = "card", Status = "succeeded", Description = "Order #1 — headphones", CardLast4 = "4242", CreatedAt = "2025-07-01T10:15:00Z" },
            new Payment { Id = 2, UserId = 102, OrderId = 2, Amount = 89.99m, Currency = "USD", Method = "paypal", Status = "succeeded", Description = "Order #2 — keyboard", CreatedAt = "2025-07-02T12:40:00Z" },
            new Payment { Id = 3, UserId = 103, OrderId = 3, Amount = 1499.00m, Currency = "INR", Method = "upi", Status = "succeeded", Description = "Order #3 — coffee mugs", CreatedAt = "2025-07-03T09:05:00Z" },
            new Payment { Id = 4, UserId = 104, OrderId = 4, Amount = 59.99m, Currency = "USD", Method = "card", Status = "failed", Description = "Order #4 — webcam (card declined)", CardLast4 = "0002", CreatedAt = "2025-07-04T14:22:00Z" },
            new Payment { Id = 5, UserId = 105, OrderId = 5, Amount = 34.99m, Currency = "EUR", Method = "card", Status = "refunded", Description = "Order #5 — USB-C hub", CardLast4 = "5556", CreatedAt = "2025-07-05T16:10:00Z", RefundedAt = "2025-07-08T11:00:00Z" },
            new Payment { Id = 6, UserId = 106, OrderId = 6, Amount = 120.50m, Currency = "GBP", Method = "bank_transfer", Status = "pending", Description = "Order #6 — hoodie bundle", CreatedAt = "2025-07-06T08:30:00Z" },
            new Payment { Id = 7, UserId = 107, OrderId = 7, Amount = 27.99m, Currency = "USD", Method = "card", Status = "succeeded", Description = "Order #7 — Project Hail Mary", CardLast4 = "1881", CreatedAt = "2025-07-07T19:45:00Z" },
            new Payment { Id = 8, UserId = 108, OrderId = 8, Amount = 74.97m, Currency = "USD", Method = "paypal", Status = "succeeded", Description = "Order #8 — home essentials", CreatedAt = "2025-07-08T13:12:00Z" },
            new Payment { Id = 9, UserId = 109, OrderId = null, Amount = 9.99m, Currency = "USD", Method = "card", Status = "succeeded", Description = "Monthly subscription — Basic plan", CardLast4 = "4242", CreatedAt = "2025-07-09T00:00:00Z" },
            new Payment { Id = 10, UserId = 110, OrderId = 10, Amount = 3250.00m, Currency = "INR", Method = "upi", Status = "failed", Description = "Order #10 — yoga mat (UPI timeout)", CreatedAt = "2025-07-10T10:50:00Z" },
            new Payment { Id = 11, UserId = 101, OrderId = 11, Amount = 199.99m, Currency = "USD", Method = "card", Status = "refunded", Description = "Order #11 — smartwatch (returned)", CardLast4 = "3184", CreatedAt = "2025-07-11T15:35:00Z", RefundedAt = "2025-07-15T09:20:00Z" },
            new Payment { Id = 12, UserId = 102, OrderId = null, Amount = 19.99m, Currency = "EUR", Method = "card", Status = "pending", Description = "Monthly subscription — Pro plan", CardLast4 = "8210", CreatedAt = "2025-07-12T00:00:00Z" }
        };
    }
}
