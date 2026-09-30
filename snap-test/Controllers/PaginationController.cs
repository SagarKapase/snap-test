using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// The same 100-item dataset exposed through six common pagination styles, so clients can test each one:
    /// offset/limit, Spring page envelope, Stripe cursor, GitHub Link header, keyset (after_id) and HAL (_links).
    /// </summary>
    [ApiController]
    [Route("api/pagination")]
    public class PaginationController : ControllerBase
    {
        /// <summary>An item in the 100-item pagination dataset.</summary>
        public class PageItem
        {
            /// <summary>Item id, 1-100.</summary>
            public int Id { get; set; }
            /// <summary>Display name.</summary>
            public string Name { get; set; } = string.Empty;
            /// <summary>Category.</summary>
            public string Category { get; set; } = string.Empty;
            /// <summary>Price.</summary>
            public decimal Price { get; set; }
            /// <summary>Creation time (ISO 8601, UTC).</summary>
            public string CreatedAt { get; set; } = string.Empty;
        }

        private static readonly string[] Adjectives = { "Classic", "Smart", "Compact", "Deluxe", "Eco", "Ultra", "Vintage", "Portable", "Rugged", "Premium" };
        private static readonly string[] Nouns = { "Lamp", "Kettle", "Backpack", "Speaker", "Notebook", "Chair", "Bottle", "Headset", "Planter", "Clock", "Blender" };
        private static readonly string[] Categories = { "home", "electronics", "outdoors", "office", "kitchen" };

        private static readonly List<PageItem> Items = Enumerable.Range(1, 100).Select(i => new PageItem
        {
            Id = i,
            Name = $"{Adjectives[i % Adjectives.Length]} {Nouns[i * 7 % Nouns.Length]}",
            Category = Categories[i % Categories.Length],
            Price = 5m + i * 37 % 200 + i % 100 / 100m,
            CreatedAt = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc).AddHours(i * 13).ToString("yyyy-MM-ddTHH:mm:ssZ")
        }).ToList();

        // -------------------- SINGLE ITEM (target of HAL self links) --------------------
        /// <summary>Get one item (the target of HAL self links).</summary>
        /// <param name="id">Item id (1-100).</param>
        /// <response code="200">The item.</response>
        /// <response code="404">No item with that id.</response>
        [HttpGet("items/{id:int}")]
        public IActionResult GetItem(int id)
        {
            var item = Items.FirstOrDefault(i => i.Id == id);
            return item == null
                ? NotFound(ApiResponse.Error(404, $"Item with ID {id} does not exist."))
                : Ok(item);
        }

        // -------------------- OFFSET / LIMIT --------------------
        /// <summary>Page with offset and limit.</summary>
        /// <param name="offset">Number of items to skip (default 0; negative values become 0).</param>
        /// <param name="limit">Page size (1-100, default 10).</param>
        /// <remarks>The response is <c>{ data, pagination: { offset, limit, total, hasMore } }</c> plus <c>X-Total-Count</c>.</remarks>
        /// <response code="200">One page.</response>
        [HttpGet("offset")]
        public IActionResult Offset([FromQuery] int offset = 0, [FromQuery] int limit = 10)
        {
            offset = Math.Max(offset, 0);
            limit = Math.Clamp(limit, 1, 100);

            var data = Items.Skip(offset).Take(limit).ToList();
            Response.Headers["X-Total-Count"] = Items.Count.ToString();

            return Ok(new
            {
                data,
                pagination = new { offset, limit, total = Items.Count, hasMore = offset + data.Count < Items.Count }
            });
        }

        // -------------------- PAGE / SIZE (Spring Data envelope, 0-based) --------------------
        /// <summary>Page with a Spring Data style envelope (0-based page numbers).</summary>
        /// <param name="page">Page number, starting at 0 (default 0).</param>
        /// <param name="size">Page size (1-100, default 10).</param>
        /// <remarks>The response has <c>content</c>, <c>pageable</c>, <c>totalElements</c>, <c>totalPages</c>, <c>first</c>, <c>last</c> and <c>empty</c>.</remarks>
        /// <response code="200">One page.</response>
        [HttpGet("page")]
        public IActionResult Page([FromQuery] int page = 0, [FromQuery] int size = 10)
        {
            page = Math.Max(page, 0);
            size = Math.Clamp(size, 1, 100);

            var totalPages = (int)Math.Ceiling(Items.Count / (double)size);
            var content = Items.Skip(page * size).Take(size).ToList();

            return Ok(new
            {
                content,
                pageable = new { pageNumber = page, pageSize = size, offset = page * size, paged = true },
                totalElements = Items.Count,
                totalPages,
                number = page,
                size,
                numberOfElements = content.Count,
                first = page == 0,
                last = page >= totalPages - 1,
                empty = content.Count == 0
            });
        }

        // -------------------- CURSOR (Stripe style) --------------------
        /// <summary>Page with an opaque cursor (Stripe style).</summary>
        /// <param name="cursor">The <c>next_cursor</c> value from the previous page. Omit it for the first page.</param>
        /// <param name="limit">Page size (1-100, default 10).</param>
        /// <remarks>The response is <c>{ object: "list", data, has_more, next_cursor }</c>. Keep requesting with <c>next_cursor</c> until <c>has_more</c> is false.</remarks>
        /// <response code="200">One page.</response>
        /// <response code="400">The cursor is malformed.</response>
        [HttpGet("cursor")]
        public IActionResult Cursor([FromQuery] string? cursor = null, [FromQuery] int limit = 10)
        {
            limit = Math.Clamp(limit, 1, 100);
            var afterId = 0;

            if (!string.IsNullOrEmpty(cursor) && !TryDecodeCursor(cursor, out afterId))
                return BadRequest(ApiResponse.Error(400, "Invalid cursor. Pass the 'next_cursor' value from a previous response unchanged."));

            var data = Items.Where(i => i.Id > afterId).Take(limit).ToList();
            var hasMore = data.Count > 0 && data[^1].Id < Items[^1].Id;

            return Ok(new
            {
                @object = "list",
                url = "/api/pagination/cursor",
                data,
                has_more = hasMore,
                next_cursor = hasMore ? EncodeCursor(data[^1].Id) : null
            });
        }

        // -------------------- LINK HEADER (GitHub style, 1-based) --------------------
        /// <summary>Page with an RFC 5988 Link header (GitHub style, 1-based).</summary>
        /// <param name="page">Page number, starting at 1 (default 1).</param>
        /// <param name="perPage">Page size, sent as the <c>per_page</c> query parameter (1-100, default 10).</param>
        /// <remarks>
        /// The body is a plain array. Navigation is in the <c>Link</c> header (<c>rel="next"</c>, <c>prev</c>, <c>first</c>,
        /// <c>last</c>, with absolute URLs), plus <c>X-Total-Count</c>. A page past the end returns an empty array.
        /// </remarks>
        /// <response code="200">One page.</response>
        [HttpGet("link")]
        public IActionResult Link([FromQuery] int page = 1, [FromQuery(Name = "per_page")] int perPage = 10)
        {
            page = Math.Max(page, 1);
            perPage = Math.Clamp(perPage, 1, 100);

            var lastPage = (int)Math.Ceiling(Items.Count / (double)perPage);
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}";
            string Url(int p) => $"<{baseUrl}?page={p}&per_page={perPage}>";

            var links = new List<string>();
            if (page < lastPage) links.Add($"{Url(page + 1)}; rel=\"next\"");
            links.Add($"{Url(lastPage)}; rel=\"last\"");
            links.Add($"{Url(1)}; rel=\"first\"");
            if (page > 1) links.Add($"{Url(Math.Min(page - 1, lastPage))}; rel=\"prev\"");

            Response.Headers["Link"] = string.Join(", ", links);
            Response.Headers["X-Total-Count"] = Items.Count.ToString();

            // Like GitHub, a page past the end is an empty array, not an error.
            return Ok(Items.Skip((page - 1) * perPage).Take(perPage).ToList());
        }

        // -------------------- KEYSET (after_id) --------------------
        /// <summary>Page by key: return items with an id greater than <c>after_id</c>.</summary>
        /// <param name="afterId">Last id seen, sent as the <c>after_id</c> query parameter (default 0).</param>
        /// <param name="limit">Page size (1-100, default 10).</param>
        /// <remarks>The response is <c>{ data, has_more, next_after_id }</c>.</remarks>
        /// <response code="200">One page.</response>
        [HttpGet("keyset")]
        public IActionResult Keyset([FromQuery(Name = "after_id")] int afterId = 0, [FromQuery] int limit = 10)
        {
            limit = Math.Clamp(limit, 1, 100);

            var data = Items.Where(i => i.Id > afterId).OrderBy(i => i.Id).Take(limit).ToList();
            var hasMore = data.Count > 0 && data[^1].Id < Items[^1].Id;

            return Ok(new
            {
                data,
                has_more = hasMore,
                next_after_id = hasMore ? data[^1].Id : (int?)null
            });
        }

        // -------------------- HAL (_links / _embedded, 0-based) --------------------
        /// <summary>Page with HAL hypermedia links (0-based).</summary>
        /// <param name="page">Page number, starting at 0 (default 0).</param>
        /// <param name="size">Page size (1-100, default 10).</param>
        /// <remarks>Content type is <c>application/hal+json</c>. Items are under <c>_embedded.items</c>, each with a self link; <c>_links</c> holds self, first, prev, next and last.</remarks>
        /// <response code="200">One page.</response>
        [HttpGet("hal")]
        public IActionResult Hal([FromQuery] int page = 0, [FromQuery] int size = 10)
        {
            page = Math.Max(page, 0);
            size = Math.Clamp(size, 1, 100);

            var totalPages = (int)Math.Ceiling(Items.Count / (double)size);
            object Href(int p) => new { href = $"/api/pagination/hal?page={p}&size={size}" };

            var links = new Dictionary<string, object>
            {
                ["self"] = Href(page),
                ["first"] = Href(0)
            };
            if (page > 0) links["prev"] = Href(Math.Min(page - 1, totalPages - 1));
            if (page < totalPages - 1) links["next"] = Href(page + 1);
            links["last"] = Href(totalPages - 1);

            var items = Items.Skip(page * size).Take(size).Select(i => new
            {
                i.Id,
                i.Name,
                i.Category,
                i.Price,
                i.CreatedAt,
                _links = new { self = new { href = $"/api/pagination/items/{i.Id}" } }
            }).ToList();

            return new ObjectResult(new
            {
                _embedded = new { items },
                _links = links,
                page = new { size, totalElements = Items.Count, totalPages, number = page }
            })
            {
                ContentTypes = { "application/hal+json" }
            };
        }

        private static string EncodeCursor(int lastId) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { lastId })))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static bool TryDecodeCursor(string cursor, out int lastId)
        {
            lastId = 0;
            try
            {
                var base64 = cursor.Replace('-', '+').Replace('_', '/');
                base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

                using var doc = JsonDocument.Parse(Convert.FromBase64String(base64));
                return doc.RootElement.TryGetProperty("lastId", out var value) && value.TryGetInt32(out lastId) && lastId >= 0;
            }
            catch (Exception ex) when (ex is FormatException or JsonException or InvalidOperationException)
            {
                return false;
            }
        }
    }
}
