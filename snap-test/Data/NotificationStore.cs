using snap_test.Models;

namespace snap_test.Data
{
    /// <summary>
    /// In-memory seed store for notifications (20 records). Types: mention, like, follow, system, order_update.
    /// Mix of read/unread. UserId references existing Users (101-110).
    /// Writes mutate this static list and reset on app restart (non-persistent, per PRD).
    /// </summary>
    public static class NotificationStore
    {
        public static List<Notification> Notifications = new()
        {
            new Notification { Id = 1, UserId = 101, Type = "mention", Title = "You were mentioned in a comment", Body = "Liam mentioned you in 'API Design Patterns'", Read = false, Link = "/posts/3", CreatedAt = "2025-07-17T08:30:00Z" },
            new Notification { Id = 2, UserId = 101, Type = "like", Title = "Your post got a like", Body = "Emma liked your post 'Getting Started with REST APIs'", Read = true, Link = "/posts/1", CreatedAt = "2025-07-16T14:10:00Z" },
            new Notification { Id = 3, UserId = 102, Type = "follow", Title = "New follower", Body = "Michael started following you", Read = false, Link = "/users/101", CreatedAt = "2025-07-17T09:15:00Z" },
            new Notification { Id = 4, UserId = 102, Type = "system", Title = "Password changed", Body = "Your password was updated successfully", Read = true, Link = "/settings", CreatedAt = "2025-07-15T11:00:00Z" },
            new Notification { Id = 5, UserId = 103, Type = "order_update", Title = "Order shipped", Body = "Your order #2 has shipped", Read = false, Link = "/orders/2", CreatedAt = "2025-07-16T16:45:00Z" },
            new Notification { Id = 6, UserId = 103, Type = "mention", Title = "You were mentioned", Body = "Sophia mentioned you in a comment", Read = false, Link = "/posts/7", CreatedAt = "2025-07-17T10:05:00Z" },
            new Notification { Id = 7, UserId = 104, Type = "like", Title = "New like", Body = "James liked your comment", Read = true, Link = "/posts/6", CreatedAt = "2025-07-15T13:20:00Z" },
            new Notification { Id = 8, UserId = 104, Type = "follow", Title = "New follower", Body = "Ava started following you", Read = false, Link = "/users/108", CreatedAt = "2025-07-17T07:50:00Z" },
            new Notification { Id = 9, UserId = 105, Type = "order_update", Title = "Order delivered", Body = "Your order #11 was delivered", Read = true, Link = "/orders/11", CreatedAt = "2025-07-14T12:00:00Z" },
            new Notification { Id = 10, UserId = 105, Type = "system", Title = "Welcome to TestingAPIs", Body = "Thanks for joining! Explore the docs to get started.", Read = true, Link = "/docs", CreatedAt = "2025-07-10T09:00:00Z" },
            new Notification { Id = 11, UserId = 106, Type = "mention", Title = "Mentioned in a post", Body = "Noah mentioned you in 'Writing Effective Unit Tests'", Read = false, Link = "/posts/7", CreatedAt = "2025-07-17T11:30:00Z" },
            new Notification { Id = 12, UserId = 106, Type = "like", Title = "Post liked", Body = "Your post reached 50 likes", Read = false, Link = "/posts/8", CreatedAt = "2025-07-16T18:25:00Z" },
            new Notification { Id = 13, UserId = 107, Type = "follow", Title = "New follower", Body = "Benjamin started following you", Read = true, Link = "/users/109", CreatedAt = "2025-07-15T10:40:00Z" },
            new Notification { Id = 14, UserId = 107, Type = "order_update", Title = "Order processing", Body = "Your order #9 is being processed", Read = false, Link = "/orders/9", CreatedAt = "2025-07-17T08:00:00Z" },
            new Notification { Id = 15, UserId = 108, Type = "system", Title = "Security alert", Body = "New login from a new device", Read = false, Link = "/security", CreatedAt = "2025-07-17T06:15:00Z" },
            new Notification { Id = 16, UserId = 108, Type = "like", Title = "New like", Body = "Mia liked your photo", Read = true, Link = "/posts/10", CreatedAt = "2025-07-16T15:50:00Z" },
            new Notification { Id = 17, UserId = 109, Type = "mention", Title = "You were tagged", Body = "Olivia tagged you in a comment", Read = false, Link = "/posts/13", CreatedAt = "2025-07-17T12:10:00Z" },
            new Notification { Id = 18, UserId = 109, Type = "follow", Title = "New follower", Body = "Liam started following you", Read = false, Link = "/users/103", CreatedAt = "2025-07-16T09:35:00Z" },
            new Notification { Id = 19, UserId = 110, Type = "order_update", Title = "Order pending", Body = "Your order #10 is pending payment", Read = true, Link = "/orders/10", CreatedAt = "2025-07-16T13:05:00Z" },
            new Notification { Id = 20, UserId = 110, Type = "system", Title = "Profile updated", Body = "Your profile changes were saved", Read = false, Link = "/profile", CreatedAt = "2025-07-17T10:55:00Z" }
        };
    }
}
