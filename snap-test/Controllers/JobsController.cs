using Microsoft.AspNetCore.Mvc;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// Async job pattern (202 Accepted + polling). Status is computed from elapsed time, so no background workers:
    ///   queued (first 2s) -> running (progress 0-99%) -> completed | failed ("fail" jobs die at 60%) | cancelled.
    /// The store is in-memory, capped at 200 jobs (oldest non-seed jobs are evicted), and resets on restart.
    /// </summary>
    [ApiController]
    [Route("api/jobs")]
    public class JobsController : ControllerBase
    {
        private const int QueuedSeconds = 2;
        private const int MaxJobs = 200;
        private const double FailAt = 0.6;

        private static readonly string[] Types = { "report", "export", "import", "fail" };
        private static readonly object Gate = new();
        private static readonly List<Job> Jobs = SeedJobs();
        private static int _counter = 1000;

        /// <summary>Body for starting a job.</summary>
        public class JobRequest
        {
            /// <summary>Job type: <c>report</c>, <c>export</c>, <c>import</c> or <c>fail</c> (fails at 60%). Default <c>report</c>.</summary>
            public string? Type { get; set; }
            /// <summary>How long the job runs after leaving the queue, in seconds (1-60, default 10).</summary>
            public int? DurationSeconds { get; set; }
        }

        private class Job
        {
            public string Id { get; init; } = string.Empty;
            public string Type { get; init; } = string.Empty;
            public int DurationSeconds { get; init; }
            public DateTime CreatedAt { get; init; }
            public DateTime? CancelledAt { get; set; }
            public bool Seed { get; init; }
        }

        private static List<Job> SeedJobs() => new()
        {
            new Job { Id = "job_0001", Type = "report", DurationSeconds = 12, CreatedAt = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc), Seed = true },
            new Job { Id = "job_0002", Type = "export", DurationSeconds = 20, CreatedAt = new DateTime(2026, 7, 2, 14, 30, 0, DateTimeKind.Utc), Seed = true },
            new Job { Id = "job_0003", Type = "fail", DurationSeconds = 10, CreatedAt = new DateTime(2026, 7, 3, 11, 15, 0, DateTimeKind.Utc), Seed = true },
            new Job { Id = "job_0004", Type = "import", DurationSeconds = 30, CreatedAt = new DateTime(2026, 7, 4, 16, 45, 0, DateTimeKind.Utc), CancelledAt = new DateTime(2026, 7, 4, 16, 45, 9, DateTimeKind.Utc), Seed = true }
        };

        // -------------------- CREATE (202 Accepted) --------------------
        /// <summary>Start an asynchronous job.</summary>
        /// <param name="request">Job type and duration. An empty body starts a 10-second report job.</param>
        /// <remarks>
        /// Example body: <c>{"type":"report","durationSeconds":10}</c>
        /// <para>Returns 202 with a <c>Location</c> header pointing at the job and a <c>Retry-After</c> hint. Poll that URL;
        /// the job is queued for 2 seconds, then runs for <c>durationSeconds</c>. When it completes, fetch
        /// <c>resultUrl</c>.</para>
        /// </remarks>
        /// <response code="202">Job accepted; poll the Location URL.</response>
        /// <response code="400">Unknown type or duration out of range.</response>
        [HttpPost]
        public IActionResult Create([FromBody] JobRequest? request)
        {
            var type = (request?.Type ?? "report").Trim().ToLowerInvariant();
            if (!Types.Contains(type))
                return BadRequest(ApiResponse.Error(400, $"Unknown job type '{request?.Type}'. Supported: {string.Join(", ", Types)}."));

            var duration = request?.DurationSeconds ?? 10;
            if (duration < 1 || duration > 60)
                return BadRequest(ApiResponse.Error(400, "durationSeconds must be between 1 and 60."));

            var job = new Job
            {
                Id = $"job_{Interlocked.Increment(ref _counter)}",
                Type = type,
                DurationSeconds = duration,
                CreatedAt = DateTime.UtcNow
            };

            lock (Gate)
            {
                Jobs.Add(job);
                if (Jobs.Count > MaxJobs)
                {
                    var oldest = Jobs.FirstOrDefault(j => !j.Seed);
                    if (oldest != null) Jobs.Remove(oldest);
                }
            }

            Response.Headers.RetryAfter = QueuedSeconds.ToString();
            return Accepted($"/api/jobs/{job.Id}", new
            {
                message = "Job accepted. Poll the Location URL for status.",
                data = View(job, DateTime.UtcNow)
            });
        }

        // -------------------- LIST --------------------
        /// <summary>List all jobs, newest first.</summary>
        /// <param name="status">Optional filter: <c>queued</c>, <c>running</c>, <c>completed</c>, <c>failed</c> or
        /// <c>cancelled</c> (case-insensitive).</param>
        /// <response code="200">Array of jobs; <c>X-Total-Count</c> holds the count.</response>
        [HttpGet]
        public IActionResult GetAll([FromQuery] string? status = null)
        {
            var now = DateTime.UtcNow;
            List<Job> snapshot;
            lock (Gate) snapshot = Jobs.ToList();

            var views = snapshot
                .OrderByDescending(j => j.CreatedAt)
                .Select(j => (job: j, state: State(j, now)))
                .Where(x => string.IsNullOrWhiteSpace(status) || string.Equals(x.state.status, status, StringComparison.OrdinalIgnoreCase))
                .Select(x => View(x.job, now))
                .ToList();

            Response.Headers["X-Total-Count"] = views.Count.ToString();
            return Ok(views);
        }

        // -------------------- STATUS --------------------
        /// <summary>Get a job's current status and progress.</summary>
        /// <param name="id">Job id, e.g. <c>job_0001</c> or the id returned by POST.</param>
        /// <remarks>While the job is queued or running the response includes <c>Retry-After: 1</c>.</remarks>
        /// <response code="200">The job.</response>
        /// <response code="404">No job with that id.</response>
        [HttpGet("{id}")]
        public IActionResult GetById(string id)
        {
            var job = Find(id);
            if (job == null) return NotFoundError(id);

            var now = DateTime.UtcNow;
            var (status, _) = State(job, now);
            if (status is "queued" or "running") Response.Headers.RetryAfter = "1";

            return Ok(View(job, now));
        }

        // -------------------- RESULT --------------------
        /// <summary>Get the result of a completed job.</summary>
        /// <param name="id">Job id.</param>
        /// <response code="200">The job's result (shape depends on the job type).</response>
        /// <response code="404">No job with that id.</response>
        /// <response code="409">The job is still queued or running, or it failed or was cancelled.</response>
        [HttpGet("{id}/result")]
        public IActionResult GetResult(string id)
        {
            var job = Find(id);
            if (job == null) return NotFoundError(id);

            var (status, progress) = State(job, DateTime.UtcNow);
            switch (status)
            {
                case "queued":
                case "running":
                    Response.Headers.RetryAfter = "1";
                    return Conflict(ApiResponse.Error(409, $"Job is still {status} ({progress}%). Poll /api/jobs/{id} and retry."));
                case "failed":
                    return Conflict(ApiResponse.Error(409, "Job failed, so no result is available. See the job's 'error' field."));
                case "cancelled":
                    return Conflict(ApiResponse.Error(409, "Job was cancelled, so no result is available."));
            }

            return Ok(new { jobId = job.Id, type = job.Type, result = Result(job) });
        }

        // -------------------- CANCEL --------------------
        /// <summary>Cancel a queued or running job.</summary>
        /// <param name="id">Job id.</param>
        /// <response code="200">Job cancelled.</response>
        /// <response code="404">No job with that id.</response>
        /// <response code="409">The job has already finished.</response>
        [HttpDelete("{id}")]
        public IActionResult Cancel(string id)
        {
            var now = DateTime.UtcNow;
            lock (Gate)
            {
                var job = Jobs.FirstOrDefault(j => j.Id == id);
                if (job == null) return NotFoundError(id);

                var (status, _) = State(job, now);
                if (status is not ("queued" or "running"))
                    return Conflict(ApiResponse.Error(409, $"Job is already {status} and cannot be cancelled."));

                job.CancelledAt = now;
                return Ok(new { message = "Job cancelled", data = View(job, now) });
            }
        }

        private static Job? Find(string id)
        {
            lock (Gate) return Jobs.FirstOrDefault(j => j.Id == id);
        }

        private IActionResult NotFoundError(string id) =>
            NotFound(ApiResponse.Error(404, $"Job '{id}' does not exist."));

        private static (string status, int progress) State(Job job, DateTime now)
        {
            var at = job.CancelledAt ?? now;
            var running = (at - job.CreatedAt).TotalSeconds - QueuedSeconds;
            var progress = running <= 0 ? 0 : (int)Math.Min(99, running / job.DurationSeconds * 100);

            if (job.CancelledAt != null) return ("cancelled", progress);
            if (running < 0) return ("queued", 0);
            if (job.Type == "fail" && running >= job.DurationSeconds * FailAt) return ("failed", (int)(FailAt * 100));
            if (running < job.DurationSeconds) return ("running", progress);
            return ("completed", 100);
        }

        private static object View(Job job, DateTime now)
        {
            var (status, progress) = State(job, now);
            var startedAt = job.CreatedAt.AddSeconds(QueuedSeconds);

            DateTime? finishedAt = status switch
            {
                "completed" => startedAt.AddSeconds(job.DurationSeconds),
                "failed" => startedAt.AddSeconds(job.DurationSeconds * FailAt),
                "cancelled" => job.CancelledAt,
                _ => null
            };

            return new
            {
                id = job.Id,
                type = job.Type,
                status,
                progress,
                durationSeconds = job.DurationSeconds,
                createdAt = Iso(job.CreatedAt),
                startedAt = status == "queued" || (status == "cancelled" && job.CancelledAt < startedAt) ? null : Iso(startedAt),
                finishedAt = finishedAt.HasValue ? Iso(finishedAt.Value) : null,
                statusUrl = $"/api/jobs/{job.Id}",
                resultUrl = status == "completed" ? $"/api/jobs/{job.Id}/result" : null,
                error = status == "failed" ? "Simulated failure: worker crashed while processing batch 3 of 5." : null
            };
        }

        private static object Result(Job job) => job.Type switch
        {
            "report" => new
            {
                reportName = "Monthly Sales Summary",
                period = "2026-08",
                rows = new[]
                {
                    new { region = "North America", revenue = 125430.50m, orders = 1421 },
                    new { region = "Europe", revenue = 98210.75m, orders = 1187 },
                    new { region = "Asia Pacific", revenue = 76455.20m, orders = 964 },
                    new { region = "Latin America", revenue = 23890.00m, orders = 312 }
                },
                totals = new { revenue = 323986.45m, orders = 3884 }
            },
            "export" => new
            {
                format = "csv",
                fileName = $"export_{job.Id}.csv",
                rowCount = 1250,
                sizeBytes = 48213,
                downloadUrl = $"https://files.example.com/exports/{job.Id}.csv",
                expiresAt = Iso(job.CreatedAt.AddDays(7))
            },
            _ => (object)new
            {
                processed = 250,
                imported = 243,
                skipped = 5,
                failed = 2,
                errors = new[]
                {
                    new { row = 17, message = "Invalid email address 'jane.doe@'" },
                    new { row = 132, message = "Duplicate SKU 'SKU-0042'" }
                }
            }
        };

        private static string Iso(DateTime value) => value.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
}
