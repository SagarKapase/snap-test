using snap_test.Data;
using Microsoft.AspNetCore.Mvc;
using snap_test.Models;

namespace snap_test.Controllers
{
    /// <summary>Photos with full-size and thumbnail URLs; each belongs to an album through albumId. Hardcoded in-memory data: writes last until the app restarts.</summary>
    [Route("api/[controller]")]
    public class PhotosController : CrudControllerBase<Photo>
    {
        /// <inheritdoc />
        protected override List<Photo> Store => PhotoStore.Photos;
        /// <inheritdoc />
        protected override string ResourceName => "Photo";

        /// <inheritdoc />
        protected override string? Validate(Photo item)
        {
            if (item == null) return "Request body is required.";
            if (string.IsNullOrWhiteSpace(item.Title)) return "Missing required field: title";
            if (item.AlbumId <= 0) return "Field 'albumId' must be a positive integer.";
            if (!Uri.TryCreate(item.Url, UriKind.Absolute, out _)) return "Field 'url' must be an absolute URL.";
            return null;
        }
    }
}
