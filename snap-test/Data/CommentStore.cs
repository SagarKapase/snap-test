using snap_test.Models;

namespace snap_test.Data
{
    /// <summary>
    /// In-memory seed store for comments (50 records) distributed across the 15 posts.
    /// Linked by PostId; UserId references existing Users (101-110).
    /// Writes mutate this static list and reset on app restart (non-persistent, per PRD).
    /// </summary>
    public static class CommentStore
    {
        public static List<Comment> Comments = new()
        {
            // Post 1
            new Comment { Id = 1, PostId = 1, UserId = 103, Body = "Great writeup, exactly what I needed.", CreatedAt = "2025-07-01T15:23:00Z" },
            new Comment { Id = 2, PostId = 1, UserId = 105, Body = "Could you cover authentication in a follow-up?", CreatedAt = "2025-07-01T16:10:00Z" },
            new Comment { Id = 3, PostId = 1, UserId = 108, Body = "The section on status codes was super helpful.", CreatedAt = "2025-07-01T18:05:00Z" },
            new Comment { Id = 4, PostId = 1, UserId = 102, Body = "Bookmarked. Sharing this with my team.", CreatedAt = "2025-07-02T09:12:00Z" },
            // Post 2
            new Comment { Id = 5, PostId = 2, UserId = 101, Body = "Finally understand ConfigureAwait after reading this.", CreatedAt = "2025-07-02T10:40:00Z" },
            new Comment { Id = 6, PostId = 2, UserId = 106, Body = "Nice examples. Async all the way, right?", CreatedAt = "2025-07-02T12:15:00Z" },
            new Comment { Id = 7, PostId = 2, UserId = 109, Body = "Would love a deep dive on Task vs ValueTask.", CreatedAt = "2025-07-02T14:30:00Z" },
            new Comment { Id = 8, PostId = 2, UserId = 104, Body = "This cleared up a bug I had for days.", CreatedAt = "2025-07-03T08:20:00Z" },
            // Post 3
            new Comment { Id = 9, PostId = 3, UserId = 102, Body = "Perfect intro for someone new to containers.", CreatedAt = "2025-07-03T12:00:00Z" },
            new Comment { Id = 10, PostId = 3, UserId = 107, Body = "The volume mounting part saved me.", CreatedAt = "2025-07-03T13:45:00Z" },
            new Comment { Id = 11, PostId = 3, UserId = 110, Body = "Can you explain multi-stage builds next?", CreatedAt = "2025-07-03T15:30:00Z" },
            new Comment { Id = 12, PostId = 3, UserId = 101, Body = "Clear and concise, thanks!", CreatedAt = "2025-07-04T09:05:00Z" },
            new Comment { Id = 13, PostId = 3, UserId = 105, Body = "Docker Compose deserves its own post.", CreatedAt = "2025-07-04T11:20:00Z" },
            // Post 4
            new Comment { Id = 14, PostId = 4, UserId = 103, Body = "git reflog is a lifesaver, glad you included it.", CreatedAt = "2025-07-04T17:30:00Z" },
            new Comment { Id = 15, PostId = 4, UserId = 108, Body = "TIL about git bisect. Mind blown.", CreatedAt = "2025-07-04T18:50:00Z" },
            new Comment { Id = 16, PostId = 4, UserId = 106, Body = "Interactive rebase is scary but powerful.", CreatedAt = "2025-07-05T08:15:00Z" },
            new Comment { Id = 17, PostId = 4, UserId = 109, Body = "Solid list. Add git stash maybe?", CreatedAt = "2025-07-05T10:40:00Z" },
            new Comment { Id = 18, PostId = 4, UserId = 104, Body = "Sharing with all the juniors on my team.", CreatedAt = "2025-07-05T12:00:00Z" },
            // Post 5
            new Comment { Id = 19, PostId = 5, UserId = 102, Body = "Service boundaries are the hardest part imo.", CreatedAt = "2025-07-05T11:10:00Z" },
            new Comment { Id = 20, PostId = 5, UserId = 107, Body = "Would like more on inter-service communication.", CreatedAt = "2025-07-05T13:25:00Z" },
            new Comment { Id = 21, PostId = 5, UserId = 110, Body = "Great overview of the trade-offs.", CreatedAt = "2025-07-06T09:00:00Z" },
            // Post 6
            new Comment { Id = 22, PostId = 6, UserId = 105, Body = "Grid finally clicked for me. Thank you!", CreatedAt = "2025-07-06T14:35:00Z" },
            new Comment { Id = 23, PostId = 6, UserId = 108, Body = "The auto-fit vs auto-fill bit was gold.", CreatedAt = "2025-07-06T16:20:00Z" },
            new Comment { Id = 24, PostId = 6, UserId = 101, Body = "Flexbox or Grid — still confused sometimes.", CreatedAt = "2025-07-07T08:45:00Z" },
            // Post 7
            new Comment { Id = 25, PostId = 7, UserId = 103, Body = "Arrange-Act-Assert changed how I test.", CreatedAt = "2025-07-07T09:55:00Z" },
            new Comment { Id = 26, PostId = 7, UserId = 109, Body = "Mocking best practices please!", CreatedAt = "2025-07-07T11:30:00Z" },
            new Comment { Id = 27, PostId = 7, UserId = 102, Body = "Good reminder to test behavior, not implementation.", CreatedAt = "2025-07-07T14:10:00Z" },
            // Post 8
            new Comment { Id = 28, PostId = 8, UserId = 104, Body = "k8s is a beast, this helped a lot.", CreatedAt = "2025-07-08T16:00:00Z" },
            new Comment { Id = 29, PostId = 8, UserId = 106, Body = "Ingress controllers confuse me still.", CreatedAt = "2025-07-08T17:45:00Z" },
            new Comment { Id = 30, PostId = 8, UserId = 110, Body = "Pods vs deployments explained well.", CreatedAt = "2025-07-09T08:30:00Z" },
            new Comment { Id = 31, PostId = 8, UserId = 101, Body = "Waiting for the Helm follow-up!", CreatedAt = "2025-07-09T10:15:00Z" },
            // Post 9
            new Comment { Id = 32, PostId = 9, UserId = 105, Body = "Dependency inversion is key, well explained.", CreatedAt = "2025-07-09T13:00:00Z" },
            new Comment { Id = 33, PostId = 9, UserId = 108, Body = "SOLID never gets old.", CreatedAt = "2025-07-09T15:20:00Z" },
            new Comment { Id = 34, PostId = 9, UserId = 107, Body = "Where do you put DTOs though?", CreatedAt = "2025-07-10T09:40:00Z" },
            // Post 10
            new Comment { Id = 35, PostId = 10, UserId = 102, Body = "JSON.parse pitfalls are real.", CreatedAt = "2025-07-10T10:50:00Z" },
            new Comment { Id = 36, PostId = 10, UserId = 109, Body = "Good beginner content.", CreatedAt = "2025-07-10T12:30:00Z" },
            // Post 11
            new Comment { Id = 37, PostId = 11, UserId = 103, Body = "Composite indexes tripped me up before.", CreatedAt = "2025-07-11T15:40:00Z" },
            new Comment { Id = 38, PostId = 11, UserId = 106, Body = "Explain query plans next please.", CreatedAt = "2025-07-11T17:05:00Z" },
            new Comment { Id = 39, PostId = 11, UserId = 101, Body = "This doubled my query speed. Thanks!", CreatedAt = "2025-07-12T09:10:00Z" },
            // Post 12
            new Comment { Id = 40, PostId = 12, UserId = 104, Body = "Refresh tokens confused me, now clearer.", CreatedAt = "2025-07-12T12:25:00Z" },
            new Comment { Id = 41, PostId = 12, UserId = 110, Body = "Never store JWT in localStorage, folks.", CreatedAt = "2025-07-12T14:50:00Z" },
            new Comment { Id = 42, PostId = 12, UserId = 105, Body = "Great security walkthrough.", CreatedAt = "2025-07-13T08:35:00Z" },
            // Post 13
            new Comment { Id = 43, PostId = 13, UserId = 108, Body = "OWASP Top 10 should be required reading.", CreatedAt = "2025-07-13T17:00:00Z" },
            new Comment { Id = 44, PostId = 13, UserId = 102, Body = "CSRF vs XSS finally makes sense.", CreatedAt = "2025-07-13T18:30:00Z" },
            new Comment { Id = 45, PostId = 13, UserId = 107, Body = "Adding a CSP header today.", CreatedAt = "2025-07-14T09:20:00Z" },
            new Comment { Id = 46, PostId = 13, UserId = 101, Body = "Security is everyone's job. Great post.", CreatedAt = "2025-07-14T11:00:00Z" },
            // Post 14
            new Comment { Id = 47, PostId = 14, UserId = 106, Body = "Depends on the project, honestly.", CreatedAt = "2025-07-14T12:15:00Z" },
            new Comment { Id = 48, PostId = 14, UserId = 109, Body = "Hybrid approaches work best for us.", CreatedAt = "2025-07-14T14:40:00Z" },
            // Post 15
            new Comment { Id = 49, PostId = 15, UserId = 105, Body = "Mobile-first is the way.", CreatedAt = "2025-07-15T14:30:00Z" },
            new Comment { Id = 50, PostId = 15, UserId = 103, Body = "Container queries are a game changer.", CreatedAt = "2025-07-15T16:10:00Z" }
        };
    }
}
