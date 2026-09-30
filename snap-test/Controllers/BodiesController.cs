using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// Request-body parsing per content type. Typed endpoints only accept their own Content-Type (415 otherwise)
    /// and report what they parsed; /any sniffs the payload; /large measures uploads up to 10MB.
    /// </summary>
    [ApiController]
    [Route("api/bodies")]
    public class BodiesController : ControllerBase
    {
        private const int MaxTextBytes = 1024 * 1024;
        private const int MaxBinaryBytes = 10 * 1024 * 1024;
        private const int MaxXmlDepth = 32;

        // -------------------- JSON --------------------
        /// <summary>Parses a JSON body and describes its structure.</summary>
        /// <param name="body">Any JSON value: object, array or scalar.</param>
        /// <remarks>Accepts <c>application/json</c> and <c>application/*+json</c>.</remarks>
        /// <response code="200">Root type, top-level keys or array length, and the parsed value.</response>
        /// <response code="400">Empty or malformed JSON.</response>
        /// <response code="415">Any other Content-Type.</response>
        [HttpPost("json")]
        [Consumes("application/json", "application/*+json")]
        public IActionResult Json([FromBody] JsonElement body) => Ok(new
        {
            contentType = Request.ContentType,
            rootType = body.ValueKind.ToString().ToLowerInvariant(),
            keys = body.ValueKind == JsonValueKind.Object ? body.EnumerateObject().Select(p => p.Name).ToList() : null,
            length = body.ValueKind == JsonValueKind.Array ? body.GetArrayLength() : (int?)null,
            received = body
        });

        // -------------------- FORM (urlencoded or multipart) --------------------
        /// <summary>Parses a URL-encoded or multipart form.</summary>
        /// <remarks>Repeated keys come back as arrays. Each uploaded file is reported with its size and SHA-256.</remarks>
        /// <response code="200">Encoding, fields and file metadata.</response>
        /// <response code="415">Content-Type is not <c>application/x-www-form-urlencoded</c> or <c>multipart/form-data</c>.</response>
        [HttpPost("form")]
        [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
        public async Task<IActionResult> Form()
        {
            var form = await Request.ReadFormAsync();

            var files = new List<object>();
            foreach (var file in form.Files)
            {
                await using var stream = file.OpenReadStream();
                files.Add(new
                {
                    field = file.Name,
                    fileName = file.FileName,
                    contentType = file.ContentType,
                    size = file.Length,
                    sha256 = Convert.ToHexString(await SHA256.HashDataAsync(stream)).ToLowerInvariant()
                });
            }

            return Ok(new
            {
                contentType = Request.ContentType,
                encoding = Request.ContentType!.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase) ? "multipart" : "urlencoded",
                fields = HttpRequestReader.Flatten(form),
                files
            });
        }

        // -------------------- PLAIN TEXT --------------------
        /// <summary>Reads a plain-text body and counts its bytes, characters, lines and words.</summary>
        /// <remarks>Up to 1MB is analysed; larger bodies are counted and flagged with <c>truncated</c>.</remarks>
        /// <response code="200">Text statistics and the text itself.</response>
        /// <response code="400">Body is not valid UTF-8.</response>
        /// <response code="415">Content-Type is not <c>text/plain</c>.</response>
        [HttpPost("text")]
        [Consumes("text/plain")]
        public async Task<IActionResult> Text()
        {
            var (bytes, total, truncated) = await HttpRequestReader.ReadCappedAsync(Request, MaxTextBytes);
            var text = HttpRequestReader.TryDecodeUtf8(bytes);
            if (text == null)
                return BadRequest(ApiResponse.Error(400, "Body is not valid UTF-8 text."));

            return Ok(new
            {
                contentType = Request.ContentType,
                bytes = total,
                characters = text.Length,
                lines = text.Length == 0 ? 0 : text.Split('\n').Length,
                words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length,
                truncated,
                text
            });
        }

        // -------------------- XML --------------------
        /// <summary>Parses an XML body into an element tree.</summary>
        /// <remarks>
        /// Accepts <c>application/xml</c> and <c>text/xml</c>, up to 1MB. DTDs are rejected, which blocks XXE and
        /// entity-expansion attacks. Example body: <c>&lt;order id="7"&gt;&lt;item&gt;Pen&lt;/item&gt;&lt;/order&gt;</c>.
        /// </remarks>
        /// <response code="200">Root name, element count and the element tree.</response>
        /// <response code="400">Malformed XML or a DTD.</response>
        /// <response code="413">Body larger than 1MB.</response>
        /// <response code="415">Any other Content-Type.</response>
        [HttpPost("xml")]
        [Consumes("application/xml", "text/xml")]
        public async Task<IActionResult> Xml()
        {
            var bytes = await HttpRequestReader.ReadLimitedAsync(Request, MaxTextBytes);
            if (bytes == null)
                return StatusCode(413, ApiResponse.Error(413, "XML body exceeds 1MB."));

            XDocument doc;
            try
            {
                // DTDs are prohibited and external resolution disabled (no XXE / billion-laughs).
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
                using var reader = XmlReader.Create(new MemoryStream(bytes), settings);
                doc = XDocument.Load(reader);
            }
            catch (XmlException ex)
            {
                return BadRequest(ApiResponse.Error(400, $"Malformed XML: {ex.Message}"));
            }

            return Ok(new
            {
                contentType = Request.ContentType,
                root = doc.Root!.Name.LocalName,
                elementCount = doc.Descendants().Count(),
                tree = ToTree(doc.Root, 0)
            });
        }

        // -------------------- BINARY --------------------
        /// <summary>Reads a binary body and returns its size, hashes and detected type.</summary>
        /// <remarks>Accepts <c>application/octet-stream</c>, <c>application/pdf</c>, <c>application/zip</c>, <c>image/png</c>, <c>image/jpeg</c> and <c>image/gif</c>, up to 10MB.</remarks>
        /// <response code="200">Size, SHA-256, MD5, first 16 bytes and the type detected from magic bytes.</response>
        /// <response code="413">Body larger than 10MB.</response>
        /// <response code="415">Any other Content-Type.</response>
        [HttpPost("binary")]
        [Consumes("application/octet-stream", "application/pdf", "application/zip", "image/png", "image/jpeg", "image/gif")]
        public async Task<IActionResult> Binary()
        {
            var bytes = await HttpRequestReader.ReadLimitedAsync(Request, MaxBinaryBytes);
            if (bytes == null)
                return StatusCode(413, ApiResponse.Error(413, "Binary body exceeds 10MB."));

            return Ok(new
            {
                contentType = Request.ContentType,
                size = bytes.Length,
                sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
                md5 = Convert.ToHexString(MD5.HashData(bytes)).ToLowerInvariant(),
                first16BytesHex = Convert.ToHexString(bytes.AsSpan(0, Math.Min(16, bytes.Length))),
                detectedType = Detect(bytes, Request.ContentType)
            });
        }

        // -------------------- ANY (content sniffing) --------------------
        /// <summary>Detects a body's real type from its content and compares it with the declared Content-Type.</summary>
        /// <remarks>
        /// Accepts POST, PUT and PATCH, up to 10MB. Recognises PNG, JPEG, GIF, PDF, ZIP and gzip by magic bytes, then JSON,
        /// HTML, XML, URL-encoded form, multipart and plain text; anything else is <c>application/octet-stream</c>.
        /// </remarks>
        /// <response code="200">Declared and detected types, length and SHA-256.</response>
        /// <response code="413">Body larger than 10MB.</response>
        [AcceptVerbs("POST", "PUT", "PATCH", Route = "any")]
        public async Task<IActionResult> Any()
        {
            var bytes = await HttpRequestReader.ReadLimitedAsync(Request, MaxBinaryBytes);
            if (bytes == null)
                return StatusCode(413, ApiResponse.Error(413, "Body exceeds 10MB."));

            var detected = Detect(bytes, Request.ContentType);
            return Ok(new
            {
                method = Request.Method,
                declaredContentType = Request.ContentType,
                detectedType = detected,
                matchesDeclared = Request.ContentType?.Contains(detected, StringComparison.OrdinalIgnoreCase) ?? false,
                length = bytes.Length,
                sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()
            });
        }

        // -------------------- LARGE UPLOAD (streamed, up to 10MB) --------------------
        /// <summary>Streams an upload of up to 10MB and reports its size, hash and timing.</summary>
        /// <remarks>Any Content-Type is accepted. The body is hashed while it streams, so it is never held in memory.</remarks>
        /// <response code="200">Size, human-readable size, SHA-256 and duration.</response>
        /// <response code="413">Upload larger than 10MB.</response>
        [HttpPost("large")]
        public async Task<IActionResult> Large()
        {
            var watch = Stopwatch.StartNew();
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[81920];
            long size = 0;
            int read;

            while ((read = await Request.Body.ReadAsync(buffer, HttpContext.RequestAborted)) > 0)
            {
                size += read;
                if (size > MaxBinaryBytes)
                    return StatusCode(413, ApiResponse.Error(413, "Upload exceeds the 10MB limit."));
                hash.AppendData(buffer, 0, read);
            }

            return Ok(new
            {
                size,
                sizeHuman = size >= 1024 * 1024 ? $"{size / 1024.0 / 1024.0:0.##} MB" : $"{size / 1024.0:0.##} KB",
                sha256 = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant(),
                durationMs = watch.ElapsedMilliseconds,
                limitBytes = MaxBinaryBytes
            });
        }

        private static object ToTree(XElement e, int depth) => new
        {
            name = e.Name.LocalName,
            @namespace = string.IsNullOrEmpty(e.Name.NamespaceName) ? null : e.Name.NamespaceName,
            attributes = e.Attributes()
                .Where(a => !a.IsNamespaceDeclaration)
                .GroupBy(a => a.Name.LocalName)
                .ToDictionary(g => g.Key, g => g.First().Value),
            text = e.HasElements ? null : e.Value,
            children = depth >= MaxXmlDepth || !e.HasElements
                ? new List<object>()
                : e.Elements().Select(c => ToTree(c, depth + 1)).ToList()
        };

        // Magic bytes first, then text-based formats.
        private static string Detect(byte[] b, string? declared)
        {
            if (b.Length == 0) return "empty";
            if (b.Length >= 4 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "image/png";
            if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "image/jpeg";
            if (b.Length >= 4 && b[0] == 'G' && b[1] == 'I' && b[2] == 'F' && b[3] == '8') return "image/gif";
            if (b.Length >= 4 && b[0] == '%' && b[1] == 'P' && b[2] == 'D' && b[3] == 'F') return "application/pdf";
            if (b.Length >= 4 && b[0] == 'P' && b[1] == 'K' && b[2] == 3 && b[3] == 4) return "application/zip";
            if (b.Length >= 2 && b[0] == 0x1F && b[1] == 0x8B) return "application/gzip";

            var text = HttpRequestReader.TryDecodeUtf8(b);
            if (text == null) return "application/octet-stream";

            var trimmed = text.TrimStart('﻿', ' ', '\t', '\r', '\n');
            if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
            {
                try { using var _ = JsonDocument.Parse(trimmed); return "application/json"; }
                catch (JsonException) { /* not JSON, fall through */ }
            }
            if (trimmed.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase)) return "text/html";
            if (trimmed.StartsWith('<')) return "application/xml";
            if (declared?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) == true) return "multipart/form-data";
            if (trimmed.Contains('=') && !trimmed.Contains(' ') && !trimmed.Contains('\n')) return "application/x-www-form-urlencoded";
            return "text/plain";
        }
    }
}
