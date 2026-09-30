using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// File download testing: attachments vs inline, deterministic binary blobs, HTTP Range requests
    /// and large streamed downloads. All content is hardcoded or generated deterministically.
    /// </summary>
    [ApiController]
    [Route("api/files")]
    public class FilesController : ControllerBase
    {
        private const int MaxBytes = 10 * 1024 * 1024; // 10 MB
        private const int MaxLargeMb = 20;

        private static readonly Dictionary<string, (string contentType, Func<byte[]> content)> Samples =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["sample.txt"] = ("text/plain; charset=utf-8", () => Encoding.UTF8.GetBytes(SampleContent.Text)),
                ["sample.csv"] = ("text/csv; charset=utf-8", () => Encoding.UTF8.GetBytes(SampleContent.Csv)),
                ["sample.json"] = ("application/json; charset=utf-8", () => Encoding.UTF8.GetBytes(SampleContent.Json)),
                ["sample.xml"] = ("application/xml; charset=utf-8", () => Encoding.UTF8.GetBytes(SampleContent.Xml)),
                ["sample.pdf"] = ("application/pdf", () => SampleContent.Pdf()),
                ["sample.png"] = ("image/png", () => SampleContent.Png(256)),
                ["sample.gif"] = ("image/gif", () => SampleContent.Gif),
                ["sample.zip"] = ("application/zip", () => SampleContent.Zip())
            };

        // -------------------- LIST --------------------
        /// <summary>List the downloadable sample files with their size and SHA-256.</summary>
        /// <response code="200">Success.</response>
        [HttpGet]
        public IActionResult List()
        {
            var files = Samples.Select(kv =>
            {
                var bytes = kv.Value.content();
                return new
                {
                    name = kv.Key,
                    contentType = kv.Value.contentType,
                    size = bytes.Length,
                    sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
                    download = $"/api/files/{kv.Key}",
                    inline = $"/api/files/{kv.Key}/inline"
                };
            });

            return Ok(files);
        }

        // -------------------- DOWNLOAD (attachment) --------------------
        /// <summary>Download a sample file as an attachment.</summary>
        /// <param name="name">File name from <c>GET /api/files</c>, e.g. <c>sample.pdf</c>. Case-insensitive.</param>
        /// <remarks>Sends <c>Content-Disposition: attachment</c> and supports single <c>Range</c> requests. HEAD returns the same headers without a body.</remarks>
        /// <response code="200">The file.</response>
        /// <response code="206">The requested byte range of the file.</response>
        /// <response code="404">No such file.</response>
        [HttpGet("{name}")]
        [HttpHead("{name}")]
        public IActionResult Download(string name)
        {
            if (!Samples.TryGetValue(name, out var sample))
                return NotFound(ApiResponse.Error(404, $"File '{name}' does not exist. See GET /api/files for the list."));

            // Passing a download name makes ASP.NET send Content-Disposition: attachment.
            return File(sample.content(), sample.contentType, name.ToLowerInvariant(), enableRangeProcessing: true);
        }

        // -------------------- VIEW (inline) --------------------
        /// <summary>Serve a sample file inline so a browser displays it.</summary>
        /// <param name="name">File name from <c>GET /api/files</c>, e.g. <c>sample.png</c>. Case-insensitive.</param>
        /// <response code="200">The file, with <c>Content-Disposition: inline</c>.</response>
        /// <response code="404">No such file.</response>
        [HttpGet("{name}/inline")]
        public IActionResult Inline(string name)
        {
            if (!Samples.TryGetValue(name, out var sample))
                return NotFound(ApiResponse.Error(404, $"File '{name}' does not exist. See GET /api/files for the list."));

            Response.Headers["Content-Disposition"] = $"inline; filename=\"{name.ToLowerInvariant()}\"";
            return File(sample.content(), sample.contentType);
        }

        // -------------------- DETERMINISTIC BYTES --------------------
        /// <summary>Download deterministic pseudo-random bytes.</summary>
        /// <param name="n">Number of bytes, 0–10485760 (10 MB).</param>
        /// <param name="seed">Random seed, default 42. The same <c>n</c> and <c>seed</c> always return the same bytes.</param>
        /// <remarks>The <c>X-Content-SHA256</c> response header carries the body's hash for verification.</remarks>
        /// <response code="200">Success.</response>
        /// <response code="400"><c>n</c> is outside 0–10485760.</response>
        [HttpGet("bytes/{n:int}")]
        public IActionResult Bytes(int n, [FromQuery] int seed = 42)
        {
            if (n < 0 || n > MaxBytes)
                return BadRequest(ApiResponse.Error(400, $"n must be between 0 and {MaxBytes} bytes."));

            var bytes = SampleContent.SeededBytes(n, seed);
            Response.Headers["X-Content-SHA256"] = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            return File(bytes, "application/octet-stream", $"bytes-{n}-seed-{seed}.bin");
        }

        // -------------------- RANGE REQUESTS --------------------
        /// <summary>Download a verifiable byte pattern that supports HTTP Range requests.</summary>
        /// <param name="n">Total size in bytes, 1–10485760 (10 MB).</param>
        /// <remarks>
        /// The byte at offset <c>i</c> is <c>i % 256</c>, so any slice can be checked. Single ranges such as <c>bytes=0-99</c>,
        /// <c>bytes=500-</c> and <c>bytes=-10</c> return 206; unsatisfiable ranges return 416, and multiple ranges are ignored (full 200).
        /// A fixed ETag and Last-Modified make <c>If-None-Match</c>, <c>If-Modified-Since</c> and <c>If-Range</c> work. HEAD returns headers only.
        /// </remarks>
        /// <response code="200">The full body.</response>
        /// <response code="206">The requested range.</response>
        /// <response code="304">Not modified (a conditional request matched).</response>
        /// <response code="400"><c>n</c> is outside 1–10485760.</response>
        /// <response code="416">The range is not satisfiable.</response>
        [HttpGet("range/{n:int}")]
        [HttpHead("range/{n:int}")]
        public IActionResult Range(int n)
        {
            if (n < 1 || n > MaxBytes)
                return BadRequest(ApiResponse.Error(400, $"n must be between 1 and {MaxBytes} bytes."));

            // Byte at offset i == i % 256, so clients can verify any returned slice.
            var bytes = new byte[n];
            for (var i = 0; i < n; i++) bytes[i] = (byte)(i % 256);

            var etag = new Microsoft.Net.Http.Headers.EntityTagHeaderValue($"\"range-{n}\"");
            return File(bytes, "application/octet-stream", lastModified: new DateTimeOffset(2025, 7, 1, 0, 0, 0, TimeSpan.Zero),
                entityTag: etag, enableRangeProcessing: true);
        }

        // -------------------- LARGE STREAMED DOWNLOAD --------------------
        /// <summary>Stream a large binary download without buffering it in memory.</summary>
        /// <param name="mb">Size in megabytes, 1–20, default 5.</param>
        /// <remarks>Content-Length is sent up front and the body is written in 64 KB chunks, which is handy for testing progress bars and timeouts.</remarks>
        /// <response code="200">Success.</response>
        /// <response code="400"><c>mb</c> is outside 1–20.</response>
        [HttpGet("large")]
        public async Task Large([FromQuery] int mb = 5)
        {
            if (mb < 1 || mb > MaxLargeMb)
            {
                Response.StatusCode = 400;
                await Response.WriteAsJsonAsync(ApiResponse.Error(400, $"mb must be between 1 and {MaxLargeMb}."));
                return;
            }

            const int chunkSize = 64 * 1024;
            var chunk = SampleContent.SeededBytes(chunkSize, 7);
            long total = (long)mb * 1024 * 1024;

            Response.ContentType = "application/octet-stream";
            Response.ContentLength = total;
            Response.Headers["Content-Disposition"] = $"attachment; filename=\"large-{mb}mb.bin\"";

            try
            {
                for (long sent = 0; sent < total; sent += chunkSize)
                    await Response.Body.WriteAsync(chunk.AsMemory(0, (int)Math.Min(chunkSize, total - sent)), HttpContext.RequestAborted);
            }
            catch (OperationCanceledException)
            {
                // Client disconnected mid-download — nothing to do.
            }
        }
    }
}
