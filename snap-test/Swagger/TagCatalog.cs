using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace snap_test.Swagger
{
    /// <summary>
    /// Orders the Swagger sidebar by category (HTTP basics, auth, patterns, resources...) instead of A–Z,
    /// and prefixes each tag description with its category. Tags are controller names.
    /// </summary>
    public class TagCatalog : IDocumentFilter
    {
        private static readonly (string Category, string[] Controllers)[] Categories =
        {
            ("Start here", new[] { "Catalog" }),
            ("HTTP basics", new[] { "Echo", "Methods", "Status", "Redirect", "Cookies", "Caching", "Bodies" }),
            ("Formats & data", new[] { "Formats", "Files", "EdgeCases", "Utils" }),
            ("Auth & security", new[] { "AuthSchemes", "OAuth", "JwtAuth", "RateLimit", "Payments" }),
            ("API patterns", new[] { "Streaming", "WebSocket", "Jobs", "Pagination", "Chaos", "Webhooks", "Versioning", "Soap", "Validation" }),
            ("Resources", new[] { "Products", "Posts", "Comments", "Todos", "Carts", "Orders", "Quotes", "Recipes", "Notifications", "Transactions",
                                  "Employees", "Companies", "People", "Books", "Movies", "Countries", "Events", "Albums", "Photos" }),
            ("Original snap-test", new[] { "User", "Admin", "AuthTest", "UserXML", "FormData", "Proxy", "WeatherForecast" }),
        };

        private static readonly Dictionary<string, (int Order, string Category)> Index = Categories
            .SelectMany((c, ci) => c.Controllers.Select((name, i) => (name, order: ci * 100 + i, c.Category)))
            .ToDictionary(x => x.name, x => (x.order, x.Category), StringComparer.OrdinalIgnoreCase);

        /// <summary>Sort key used by OrderActionsBy so operations (and thus tags) appear in category order.</summary>
        public static string SortKey(string? controller) =>
            controller != null && Index.TryGetValue(controller, out var e) ? e.Order.ToString("D4") : "9999" + controller;

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // Every tag used by an operation, plus any the XML comments already described.
            var described = (swaggerDoc.Tags ?? new HashSet<OpenApiTag>())
                .Where(t => t.Name != null)
                .ToDictionary(t => t.Name!, t => t.Description, StringComparer.OrdinalIgnoreCase);

            var used = swaggerDoc.Paths.Values
                .SelectMany(p => p.Operations?.Values ?? Enumerable.Empty<OpenApiOperation>())
                .SelectMany(o => o.Tags ?? new HashSet<OpenApiTagReference>())
                .Select(t => t.Name)
                .Where(n => n != null)
                .Select(n => n!)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            var ordered = used
                .OrderBy(SortKey, StringComparer.Ordinal)
                .Select(name =>
                {
                    var category = Index.TryGetValue(name, out var e) ? e.Category : "Other";
                    described.TryGetValue(name, out var description);
                    return new OpenApiTag
                    {
                        Name = name,
                        Description = string.IsNullOrWhiteSpace(description) ? $"**{category}**" : $"**{category}** · {description.Trim()}"
                    };
                })
                .ToList();

            swaggerDoc.Tags = new HashSet<OpenApiTag>(ordered);

            DeduplicateOperationIds(swaggerDoc);
        }

        // One action can serve several verbs (GET + HEAD, AcceptVerbs) or routes (catch-all captures), which would
        // repeat its operationId. Suffix the verb when needed, then a counter as a last resort.
        private static void DeduplicateOperationIds(OpenApiDocument doc)
        {
            var operations = doc.Paths
                .SelectMany(p => (p.Value.Operations ?? new Dictionary<HttpMethod, OpenApiOperation>())
                    .Select(o => (Method: o.Key.Method, Operation: o.Value)))
                .Where(x => !string.IsNullOrEmpty(x.Operation.OperationId))
                .ToList();

            foreach (var group in operations.GroupBy(x => x.Operation.OperationId).Where(g => g.Count() > 1))
            {
                foreach (var (method, operation) in group)
                    operation.OperationId += "_" + method.ToUpperInvariant();
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (_, operation) in operations)
            {
                var id = operation.OperationId!;
                for (var n = 2; !seen.Add(id); n++)
                    id = $"{operation.OperationId}{n}";
                operation.OperationId = id;
            }
        }
    }
}
