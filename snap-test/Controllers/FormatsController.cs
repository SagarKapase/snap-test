using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// Hardcoded responses in every common content type, plus content negotiation, pre-compressed bodies,
    /// charsets and deliberately broken payloads for negative testing.
    /// </summary>
    [ApiController]
    [Route("api/formats")]
    public class FormatsController : ControllerBase
    {
        private const string SampleJson = "{\"id\":1,\"name\":\"Alice Johnson\",\"email\":\"alice@example.com\",\"roles\":[\"admin\",\"editor\"],\"active\":true,\"score\":98.5,\"manager\":null}";

        // -------------------- INDEX --------------------
        /// <summary>List every available format and special-case endpoint.</summary>
        /// <response code="200">Success.</response>
        [HttpGet]
        public IActionResult Index() => Ok(new
        {
            formats = new[] { "json", "xml", "html", "text", "csv", "tsv", "yaml", "markdown", "svg", "png", "gif", "pdf", "javascript", "css", "rss", "atom", "ical" },
            special = new[] { "jsonp?callback=fn", "negotiate", "gzip", "deflate", "brotli", "empty", "no-content", "malformed-json", "wrong-content-type", "bom", "charset/latin1", "charset/utf16" }
        });

        // -------------------- STRUCTURED DATA --------------------
        /// <summary>Return a sample user object as <c>application/json</c>.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("json")]
        public IActionResult Json() => Content(SampleJson, "application/json; charset=utf-8");

        /// <summary>Return a sample user list as <c>application/xml</c>.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("xml")]
        public IActionResult Xml() => Content(SampleContent.Xml, "application/xml; charset=utf-8");

        /// <summary>Return a sample employee table as RFC 4180 <c>text/csv</c>.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("csv")]
        public IActionResult Csv() => Content(SampleContent.Csv, "text/csv; charset=utf-8");

        /// <summary>Return the sample employee table as <c>text/tab-separated-values</c>.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("tsv")]
        public IActionResult Tsv() => Content(SampleContent.Tsv, "text/tab-separated-values; charset=utf-8");

        /// <summary>Return a sample user object as <c>application/yaml</c>.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("yaml")]
        public IActionResult Yaml() => Content(
            "id: 1\n" +
            "name: Alice Johnson\n" +
            "email: alice@example.com\n" +
            "active: true\n" +
            "score: 98.5\n" +
            "manager: null\n" +
            "roles:\n" +
            "  - admin\n" +
            "  - editor\n" +
            "address:\n" +
            "  city: Pune\n" +
            "  country: India\n" +
            "  zip: \"411001\"\n",
            "application/yaml; charset=utf-8");

        // -------------------- TEXT / MARKUP --------------------
        /// <summary>Return a sample plain-text document.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("text")]
        public IActionResult Text() => Content(SampleContent.Text, "text/plain; charset=utf-8");

        /// <summary>Return a sample HTML page with a list, a table and a form.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("html")]
        public IActionResult Html() => Content(
            "<!DOCTYPE html>\n" +
            "<html lang=\"en\">\n" +
            "<head><meta charset=\"utf-8\"><title>APIBee Sample Page</title></head>\n" +
            "<body>\n" +
            "  <h1 id=\"title\">APIBee Sample Page</h1>\n" +
            "  <p class=\"intro\">Hardcoded HTML for testing HTTP clients and HTML parsers.</p>\n" +
            "  <ul id=\"users\">\n" +
            "    <li data-id=\"1\">Alice Johnson</li>\n" +
            "    <li data-id=\"2\">Bob Smith</li>\n" +
            "    <li data-id=\"3\">Carol White</li>\n" +
            "  </ul>\n" +
            "  <table id=\"prices\"><tr><th>Item</th><th>Price</th></tr><tr><td>Mug</td><td>12.99</td></tr><tr><td>Lamp</td><td>29.99</td></tr></table>\n" +
            "  <form action=\"/api/echo\" method=\"post\"><input name=\"q\" value=\"bee\"><button type=\"submit\">Send</button></form>\n" +
            "</body>\n" +
            "</html>\n",
            "text/html; charset=utf-8");

        /// <summary>Return a sample Markdown document with a table and a code block.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("markdown")]
        public IActionResult Markdown() => Content(
            "# APIBee Sample\n\n" +
            "Hardcoded **Markdown** for testing.\n\n" +
            "## Users\n\n" +
            "| id | name | active |\n" +
            "|----|------|--------|\n" +
            "| 1 | Alice Johnson | yes |\n" +
            "| 2 | Bob Smith | no |\n\n" +
            "- item one\n- item two\n\n" +
            "```json\n{ \"hello\": \"world\" }\n```\n",
            "text/markdown; charset=utf-8");

        /// <summary>Return a small runnable JavaScript snippet.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("javascript")]
        public IActionResult JavaScript() => Content(
            "// APIBee sample script\n" +
            "const users = [{ id: 1, name: 'Alice' }, { id: 2, name: 'Bob' }];\n" +
            "function greet(user) { return `Hello, ${user.name}!`; }\n" +
            "console.log(users.map(greet).join('\\n'));\n",
            "text/javascript; charset=utf-8");

        /// <summary>Return a sample CSS stylesheet.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("css")]
        public IActionResult Css() => Content(
            "/* APIBee sample stylesheet */\n" +
            ":root { --bee-yellow: #ffc107; --bee-black: #1e1e1e; }\n" +
            "body { font-family: system-ui, sans-serif; background: var(--bee-black); color: var(--bee-yellow); }\n" +
            "h1 { font-size: 2rem; margin: 0 0 1rem; }\n",
            "text/css; charset=utf-8");

        // -------------------- FEEDS / CALENDAR --------------------
        /// <summary>Return a sample RSS 2.0 feed with two items.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("rss")]
        public IActionResult Rss() => Content(
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            "<rss version=\"2.0\">\n" +
            "  <channel>\n" +
            "    <title>APIBee Blog</title>\n" +
            "    <link>https://example.com/blog</link>\n" +
            "    <description>Hardcoded RSS feed for testing.</description>\n" +
            "    <item><title>Getting started with APIBee</title><link>https://example.com/blog/1</link><guid>apibee-post-1</guid><pubDate>Tue, 01 Jul 2025 10:00:00 GMT</pubDate><description>Learn the basics.</description></item>\n" +
            "    <item><title>Testing pagination</title><link>https://example.com/blog/2</link><guid>apibee-post-2</guid><pubDate>Mon, 07 Jul 2025 09:30:00 GMT</pubDate><description>Limit, page and offset.</description></item>\n" +
            "  </channel>\n" +
            "</rss>\n",
            "application/rss+xml; charset=utf-8");

        /// <summary>Return a sample Atom feed with two entries.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("atom")]
        public IActionResult Atom() => Content(
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            "<feed xmlns=\"http://www.w3.org/2005/Atom\">\n" +
            "  <title>APIBee Blog</title>\n" +
            "  <id>urn:uuid:7c0e5f5a-1b1e-4a53-9a47-3d3c2f5e8a01</id>\n" +
            "  <updated>2025-07-07T09:30:00Z</updated>\n" +
            "  <link href=\"https://example.com/blog\"/>\n" +
            "  <entry><title>Getting started with APIBee</title><id>urn:apibee:post:1</id><updated>2025-07-01T10:00:00Z</updated><link href=\"https://example.com/blog/1\"/><summary>Learn the basics.</summary></entry>\n" +
            "  <entry><title>Testing pagination</title><id>urn:apibee:post:2</id><updated>2025-07-07T09:30:00Z</updated><link href=\"https://example.com/blog/2\"/><summary>Limit, page and offset.</summary></entry>\n" +
            "</feed>\n",
            "application/atom+xml; charset=utf-8");

        /// <summary>Return a sample iCalendar (<c>.ics</c>) event with CRLF line endings.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("ical")]
        public IActionResult ICal() => Content(
            "BEGIN:VCALENDAR\r\n" +
            "VERSION:2.0\r\n" +
            "PRODID:-//APIBee//Sample Calendar//EN\r\n" +
            "BEGIN:VEVENT\r\n" +
            "UID:apibee-event-1@example.com\r\n" +
            "DTSTAMP:20250701T100000Z\r\n" +
            "DTSTART:20250715T090000Z\r\n" +
            "DTEND:20250715T100000Z\r\n" +
            "SUMMARY:APIBee Sprint Planning\r\n" +
            "LOCATION:Conference Room A\r\n" +
            "END:VEVENT\r\n" +
            "END:VCALENDAR\r\n",
            "text/calendar; charset=utf-8");

        // -------------------- IMAGES / DOCUMENTS --------------------
        /// <summary>Return a sample SVG image.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("svg")]
        public IActionResult Svg() => Content(
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"200\" viewBox=\"0 0 200 200\">\n" +
            "  <rect width=\"200\" height=\"200\" fill=\"#1e1e1e\"/>\n" +
            "  <polygon points=\"100,20 170,60 170,140 100,180 30,140 30,60\" fill=\"#ffc107\"/>\n" +
            "  <text x=\"100\" y=\"110\" font-family=\"sans-serif\" font-size=\"28\" text-anchor=\"middle\" fill=\"#1e1e1e\">APIBee</text>\n" +
            "</svg>\n",
            "image/svg+xml; charset=utf-8");

        /// <summary>Generate a real, valid PNG image.</summary>
        /// <param name="size">Width and height in pixels. Default 128; values outside 1–512 are clamped, not rejected.</param>
        /// <param name="pattern"><c>gradient</c> (default) or <c>checkerboard</c>. Case-insensitive; unknown values fall back to <c>gradient</c>.</param>
        /// <response code="200">An <c>image/png</c> body.</response>
        /// <response code="400"><c>size</c> is not an integer.</response>
        [HttpGet("png")]
        public IActionResult Png([FromQuery] int size = 128, [FromQuery] string pattern = "gradient") =>
            File(SampleContent.Png(size, pattern), "image/png");

        /// <summary>Return a 1×1 transparent GIF89a image.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("gif")]
        public IActionResult Gif() => File(SampleContent.Gif, "image/gif");

        /// <summary>Return a minimal valid single-page PDF.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("pdf")]
        public IActionResult Pdf() => File(SampleContent.Pdf(), "application/pdf");

        // -------------------- JSONP --------------------
        private static readonly Regex CallbackPattern = new(@"^[A-Za-z_$][\w$]*(\.[A-Za-z_$][\w$]*)*$", RegexOptions.Compiled);

        /// <summary>Wrap the sample JSON in a JSONP callback.</summary>
        /// <param name="callback">JavaScript function name: an identifier, dots allowed, max 64 chars. Default <c>callback</c>; an empty value also uses the default.</param>
        /// <remarks>Served as <c>application/javascript</c>. The body is prefixed with <c>/**/</c> to guard against content-sniffing attacks.</remarks>
        /// <response code="200">Success.</response>
        /// <response code="400"><c>callback</c> is not a valid identifier or is too long.</response>
        [HttpGet("jsonp")]
        public IActionResult Jsonp([FromQuery] string callback = "callback")
        {
            if (callback.Length > 64 || !CallbackPattern.IsMatch(callback))
                return BadRequest(ApiResponse.Error(400, "Invalid callback: must be a JavaScript identifier (dots allowed), max 64 chars."));

            return Content($"/**/{callback}({SampleJson});", "application/javascript; charset=utf-8");
        }

        // -------------------- CONTENT NEGOTIATION --------------------
        private static readonly string[] Negotiable = { "application/json", "application/xml", "text/xml", "text/html", "text/plain", "text/csv" };

        /// <summary>Return the same user in the format chosen by the <c>Accept</c> header.</summary>
        /// <remarks>
        /// Supported media types: <c>application/json</c>, <c>application/xml</c>, <c>text/xml</c>, <c>text/html</c>, <c>text/plain</c> and <c>text/csv</c>.
        /// Types are tried in descending q-value order and <c>q=0</c> excludes a type. <c>*/*</c> and <c>application/*</c> map to JSON,
        /// <c>text/*</c> maps to plain text, and a missing <c>Accept</c> header yields JSON. Every response carries <c>Vary: Accept</c>.
        /// </remarks>
        /// <response code="200">The user in the negotiated format.</response>
        /// <response code="406">No acceptable media type; the body lists the supported ones.</response>
        [HttpGet("negotiate")]
        public IActionResult Negotiate()
        {
            var accept = Request.Headers.Accept.ToString();
            string? chosen = null;

            if (string.IsNullOrWhiteSpace(accept))
            {
                chosen = "application/json";
            }
            else if (MediaTypeHeaderValue.TryParseList(Request.Headers.Accept.ToArray()!, out var parsed))
            {
                foreach (var mt in parsed.Where(m => (m.Quality ?? 1) > 0).OrderByDescending(m => m.Quality ?? 1))
                {
                    var type = mt.MediaType.ToString().ToLowerInvariant();
                    chosen = type switch
                    {
                        "*/*" or "application/*" => "application/json",
                        "text/*" => "text/plain",
                        _ when Negotiable.Contains(type) => type,
                        _ => null
                    };
                    if (chosen != null) break;
                }
            }

            Response.Headers["Vary"] = "Accept";

            return chosen switch
            {
                "application/json" => Content(SampleJson, "application/json; charset=utf-8"),
                "application/xml" or "text/xml" => Content(
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><user><id>1</id><name>Alice Johnson</name><email>alice@example.com</email><active>true</active></user>",
                    chosen + "; charset=utf-8"),
                "text/html" => Content("<!DOCTYPE html><html><body><h1>Alice Johnson</h1><p>alice@example.com</p></body></html>", "text/html; charset=utf-8"),
                "text/plain" => Content("id=1\nname=Alice Johnson\nemail=alice@example.com\nactive=true\n", "text/plain; charset=utf-8"),
                "text/csv" => Content("id,name,email,active\n1,Alice Johnson,alice@example.com,true\n", "text/csv; charset=utf-8"),
                _ => StatusCode(406, new
                {
                    status = 406,
                    error = "Not Acceptable",
                    message = $"None of the requested media types are supported: '{accept}'.",
                    supported = Negotiable
                })
            };
        }

        // -------------------- PRE-COMPRESSED BODIES --------------------
        /// <summary>Return a gzip-compressed JSON body.</summary>
        /// <remarks>Always compressed and sent with <c>Content-Encoding: gzip</c>, whatever the request's <c>Accept-Encoding</c> says.</remarks>
        /// <response code="200">Success.</response>
        [HttpGet("gzip")]
        public IActionResult Gzip() => Compressed("gzip", s => new GZipStream(s, CompressionLevel.Optimal, leaveOpen: true));

        // HTTP "deflate" is the zlib format (RFC 1950), not raw deflate.
        /// <summary>Return a deflate-compressed JSON body.</summary>
        /// <remarks>Uses the zlib wrapper (RFC 1950), as HTTP's <c>deflate</c> coding requires. Always compressed, whatever the request's <c>Accept-Encoding</c> says.</remarks>
        /// <response code="200">Success.</response>
        [HttpGet("deflate")]
        public IActionResult Deflate() => Compressed("deflate", s => new ZLibStream(s, CompressionLevel.Optimal, leaveOpen: true));

        /// <summary>Return a Brotli-compressed JSON body.</summary>
        /// <remarks>Always compressed and sent with <c>Content-Encoding: br</c>, whatever the request's <c>Accept-Encoding</c> says.</remarks>
        /// <response code="200">Success.</response>
        [HttpGet("brotli")]
        public IActionResult Brotli() => Compressed("br", s => new BrotliStream(s, CompressionLevel.Optimal, leaveOpen: true));

        private IActionResult Compressed(string encoding, Func<Stream, Stream> wrap)
        {
            var body = "{\"compressed\":true,\"encoding\":\"" + encoding + "\",\"message\":\"If you can read this, your client decompressed the body.\",\"data\":" + SampleJson + "}";
            using var ms = new MemoryStream();
            using (var z = wrap(ms))
                z.Write(Encoding.UTF8.GetBytes(body));

            Response.Headers["Content-Encoding"] = encoding;
            Response.Headers["Vary"] = "Accept-Encoding";
            return File(ms.ToArray(), "application/json; charset=utf-8");
        }

        // -------------------- EMPTY BODIES --------------------
        /// <summary>Return 200 OK with a zero-length body and no Content-Type.</summary>
        /// <response code="200">Empty body.</response>
        [HttpGet("empty")]
        public IActionResult EmptyBody() => new EmptyResult();

        /// <summary>Return 204 No Content.</summary>
        /// <response code="204">No body.</response>
        [HttpGet("no-content")]
        public IActionResult NoBody() => NoContent();

        // -------------------- BROKEN PAYLOADS (negative testing) --------------------
        /// <summary>Return deliberately invalid JSON labelled <c>application/json</c>.</summary>
        /// <remarks>For negative testing: the body has a trailing comma and a truncated literal, so every JSON parser must fail on it.</remarks>
        /// <response code="200">Success.</response>
        [HttpGet("malformed-json")]
        public IActionResult MalformedJson() =>
            Content("{\"id\": 1, \"name\": \"Alice\", \"tags\": [\"a\", \"b\",], \"active\": tru", "application/json; charset=utf-8");

        /// <summary>Return valid JSON mislabelled as <c>text/html</c>.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("wrong-content-type")]
        public IActionResult WrongContentType() => Content(SampleJson, "text/html; charset=utf-8");

        /// <summary>Return JSON prefixed with a UTF-8 byte-order mark.</summary>
        /// <remarks>Strict JSON parsers reject the body unless the client strips the BOM (<c>EF BB BF</c>) first.</remarks>
        /// <response code="200">Success.</response>
        [HttpGet("bom")]
        public IActionResult Bom()
        {
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(SampleJson)).ToArray();
            return File(bytes, "application/json; charset=utf-8");
        }

        // -------------------- CHARSETS --------------------
        /// <summary>Return text encoded in a non-UTF-8 charset.</summary>
        /// <param name="name"><c>latin1</c> (alias <c>iso-8859-1</c>) returns ISO-8859-1 plain text; <c>utf16</c> (alias <c>utf-16</c>) returns UTF-16LE JSON with a BOM. Case-insensitive.</param>
        /// <response code="200">Success.</response>
        /// <response code="404">Unknown charset.</response>
        [HttpGet("charset/{name}")]
        public IActionResult Charset(string name)
        {
            const string text = "Café déjà vu — naïve façade, Straße, Ærø, ¡Hola!";

            switch (name.ToLowerInvariant())
            {
                case "latin1":
                case "iso-8859-1":
                    // Latin-1 can't encode the em dash; .NET substitutes a best-fit '-', which is itself a useful test.
                    return File(Encoding.Latin1.GetBytes(text + "\n"), "text/plain; charset=iso-8859-1");
                case "utf16":
                case "utf-16":
                    var json = "{\"text\":\"" + text + "\",\"charset\":\"utf-16\"}";
                    var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(json)).ToArray();
                    return File(bytes, "application/json; charset=utf-16");
                default:
                    return NotFound(ApiResponse.Error(404, $"Unknown charset '{name}'. Supported: latin1, utf16."));
            }
        }
    }
}
