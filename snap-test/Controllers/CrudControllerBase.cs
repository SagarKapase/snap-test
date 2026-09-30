using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// Generic in-memory CRUD for a seed store. A derived controller only supplies its route, store and name:
    /// GET / (pagination/sort/filter/search), GET /count, GET|HEAD /{id}, POST /, POST /bulk,
    /// PUT /{id}, PATCH /{id} (merge patch), DELETE /{id}, DELETE /bulk?ids=1,2,3.
    /// Writes mutate the static list and reset on app restart (non-persistent, per PRD).
    /// </summary>
    /// <typeparam name="T">Record type stored in the seed list.</typeparam>
    [ApiController]
    public abstract class CrudControllerBase<T> : ControllerBase where T : class, IEntity
    {
        /// <summary>The seed list this controller serves.</summary>
        protected abstract List<T> Store { get; }

        /// <summary>Singular display name used in messages, e.g. "Employee".</summary>
        protected abstract string ResourceName { get; }

        /// <summary>Override to reject bad payloads on POST/PUT. Return null when valid.</summary>
        protected virtual string? Validate(T item) => null;

        // -------------------- GET ALL (pagination / sort / filter / search) --------------------
        /// <summary>List records with filtering, sorting, search and pagination.</summary>
        /// <remarks>
        /// Query parameters (all optional):
        ///
        /// - `limit`: page size, 1-100 (default: all records).
        /// - `page`: 1-based page number; only applies together with `limit`.
        /// - `offset`: number of records to skip (used when `page` is not given).
        /// - `sort`: property name to sort by, e.g. `sort=title`.
        /// - `order`: `asc` (default) or `desc`.
        /// - `q`: case-insensitive search across title, name, body, description and text fields.
        /// - Any other property name filters by exact, case-insensitive value, e.g. `?department=engineering`.
        ///
        /// Totals are returned in the X-Total-Count, X-Page, X-Per-Page and X-Total-Pages response headers.
        /// </remarks>
        /// <response code="200">The matching records (an empty array when nothing matches).</response>
        [HttpGet]
        public virtual IActionResult GetAll()
        {
            lock (Store)
            {
                var (items, total, page, perPage, totalPages) = QueryHelper.Apply(Store, Request.Query);

                Response.Headers["X-Total-Count"] = total.ToString();
                Response.Headers["X-Page"] = page.ToString();
                Response.Headers["X-Per-Page"] = perPage.ToString();
                Response.Headers["X-Total-Pages"] = totalPages.ToString();

                return Ok(items);
            }
        }

        // -------------------- COUNT --------------------
        /// <summary>Count all records.</summary>
        /// <response code="200">An object of the form { count }.</response>
        [HttpGet("count")]
        public virtual IActionResult Count()
        {
            lock (Store) return Ok(new { count = Store.Count });
        }

        // -------------------- GET / HEAD BY ID --------------------
        /// <summary>Get a record by ID (HEAD returns the headers only).</summary>
        /// <param name="id">Record ID.</param>
        /// <response code="200">The record.</response>
        /// <response code="404">No record has this ID.</response>
        [HttpGet("{id:int}")]
        [HttpHead("{id:int}")]
        public virtual IActionResult GetById(int id)
        {
            lock (Store)
            {
                var item = Store.FirstOrDefault(x => x.Id == id);
                return item == null ? NotFoundError(id) : Ok(item);
            }
        }

        // -------------------- CREATE --------------------
        /// <summary>Create a record.</summary>
        /// <remarks>The server assigns the next free ID; any ID in the body is ignored.</remarks>
        /// <param name="item">The record to create.</param>
        /// <response code="201">Created; returns { message, data } with the generated ID.</response>
        /// <response code="400">The body is missing, malformed or fails validation.</response>
        [HttpPost]
        public virtual IActionResult Create([FromBody] T item)
        {
            var error = Validate(item);
            if (error != null) return BadRequest(ApiResponse.Error(400, error));

            lock (Store)
            {
                item.Id = NextId();
                Store.Add(item);
            }

            return StatusCode(201, new { message = $"{ResourceName} created successfully", data = item });
        }

        // -------------------- BULK CREATE --------------------
        /// <summary>Create up to 100 records in one request.</summary>
        /// <remarks>All items are validated first; if any item is invalid nothing is stored and the error names its index.</remarks>
        /// <param name="items">JSON array of 1 to 100 records.</param>
        /// <response code="201">Created; returns { message, data } with every generated ID.</response>
        /// <response code="400">The array is empty, has more than 100 items, or an item fails validation.</response>
        [HttpPost("bulk")]
        public virtual IActionResult BulkCreate([FromBody] List<T> items)
        {
            if (items == null || items.Count == 0)
                return BadRequest(ApiResponse.Error(400, "Body must be a non-empty JSON array."));
            if (items.Count > 100)
                return BadRequest(ApiResponse.Error(400, "Bulk create accepts at most 100 items."));

            for (var i = 0; i < items.Count; i++)
            {
                var error = Validate(items[i]);
                if (error != null) return BadRequest(ApiResponse.Error(400, $"Item [{i}]: {error}"));
            }

            lock (Store)
            {
                foreach (var item in items)
                {
                    item.Id = NextId();
                    Store.Add(item);
                }
            }

            return StatusCode(201, new { message = $"{items.Count} {ResourceName.ToLower()} record(s) created", data = items });
        }

        // -------------------- REPLACE --------------------
        /// <summary>Replace a record.</summary>
        /// <remarks>Every field is overwritten with the body; the ID from the route wins over any ID in the body.</remarks>
        /// <param name="id">Record ID.</param>
        /// <param name="item">The full replacement record.</param>
        /// <response code="200">Replaced; returns { message, data }.</response>
        /// <response code="400">The body is missing, malformed or fails validation.</response>
        /// <response code="404">No record has this ID.</response>
        [HttpPut("{id:int}")]
        public virtual IActionResult Replace(int id, [FromBody] T item)
        {
            var error = Validate(item);
            if (error != null) return BadRequest(ApiResponse.Error(400, error));

            lock (Store)
            {
                var index = Store.FindIndex(x => x.Id == id);
                if (index < 0) return NotFoundError(id);

                item.Id = id;
                Store[index] = item;
            }

            return Ok(new { message = $"{ResourceName} updated successfully", data = item });
        }

        // -------------------- PARTIAL UPDATE (merge patch) --------------------
        /// <summary>Partially update a record with a JSON merge patch.</summary>
        /// <remarks>
        /// Only the fields present in the body change, and nested objects are merged rather than replaced.
        /// The ID and unknown fields are never applied; they are listed in `ignored`.
        /// The patched record is validated before it is saved, so a rejected patch changes nothing.
        /// </remarks>
        /// <param name="id">Record ID.</param>
        /// <param name="patch">JSON object with the fields to change, e.g. { "price": 12.5 }.</param>
        /// <response code="200">Patched; returns { message, changed, ignored, data }.</response>
        /// <response code="400">The body is not an object, sets a non-nullable field to null, has a wrongly typed value, or the result fails validation.</response>
        /// <response code="404">No record has this ID.</response>
        [HttpPatch("{id:int}")]
        public virtual IActionResult Patch(int id, [FromBody] JsonElement patch)
        {
            lock (Store)
            {
                var index = Store.FindIndex(x => x.Id == id);
                if (index < 0) return NotFoundError(id);

                // Patch a deep copy, validate it, and only then swap it in: a bad patch never half-applies.
                var copy = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(Store[index]))!;

                try
                {
                    var (changed, ignored) = PatchHelper.Apply(copy, patch);

                    var error = Validate(copy);
                    if (error != null) return BadRequest(ApiResponse.Error(400, error));

                    Store[index] = copy;
                    return Ok(new { message = $"{ResourceName} patched successfully", changed, ignored, data = copy });
                }
                catch (Exception ex) when (ex is ArgumentException or JsonException)
                {
                    return BadRequest(ApiResponse.Error(400, ex.Message));
                }
            }
        }

        // -------------------- DELETE --------------------
        /// <summary>Delete a record.</summary>
        /// <param name="id">Record ID.</param>
        /// <response code="200">Deleted.</response>
        /// <response code="404">No record has this ID.</response>
        [HttpDelete("{id:int}")]
        public virtual IActionResult Delete(int id)
        {
            lock (Store)
            {
                var removed = Store.RemoveAll(x => x.Id == id);
                if (removed == 0) return NotFoundError(id);
            }

            return Ok(new { message = $"{ResourceName} deleted successfully" });
        }

        // -------------------- BULK DELETE --------------------
        /// <summary>Delete several records by ID.</summary>
        /// <param name="ids">Comma-separated record IDs, e.g. 1,2,3.</param>
        /// <response code="200">Returns { message, deleted, notFound }; IDs that do not exist are listed in notFound.</response>
        /// <response code="400">The ids parameter is missing or is not a list of integers.</response>
        [HttpDelete("bulk")]
        public virtual IActionResult BulkDelete([FromQuery] string ids)
        {
            var parsed = (ids ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var n) ? n : (int?)null)
                .ToList();

            if (parsed.Count == 0 || parsed.Any(n => n == null))
                return BadRequest(ApiResponse.Error(400, "Query parameter 'ids' must be a comma-separated list of integers, e.g. ?ids=1,2,3"));

            var wanted = parsed.Select(n => n!.Value).ToHashSet();
            List<int> deleted;

            lock (Store)
            {
                deleted = Store.Where(x => wanted.Contains(x.Id)).Select(x => x.Id).ToList();
                Store.RemoveAll(x => wanted.Contains(x.Id));
            }

            var notFound = wanted.Except(deleted).ToList();
            return Ok(new { message = $"{deleted.Count} {ResourceName.ToLower()} record(s) deleted", deleted, notFound });
        }

        /// <summary>Next free ID: one more than the highest ID in the store.</summary>
        protected int NextId() => Store.Count == 0 ? 1 : Store.Max(x => x.Id) + 1;

        /// <summary>Standard 404 body for a missing record.</summary>
        /// <param name="id">The ID that was not found.</param>
        protected IActionResult NotFoundError(int id) =>
            NotFound(ApiResponse.Error(404, $"{ResourceName} with ID {id} does not exist."));
    }
}
