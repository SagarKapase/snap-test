using Microsoft.AspNetCore.Mvc;
using snap_test.Data;
using snap_test.Models;

namespace snap_test.Controllers
{
    /// <summary>Photo albums in JSONPlaceholder style; each seed album has five photos. Hardcoded in-memory data: writes last until the app restarts.</summary>
    [Route("api/[controller]")]
    public class AlbumsController : CrudControllerBase<Album>
    {
        /// <inheritdoc />
        protected override List<Album> Store => AlbumStore.Albums;
        /// <inheritdoc />
        protected override string ResourceName => "Album";

        /// <inheritdoc />
        protected override string? Validate(Album item)
        {
            if (item == null) return "Request body is required.";
            if (string.IsNullOrWhiteSpace(item.Title)) return "Missing required field: title";
            if (item.UserId <= 0) return "Field 'userId' must be a positive integer.";
            return null;
        }

        // -------------------- ALBUM PHOTOS (nested) --------------------
        /// <summary>List the photos in an album.</summary>
        /// <param name="id">Album ID.</param>
        /// <response code="200">Photos whose albumId is this ID (an empty array if none).</response>
        /// <response code="404">The album does not exist.</response>
        [HttpGet("{id:int}/photos")]
        public IActionResult GetPhotos(int id)
        {
            lock (Store)
            {
                if (!Store.Any(a => a.Id == id)) return NotFoundError(id);
            }

            lock (PhotoStore.Photos)
            {
                return Ok(PhotoStore.Photos.Where(p => p.AlbumId == id).ToList());
            }
        }
    }
}
