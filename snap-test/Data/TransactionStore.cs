using snap_test.Models;

namespace snap_test.Data
{
    /// <summary>
    /// In-memory seed store for transactions (40 records, ~4 per user). Categories: food, transport,
    /// shopping, entertainment, salary, transfer, bills. Credits are positive amounts, debits negative,
    /// with a per-user running balance. UserId references existing Users (101-110).
    /// </summary>
    public static class TransactionStore
    {
        public static List<Transaction> Transactions = new()
        {
            // ---- User 101 ----
            new Transaction { Id = 1, UserId = 101, Type = "credit", Amount = 3200.00m, Currency = "USD", Description = "Monthly Salary", Merchant = "Acme Corp", Category = "salary", Date = "2025-07-01T09:00:00Z", Balance = 3200.00m },
            new Transaction { Id = 2, UserId = 101, Type = "debit", Amount = -4.50m, Currency = "USD", Description = "Morning Coffee", Merchant = "Blue Bottle Coffee", Category = "food", Date = "2025-07-02T08:15:00Z", Balance = 3195.50m },
            new Transaction { Id = 3, UserId = 101, Type = "debit", Amount = -85.20m, Currency = "USD", Description = "Weekly Groceries", Merchant = "Whole Foods", Category = "shopping", Date = "2025-07-03T18:30:00Z", Balance = 3110.30m },
            new Transaction { Id = 4, UserId = 101, Type = "debit", Amount = -18.75m, Currency = "USD", Description = "Ride to Office", Merchant = "Uber", Category = "transport", Date = "2025-07-04T08:45:00Z", Balance = 3091.55m },

            // ---- User 102 ----
            new Transaction { Id = 5, UserId = 102, Type = "credit", Amount = 4100.00m, Currency = "USD", Description = "Monthly Salary", Merchant = "TechCo", Category = "salary", Date = "2025-07-01T09:00:00Z", Balance = 4100.00m },
            new Transaction { Id = 6, UserId = 102, Type = "debit", Amount = -1200.00m, Currency = "USD", Description = "Rent Payment", Merchant = "Landlord", Category = "bills", Date = "2025-07-02T10:00:00Z", Balance = 2900.00m },
            new Transaction { Id = 7, UserId = 102, Type = "debit", Amount = -55.00m, Currency = "USD", Description = "Dinner Out", Merchant = "Olive Garden", Category = "food", Date = "2025-07-05T20:15:00Z", Balance = 2845.00m },
            new Transaction { Id = 8, UserId = 102, Type = "debit", Amount = -12.99m, Currency = "USD", Description = "Streaming Subscription", Merchant = "Netflix", Category = "entertainment", Date = "2025-07-06T12:00:00Z", Balance = 2832.01m },

            // ---- User 103 ----
            new Transaction { Id = 9, UserId = 103, Type = "credit", Amount = 3800.00m, Currency = "USD", Description = "Monthly Salary", Merchant = "DevWorks", Category = "salary", Date = "2025-07-01T09:00:00Z", Balance = 3800.00m },
            new Transaction { Id = 10, UserId = 103, Type = "debit", Amount = -60.00m, Currency = "USD", Description = "Fuel", Merchant = "Shell", Category = "transport", Date = "2025-07-03T17:20:00Z", Balance = 3740.00m },
            new Transaction { Id = 11, UserId = 103, Type = "debit", Amount = -230.50m, Currency = "USD", Description = "New Headphones", Merchant = "Best Buy", Category = "shopping", Date = "2025-07-04T14:10:00Z", Balance = 3509.50m },
            new Transaction { Id = 12, UserId = 103, Type = "debit", Amount = -9.99m, Currency = "USD", Description = "Music Subscription", Merchant = "Spotify", Category = "entertainment", Date = "2025-07-05T11:30:00Z", Balance = 3499.51m },

            // ---- User 104 ----
            new Transaction { Id = 13, UserId = 104, Type = "credit", Amount = 3500.00m, Currency = "USD", Description = "Monthly Salary", Merchant = "DesignHub", Category = "salary", Date = "2025-07-01T09:00:00Z", Balance = 3500.00m },
            new Transaction { Id = 14, UserId = 104, Type = "debit", Amount = -45.00m, Currency = "USD", Description = "Lunch", Merchant = "Chipotle", Category = "food", Date = "2025-07-02T13:00:00Z", Balance = 3455.00m },
            new Transaction { Id = 15, UserId = 104, Type = "debit", Amount = -300.00m, Currency = "USD", Description = "Electricity Bill", Merchant = "PowerCo", Category = "bills", Date = "2025-07-03T09:45:00Z", Balance = 3155.00m },
            new Transaction { Id = 16, UserId = 104, Type = "debit", Amount = -75.00m, Currency = "USD", Description = "Concert Ticket", Merchant = "Ticketmaster", Category = "entertainment", Date = "2025-07-07T19:00:00Z", Balance = 3080.00m },

            // ---- User 105 ----
            new Transaction { Id = 17, UserId = 105, Type = "credit", Amount = 4300.00m, Currency = "USD", Description = "Monthly Salary", Merchant = "CloudNine", Category = "salary", Date = "2025-07-01T09:00:00Z", Balance = 4300.00m },
            new Transaction { Id = 18, UserId = 105, Type = "debit", Amount = -22.40m, Currency = "USD", Description = "Taxi", Merchant = "Lyft", Category = "transport", Date = "2025-07-02T22:30:00Z", Balance = 4277.60m },
            new Transaction { Id = 19, UserId = 105, Type = "debit", Amount = -150.00m, Currency = "USD", Description = "Transfer to Savings", Merchant = "Self", Category = "transfer", Date = "2025-07-03T10:00:00Z", Balance = 4127.60m },
            new Transaction { Id = 20, UserId = 105, Type = "debit", Amount = -33.00m, Currency = "USD", Description = "Books", Merchant = "Amazon", Category = "shopping", Date = "2025-07-05T16:20:00Z", Balance = 4094.60m },

            // ---- User 106 ----
            new Transaction { Id = 21, UserId = 106, Type = "credit", Amount = 3600.00m, Currency = "USD", Description = "Monthly Salary", Merchant = "QualityFirst", Category = "salary", Date = "2025-07-01T09:00:00Z", Balance = 3600.00m },
            new Transaction { Id = 22, UserId = 106, Type = "debit", Amount = -6.25m, Currency = "USD", Description = "Coffee", Merchant = "Starbucks", Category = "food", Date = "2025-07-02T08:00:00Z", Balance = 3593.75m },
            new Transaction { Id = 23, UserId = 106, Type = "debit", Amount = -900.00m, Currency = "USD", Description = "Rent", Merchant = "Landlord", Category = "bills", Date = "2025-07-03T10:00:00Z", Balance = 2693.75m },
            new Transaction { Id = 24, UserId = 106, Type = "debit", Amount = -40.00m, Currency = "USD", Description = "Movie Night", Merchant = "AMC", Category = "entertainment", Date = "2025-07-06T21:00:00Z", Balance = 2653.75m },

            // ---- User 107 ----
            new Transaction { Id = 25, UserId = 107, Type = "credit", Amount = 4500.00m, Currency = "USD", Description = "Monthly Salary", Merchant = "SkyServices", Category = "salary", Date = "2025-07-01T09:00:00Z", Balance = 4500.00m },
            new Transaction { Id = 26, UserId = 107, Type = "debit", Amount = -120.00m, Currency = "USD", Description = "Groceries", Merchant = "Costco", Category = "shopping", Date = "2025-07-02T15:30:00Z", Balance = 4380.00m },
            new Transaction { Id = 27, UserId = 107, Type = "debit", Amount = -50.00m, Currency = "USD", Description = "Gym Membership", Merchant = "FitLife", Category = "bills", Date = "2025-07-04T07:00:00Z", Balance = 4330.00m },
            new Transaction { Id = 28, UserId = 107, Type = "debit", Amount = -15.50m, Currency = "USD", Description = "Fast Food", Merchant = "McDonald's", Category = "food", Date = "2025-07-05T12:45:00Z", Balance = 4314.50m },

            // ---- User 108 ----
            new Transaction { Id = 29, UserId = 108, Type = "credit", Amount = 3900.00m, Currency = "USD", Description = "Monthly Salary", Merchant = "DataWorks", Category = "salary", Date = "2025-07-01T09:00:00Z", Balance = 3900.00m },
            new Transaction { Id = 30, UserId = 108, Type = "debit", Amount = -8.00m, Currency = "USD", Description = "Bus Pass", Merchant = "MetroTransit", Category = "transport", Date = "2025-07-02T07:30:00Z", Balance = 3892.00m },
            new Transaction { Id = 31, UserId = 108, Type = "debit", Amount = -200.00m, Currency = "USD", Description = "Clothes", Merchant = "Zara", Category = "shopping", Date = "2025-07-03T16:00:00Z", Balance = 3692.00m },
            new Transaction { Id = 32, UserId = 108, Type = "debit", Amount = -14.99m, Currency = "USD", Description = "Cloud Storage", Merchant = "Dropbox", Category = "bills", Date = "2025-07-05T09:00:00Z", Balance = 3677.01m },

            // ---- User 109 ----
            new Transaction { Id = 33, UserId = 109, Type = "credit", Amount = 4200.00m, Currency = "USD", Description = "Monthly Salary", Merchant = "SecureNet", Category = "salary", Date = "2025-07-01T09:00:00Z", Balance = 4200.00m },
            new Transaction { Id = 34, UserId = 109, Type = "debit", Amount = -60.00m, Currency = "USD", Description = "Dinner", Merchant = "Nobu", Category = "food", Date = "2025-07-03T20:00:00Z", Balance = 4140.00m },
            new Transaction { Id = 35, UserId = 109, Type = "debit", Amount = -500.00m, Currency = "USD", Description = "Transfer to Investment", Merchant = "Brokerage", Category = "transfer", Date = "2025-07-04T11:00:00Z", Balance = 3640.00m },
            new Transaction { Id = 36, UserId = 109, Type = "debit", Amount = -25.00m, Currency = "USD", Description = "Streaming", Merchant = "Disney+", Category = "entertainment", Date = "2025-07-06T18:30:00Z", Balance = 3615.00m },

            // ---- User 110 ----
            new Transaction { Id = 37, UserId = 110, Type = "credit", Amount = 3700.00m, Currency = "USD", Description = "Monthly Salary", Merchant = "BizAnalytics", Category = "salary", Date = "2025-07-01T09:00:00Z", Balance = 3700.00m },
            new Transaction { Id = 38, UserId = 110, Type = "debit", Amount = -35.00m, Currency = "USD", Description = "Taxi", Merchant = "Uber", Category = "transport", Date = "2025-07-02T09:20:00Z", Balance = 3665.00m },
            new Transaction { Id = 39, UserId = 110, Type = "debit", Amount = -110.00m, Currency = "USD", Description = "Groceries", Merchant = "Trader Joe's", Category = "shopping", Date = "2025-07-03T17:45:00Z", Balance = 3555.00m },
            new Transaction { Id = 40, UserId = 110, Type = "debit", Amount = -18.50m, Currency = "USD", Description = "Lunch", Merchant = "Panera", Category = "food", Date = "2025-07-05T13:15:00Z", Balance = 3536.50m }
        };
    }
}
