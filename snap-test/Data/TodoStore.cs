using snap_test.Models;

namespace snap_test.Data
{
    /// <summary>
    /// In-memory seed store for todos (30 records). Mix of completed/pending across users and priorities.
    /// UserId references existing Users (101-110). Priorities: high, medium, low.
    /// Writes mutate this static list and reset on app restart (non-persistent, per PRD).
    /// </summary>
    public static class TodoStore
    {
        public static List<Todo> Todos = new()
        {
            new Todo { Id = 1, UserId = 101, Title = "Review pull request #42", Completed = false, Priority = "high", DueDate = "2025-07-20" },
            new Todo { Id = 2, UserId = 101, Title = "Update API documentation", Completed = false, Priority = "medium", DueDate = "2025-07-22" },
            new Todo { Id = 3, UserId = 102, Title = "Prepare sprint demo", Completed = true, Priority = "high", DueDate = "2025-07-15" },
            new Todo { Id = 4, UserId = 102, Title = "Refactor authentication module", Completed = false, Priority = "medium", DueDate = "2025-07-25" },
            new Todo { Id = 5, UserId = 103, Title = "Fix production deployment script", Completed = false, Priority = "high", DueDate = "2025-07-19" },
            new Todo { Id = 6, UserId = 103, Title = "Set up CI/CD pipeline", Completed = true, Priority = "medium", DueDate = "2025-07-12" },
            new Todo { Id = 7, UserId = 104, Title = "Design new dashboard mockups", Completed = false, Priority = "medium", DueDate = "2025-07-24" },
            new Todo { Id = 8, UserId = 104, Title = "Conduct usability testing", Completed = false, Priority = "low", DueDate = "2025-07-28" },
            new Todo { Id = 9, UserId = 105, Title = "Optimize database queries", Completed = true, Priority = "high", DueDate = "2025-07-14" },
            new Todo { Id = 10, UserId = 105, Title = "Write integration tests", Completed = false, Priority = "medium", DueDate = "2025-07-26" },
            new Todo { Id = 11, UserId = 106, Title = "Triage bug backlog", Completed = false, Priority = "high", DueDate = "2025-07-18" },
            new Todo { Id = 12, UserId = 106, Title = "Automate regression suite", Completed = false, Priority = "low", DueDate = "2025-07-30" },
            new Todo { Id = 13, UserId = 107, Title = "Migrate services to Kubernetes", Completed = false, Priority = "high", DueDate = "2025-07-27" },
            new Todo { Id = 14, UserId = 107, Title = "Review cloud cost report", Completed = true, Priority = "low", DueDate = "2025-07-10" },
            new Todo { Id = 15, UserId = 108, Title = "Build analytics report", Completed = false, Priority = "medium", DueDate = "2025-07-23" },
            new Todo { Id = 16, UserId = 108, Title = "Clean up data warehouse", Completed = true, Priority = "low", DueDate = "2025-07-11" },
            new Todo { Id = 17, UserId = 109, Title = "Run security audit", Completed = false, Priority = "high", DueDate = "2025-07-21" },
            new Todo { Id = 18, UserId = 109, Title = "Patch vulnerable dependencies", Completed = true, Priority = "high", DueDate = "2025-07-13" },
            new Todo { Id = 19, UserId = 110, Title = "Draft quarterly roadmap", Completed = false, Priority = "medium", DueDate = "2025-07-29" },
            new Todo { Id = 20, UserId = 110, Title = "Interview product candidates", Completed = false, Priority = "low", DueDate = "2025-07-31" },
            new Todo { Id = 21, UserId = 101, Title = "Merge feature branch", Completed = true, Priority = "high", DueDate = "2025-07-09" },
            new Todo { Id = 22, UserId = 102, Title = "Update onboarding guide", Completed = true, Priority = "low", DueDate = "2025-07-08" },
            new Todo { Id = 23, UserId = 103, Title = "Rotate access keys", Completed = false, Priority = "high", DueDate = "2025-07-20" },
            new Todo { Id = 24, UserId = 104, Title = "Create design system tokens", Completed = false, Priority = "medium", DueDate = "2025-07-25" },
            new Todo { Id = 25, UserId = 105, Title = "Profile API latency", Completed = false, Priority = "medium", DueDate = "2025-07-22" },
            new Todo { Id = 26, UserId = 106, Title = "Write test plan for release", Completed = true, Priority = "medium", DueDate = "2025-07-16" },
            new Todo { Id = 27, UserId = 107, Title = "Configure monitoring alerts", Completed = false, Priority = "medium", DueDate = "2025-07-24" },
            new Todo { Id = 28, UserId = 108, Title = "Validate ETL pipeline", Completed = true, Priority = "high", DueDate = "2025-07-12" },
            new Todo { Id = 29, UserId = 109, Title = "Enable two-factor auth", Completed = false, Priority = "high", DueDate = "2025-07-19" },
            new Todo { Id = 30, UserId = 110, Title = "Schedule stakeholder review", Completed = true, Priority = "low", DueDate = "2025-07-07" }
        };
    }
}
