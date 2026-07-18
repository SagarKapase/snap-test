using System.Reflection;

namespace snap_test.Helpers
{
    /// <summary>
    /// Reusable, reflection-based query pipeline for all list endpoints.
    /// Applies field filters -> search (?q=) -> sort (?sort=/?order=) -> pagination (?limit/?page/?offset).
    /// Returns the paged items plus values for the X-Total-Count / X-Page / X-Per-Page / X-Total-Pages headers.
    /// </summary>
    public static class QueryHelper
    {
        // Query keys handled by the pipeline itself (or by SimulationMiddleware) — never treated as field filters.
        private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
        {
            "limit", "page", "offset", "sort", "order", "q", "delay", "error"
        };

        // Text fields scanned by ?q= full-text search.
        private static readonly string[] SearchFields = { "title", "name", "body", "description", "text" };

        public static (List<T> items, int total, int page, int perPage, int totalPages)
            Apply<T>(IEnumerable<T> source, IQueryCollection query)
        {
            IEnumerable<T> items = source;
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // 1. Field-specific filtering: any query key matching a property name -> exact (case-insensitive) match.
            foreach (var kv in query)
            {
                if (Reserved.Contains(kv.Key)) continue;

                var prop = props.FirstOrDefault(p => string.Equals(p.Name, kv.Key, StringComparison.OrdinalIgnoreCase));
                if (prop == null) continue;

                var value = kv.Value.ToString();
                items = items.Where(x =>
                    string.Equals(prop.GetValue(x)?.ToString(), value, StringComparison.OrdinalIgnoreCase));
            }

            // 2. Full-text search across common text fields.
            var q = query["q"].ToString();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var searchProps = props
                    .Where(p => p.PropertyType == typeof(string) &&
                                SearchFields.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                items = items.Where(x => searchProps.Any(p =>
                    (p.GetValue(x) as string)?.Contains(q, StringComparison.OrdinalIgnoreCase) == true));
            }

            // 3. Sort by a named field, asc (default) or desc.
            var sort = query["sort"].ToString();
            if (!string.IsNullOrWhiteSpace(sort))
            {
                var prop = props.FirstOrDefault(p => string.Equals(p.Name, sort, StringComparison.OrdinalIgnoreCase));
                if (prop != null)
                {
                    var order = query["order"].ToString();
                    items = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase)
                        ? items.OrderByDescending(x => prop.GetValue(x))
                        : items.OrderBy(x => prop.GetValue(x));
                }
            }

            var list = items.ToList();
            var total = list.Count;

            // 4. Pagination. limit is optional (default: all), clamped to max 100.
            int? limit = null;
            if (int.TryParse(query["limit"], out var l))
                limit = Math.Clamp(l, 1, 100);

            var page = 1;
            var skip = 0;

            if (limit.HasValue)
            {
                if (int.TryParse(query["page"], out var p) && p > 0)
                {
                    page = p;
                    skip = (p - 1) * limit.Value;
                }
                else if (int.TryParse(query["offset"], out var off) && off > 0)
                {
                    skip = off;
                    page = off / limit.Value + 1;
                }
            }
            else if (int.TryParse(query["offset"], out var offNoLimit) && offNoLimit > 0)
            {
                skip = offNoLimit;
            }

            var perPage = limit ?? total;
            var totalPages = limit.HasValue ? (int)Math.Ceiling((double)total / limit.Value) : 1;

            IEnumerable<T> paged = list.Skip(skip);
            if (limit.HasValue) paged = paged.Take(limit.Value);

            return (paged.ToList(), total, page, perPage, totalPages);
        }
    }
}
