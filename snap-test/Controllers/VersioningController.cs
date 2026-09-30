using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

namespace snap_test.Controllers
{
    /// <summary>
    /// API versioning in its common forms. The same profile comes back in two shapes:
    ///   v1 (deprecated: flat fields; sends Deprecation / Sunset / Link successor-version headers) and v2 (nested).
    /// Select the version by URL path (/api/v1, /api/v2) or, on /api/versioned/profile, by the X-API-Version
    /// header, the ?api-version= query, or Accept: application/vnd.apibee.v2+json (checked in that order).
    /// </summary>
    [ApiController]
    [Route("api")]
    public class VersioningController : ControllerBase
    {
        private static readonly string[] Supported = { "1", "2" };
        private const string Latest = "2";
        private static readonly Regex VendorAccept = new(@"application/vnd\.apibee\.v([^+;,\s]+)\+json", RegexOptions.IgnoreCase);

        // -------------------- SUPPORTED VERSIONS --------------------
        /// <summary>List supported API versions and how to select them.</summary>
        /// <response code="200">Current version, supported versions and the selection options.</response>
        [HttpGet("versions")]
        public IActionResult Versions() => Ok(new
        {
            current = Latest,
            supported = new object[]
            {
                new { version = "1", status = "deprecated", sunset = "2026-12-31T23:59:59Z", url = "/api/v1/profile" },
                new { version = "2", status = "current", sunset = (string?)null, url = "/api/v2/profile" }
            },
            negotiation = new
            {
                path = "/api/v{version}/profile",
                header = "X-API-Version: 2",
                query = "/api/versioned/profile?api-version=2",
                accept = "Accept: application/vnd.apibee.v2+json",
                precedence = "header > query > accept > default (latest)"
            }
        });

        // -------------------- PATH VERSIONING --------------------
        /// <summary>Get the profile in the deprecated v1 shape (selected by URL path).</summary>
        /// <remarks>
        /// v1 uses flat fields. Every response carries <c>Deprecation: @1751328000</c>,
        /// <c>Sunset: Thu, 31 Dec 2026 23:59:59 GMT</c> and <c>Link: &lt;/api/v2/profile&gt;; rel="successor-version"</c>.
        /// </remarks>
        /// <response code="200">v1 profile.</response>
        [HttpGet("v1/profile")]
        public IActionResult ProfileV1()
        {
            AddVersionHeaders("1");
            return Ok(V1Profile());
        }

        /// <summary>Get the profile in the current v2 shape (selected by URL path).</summary>
        /// <remarks>v2 uses nested contact, address and preferences objects.</remarks>
        /// <response code="200">v2 profile.</response>
        [HttpGet("v2/profile")]
        public IActionResult ProfileV2()
        {
            AddVersionHeaders("2");
            return Ok(V2Profile());
        }

        // -------------------- HEADER / QUERY / MEDIA-TYPE VERSIONING --------------------
        /// <summary>Get the profile in the version chosen by header, query string or media type.</summary>
        /// <param name="apiVersion">Version, sent as the <c>api-version</c> query parameter, e.g. <c>1</c>, <c>2</c>, <c>v2</c> or <c>2.0</c>.</param>
        /// <remarks>
        /// Version selectors, in order of precedence:
        /// <list type="number">
        /// <item><description>Header: <c>X-API-Version: 1</c></description></item>
        /// <item><description>Query: <c>?api-version=1</c></description></item>
        /// <item><description>Media type: <c>Accept: application/vnd.apibee.v1+json</c> (the response uses the same content type)</description></item>
        /// <item><description>Default: the latest version (2)</description></item>
        /// </list>
        /// The response headers <c>X-API-Version</c> and <c>X-API-Version-Source</c> show which version was chosen and how.
        /// </remarks>
        /// <response code="200">Profile in the requested version.</response>
        /// <response code="400">Unsupported version; the body lists supported versions.</response>
        [HttpGet("versioned/profile")]
        public IActionResult Versioned([FromQuery(Name = "api-version")] string? apiVersion = null)
        {
            string? requested = null;
            var source = "default";
            var accept = Request.Headers.Accept.ToString();
            var vendorMatch = VendorAccept.Match(accept);

            if (!string.IsNullOrWhiteSpace(Request.Headers["X-API-Version"]))
            {
                requested = Request.Headers["X-API-Version"].ToString();
                source = "header";
            }
            else if (!string.IsNullOrWhiteSpace(apiVersion))
            {
                requested = apiVersion;
                source = "query";
            }
            else if (vendorMatch.Success)
            {
                requested = vendorMatch.Groups[1].Value;
                source = "accept";
            }

            var version = requested == null ? Latest : Normalize(requested);
            if (!Supported.Contains(version))
            {
                return BadRequest(new
                {
                    status = 400,
                    error = "Bad Request",
                    message = $"API version '{requested}' is not supported.",
                    source,
                    supportedVersions = Supported
                });
            }

            AddVersionHeaders(version);
            Response.Headers["X-API-Version-Source"] = source;

            object body = version == "1" ? V1Profile() : V2Profile();
            return source == "accept"
                ? new ObjectResult(body) { ContentTypes = { $"application/vnd.apibee.v{version}+json" } }
                : Ok(body);
        }

        // "v2", "V2", "2.0" and "2" all mean version 2.
        private static string Normalize(string raw)
        {
            var value = raw.Trim().TrimStart('v', 'V');
            return value.EndsWith(".0") ? value[..^2] : value;
        }

        private void AddVersionHeaders(string version)
        {
            Response.Headers["X-API-Version"] = version;
            if (version != "1") return;

            Response.Headers["Deprecation"] = "@1751328000"; // RFC 9745: deprecated since 2025-07-01
            Response.Headers["Sunset"] = "Thu, 31 Dec 2026 23:59:59 GMT"; // RFC 8594
            Response.Headers["Link"] = "</api/v2/profile>; rel=\"successor-version\"";
        }

        private static object V1Profile() => new
        {
            id = 101,
            name = "Ava Thompson",
            email = "ava.thompson@example.com",
            phone = "+1-555-0101",
            address = "742 Evergreen Terrace, Springfield, IL 62704, USA",
            memberSince = "2023-04-12"
        };

        private static object V2Profile() => new
        {
            id = 101,
            firstName = "Ava",
            lastName = "Thompson",
            displayName = "Ava T.",
            contact = new
            {
                email = "ava.thompson@example.com",
                phone = new { countryCode = "+1", number = "555-0101" }
            },
            address = new
            {
                street = "742 Evergreen Terrace",
                city = "Springfield",
                state = "IL",
                postalCode = "62704",
                country = "US"
            },
            preferences = new { language = "en-US", timezone = "America/Chicago", newsletter = true },
            createdAt = "2023-04-12T09:30:00Z",
            apiVersion = "2"
        };
    }
}
