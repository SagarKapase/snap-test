using snap_test.Models;

namespace snap_test.Data
{
    /// <summary>
    /// In-memory seed store for posts (15 records). userId references existing Users (101-110).
    /// Writes mutate this static list and reset on app restart (non-persistent, per PRD).
    /// </summary>
    public static class PostStore
    {
        public static List<Post> Posts = new()
        {
            new Post { Id = 1, UserId = 101, Title = "Getting Started with REST APIs", Body = "REST APIs are the backbone of modern web applications. In this post, we'll cover the basics of HTTP methods, status codes, and request/response patterns.", Tags = new() { "api", "tutorial", "beginner" }, PublishedAt = "2025-07-01T14:00:00Z", Likes = 42 },
            new Post { Id = 2, UserId = 102, Title = "Understanding Async/Await in C#", Body = "Asynchronous programming can be confusing at first. This guide breaks down how async and await work under the hood and when to use ConfigureAwait.", Tags = new() { "csharp", "dotnet", "async" }, PublishedAt = "2025-07-02T09:30:00Z", Likes = 88 },
            new Post { Id = 3, UserId = 103, Title = "Docker for Beginners: Containers Explained", Body = "Containers changed how we ship software. Learn what images, containers, and volumes are, and how to run your first Dockerfile.", Tags = new() { "docker", "devops", "containers" }, PublishedAt = "2025-07-03T11:15:00Z", Likes = 156 },
            new Post { Id = 4, UserId = 101, Title = "10 Git Commands Every Developer Should Know", Body = "Beyond commit and push, these ten Git commands will make you faster and more confident, from reflog to interactive rebase.", Tags = new() { "git", "productivity", "tools" }, PublishedAt = "2025-07-04T16:45:00Z", Likes = 203 },
            new Post { Id = 5, UserId = 105, Title = "Building Scalable Microservices", Body = "Splitting a monolith is only the start. We explore service boundaries, data ownership, and the trade-offs of going distributed.", Tags = new() { "architecture", "microservices", "backend" }, PublishedAt = "2025-07-05T10:00:00Z", Likes = 74 },
            new Post { Id = 6, UserId = 104, Title = "A Practical Guide to CSS Grid", Body = "CSS Grid makes complex layouts simple once it clicks. This walkthrough covers tracks, areas, and the auto-fit vs auto-fill trick.", Tags = new() { "css", "frontend", "layout" }, PublishedAt = "2025-07-06T13:20:00Z", Likes = 61 },
            new Post { Id = 7, UserId = 106, Title = "Writing Effective Unit Tests", Body = "Good tests give you confidence to refactor. Learn the Arrange-Act-Assert pattern and how to test behavior instead of implementation.", Tags = new() { "testing", "quality", "tdd" }, PublishedAt = "2025-07-07T08:40:00Z", Likes = 49 },
            new Post { Id = 8, UserId = 107, Title = "Introduction to Kubernetes", Body = "Kubernetes orchestrates containers at scale. We demystify pods, deployments, services, and ingress with practical examples.", Tags = new() { "kubernetes", "devops", "cloud" }, PublishedAt = "2025-07-08T15:10:00Z", Likes = 132 },
            new Post { Id = 9, UserId = 103, Title = "Clean Architecture Principles", Body = "Dependencies should point inward. This post explains layers, the dependency rule, and where DTOs and use cases belong.", Tags = new() { "architecture", "design", "solid" }, PublishedAt = "2025-07-09T12:00:00Z", Likes = 97 },
            new Post { Id = 10, UserId = 108, Title = "Working with JSON in JavaScript", Body = "JSON is everywhere in web development. Learn to parse, stringify, and avoid common pitfalls when handling API responses.", Tags = new() { "javascript", "json", "web" }, PublishedAt = "2025-07-10T09:25:00Z", Likes = 38 },
            new Post { Id = 11, UserId = 102, Title = "Database Indexing Explained", Body = "Indexes can make queries lightning fast or slow writes to a crawl. Understand B-trees, composite indexes, and query plans.", Tags = new() { "database", "sql", "performance" }, PublishedAt = "2025-07-11T14:50:00Z", Likes = 115 },
            new Post { Id = 12, UserId = 105, Title = "OAuth 2.0 and JWT Authentication", Body = "Token-based auth is the standard for APIs. We cover the OAuth flows, access vs refresh tokens, and how to validate a JWT.", Tags = new() { "security", "auth", "api" }, PublishedAt = "2025-07-12T11:35:00Z", Likes = 141 },
            new Post { Id = 13, UserId = 109, Title = "Securing Your Web Applications", Body = "Security is everyone's job. A tour of the OWASP Top 10 including XSS, CSRF, and why you should ship a Content-Security-Policy.", Tags = new() { "security", "web", "owasp" }, PublishedAt = "2025-07-13T16:05:00Z", Likes = 168 },
            new Post { Id = 14, UserId = 110, Title = "Agile vs Waterfall: Which to Choose", Body = "Neither methodology is a silver bullet. We compare planning, feedback loops, and where hybrid approaches shine.", Tags = new() { "agile", "management", "process" }, PublishedAt = "2025-07-14T10:15:00Z", Likes = 52 },
            new Post { Id = 15, UserId = 104, Title = "Responsive Design Best Practices", Body = "Users browse on every screen size. Learn mobile-first workflows, fluid typography, and the power of container queries.", Tags = new() { "css", "responsive", "frontend" }, PublishedAt = "2025-07-15T13:45:00Z", Likes = 79 }
        };
    }
}
