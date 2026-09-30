using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace snap_test.Swagger
{
    /// <summary>
    /// Many test endpoints read headers or the raw request body by hand (no [FromHeader]/[FromBody]),
    /// so the generator can't see them. This filter documents those inputs so "Try it out" can send them.
    /// Routes are matched on the normalised template: lower-case, no leading slash, constraints and ** removed.
    /// </summary>
    public class RequestHintsOperationFilter : IOperationFilter
    {
        private record HeaderHint(string Method, string Path, string Name, bool Required, string Description, string? Example = null);

        private record BodyHint(string Method, string Path, string ContentType, OpenApiSchema Schema, string Description, JsonNode? Example = null);

        private const string AnyWrite = "*"; // POST, PUT and PATCH

        private static readonly HeaderHint[] Headers =
        {
            new("POST", "api/payments", "Idempotency-Key", true, "Unique key per logical payment. Same key + same body replays the stored response; a different body returns 422.", "3f2b9c1e-7d4a-4e8b-9a61-0c5d2e8f1a7b"),
            new("GET", "api/ratelimit", "X-Client-Id", false, "Rate-limit bucket id (defaults to your IP). Use a unique value to get a fresh window.", "swagger-demo"),
            new("GET", "api/ratelimit/status", "X-Client-Id", false, "Rate-limit bucket id (defaults to your IP).", "swagger-demo"),
            new("POST", "api/ratelimit/reset", "X-Client-Id", false, "Rate-limit bucket id (defaults to your IP).", "swagger-demo"),
            new("GET", "api/versioned/profile", "X-API-Version", false, "API version to use (1 or 2). Takes precedence over ?api-version= and the Accept media type.", "2"),
            new("POST", "api/auth/hmac", "X-Timestamp", true, "Unix time in seconds; must be within 5 minutes of the server clock."),
            new("POST", "api/auth/hmac", "X-Signature", true, "Hex HMAC-SHA256 of \"{X-Timestamp}.{raw body}\" with secret `apibee-hmac-secret` (optional `sha256=` prefix)."),
            new("POST", "api/auth/csrf/submit", "X-CSRF-Token", true, "Token from GET /api/auth/csrf/token (the matching cookie is sent automatically by the browser)."),
            new("GET", "api/cache/etag", "If-None-Match", false, "ETag from a previous response; a match returns 304 Not Modified."),
            new("PUT", "api/cache/etag", "If-Match", false, "Current ETag. Missing returns 428, stale returns 412."),
            new("GET", "api/cache/last-modified", "If-Modified-Since", false, "HTTP date; returns 304 when the resource hasn't changed since.", "Wed, 01 Jan 2025 00:00:00 GMT"),
            new("GET", "api/cache/vary", "Accept-Language", false, "en, es, fr, de or hi.", "fr"),
            new("GET", "api/files/range/{n}", "Range", false, "Byte range, e.g. bytes=0-99. Returns 206, or 416 when unsatisfiable.", "bytes=0-99"),
            new("GET", "api/stream/sse", "Last-Event-ID", false, "Resume the stream after this event id.", "3"),
            new("POST", "api/soap/calculator", "SOAPAction", false, "SOAP 1.1 action, e.g. \"http://tempuri.org/Add\". Not used for SOAP 1.2.", "\"http://tempuri.org/Add\""),
        };

        private static readonly BodyHint[] Bodies =
        {
            new(AnyWrite, "api/echo", "application/json", AnyJson(), "Any body; it is echoed back.", JsonNode.Parse("""{"hello":"world","n":1}""")),
            new(AnyWrite, "api/echo", "text/plain", Text(), "Any body; it is echoed back."),
            new(AnyWrite, "api/echo/{path}", "application/json", AnyJson(), "Any body; it is echoed back.", JsonNode.Parse("""{"hello":"world"}""")),
            new(AnyWrite, "api/methods/post", "application/json", AnyJson(), "Optional body; described in the response.", JsonNode.Parse("""{"a":1}""")),
            new(AnyWrite, "api/methods/put", "application/json", AnyJson(), "Optional body; described in the response.", JsonNode.Parse("""{"a":1}""")),
            new(AnyWrite, "api/methods/patch", "application/json", AnyJson(), "Optional body; described in the response.", JsonNode.Parse("""{"a":1}""")),
            new(AnyWrite, "api/methods/any", "application/json", AnyJson(), "Optional body; described in the response."),
            new("POST", "api/bodies/form", "application/x-www-form-urlencoded", FreeForm(), "Any form fields.", JsonNode.Parse("""{"name":"Ada","role":"admin"}""")),
            new("POST", "api/bodies/form", "multipart/form-data", MultipartWithFile(), "Any fields and files."),
            new("POST", "api/bodies/text", "text/plain", Text(), "Plain text; counts characters, lines and words.", JsonValue.Create("Hello\nworld")),
            new("POST", "api/bodies/xml", "application/xml", Text(), "Well-formed XML (DTDs are rejected).", JsonValue.Create("<order id=\"7\"><item sku=\"A1\">2</item></order>")),
            new("POST", "api/bodies/binary", "application/octet-stream", Binary(), "Raw bytes; returns size, hashes and detected type."),
            new(AnyWrite, "api/bodies/any", "application/octet-stream", Binary(), "Any body; the real type is detected and compared with Content-Type."),
            new(AnyWrite, "api/bodies/any", "application/json", AnyJson(), "Any body; the real type is detected and compared with Content-Type."),
            new("POST", "api/bodies/large", "application/octet-stream", Binary(), "Up to 10 MB; larger returns 413."),
            new("POST", "api/auth/hmac", "application/json", AnyJson(), "Any body; it is covered by X-Signature.", JsonNode.Parse("""{"event":"ping"}""")),
            new("POST", "api/auth/csrf/submit", "application/json", AnyJson(), "Any body.", JsonNode.Parse("""{"comment":"hello"}""")),
            new("POST", "api/auth/oauth/token", "application/x-www-form-urlencoded", Form("grant_type", "client_id", "client_secret", "username", "password", "code", "redirect_uri", "code_verifier", "refresh_token", "scope"),
                "Standard OAuth2 token request (JSON with the same fields also works).",
                JsonNode.Parse("""{"grant_type":"client_credentials","client_id":"apibee-client","client_secret":"apibee-secret","scope":"read"}""")),
            new("POST", "api/auth/oauth/introspect", "application/x-www-form-urlencoded", Form("token"), "Token to inspect."),
            new("POST", "api/auth/oauth/revoke", "application/x-www-form-urlencoded", Form("token", "token_type_hint"), "Token to revoke."),
            new("POST", "api/soap/calculator", "text/xml", Text(), "SOAP 1.1 envelope (send SOAPAction). SOAP 1.2 uses application/soap+xml.",
                JsonValue.Create("""<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"><soap:Body><Add xmlns="http://tempuri.org/"><intA>5</intA><intB>3</intB></Add></soap:Body></soap:Envelope>""")),
            new("POST", "api/soap/calculator", "application/soap+xml", Text(), "SOAP 1.2 envelope.",
                JsonValue.Create("""<soap:Envelope xmlns:soap="http://www.w3.org/2003/05/soap-envelope"><soap:Body><Multiply xmlns="http://tempuri.org/"><intA>6</intA><intB>7</intB></Multiply></soap:Body></soap:Envelope>""")),
            new("POST", "api/soap/countries", "text/xml", Text(), "SOAP 1.1 envelope for GetCountryInfo or ListCountries.",
                JsonValue.Create("""<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"><soap:Body><GetCountryInfo xmlns="http://apibee.dev/countries"><countryCode>JP</countryCode></GetCountryInfo></soap:Body></soap:Envelope>""")),
            new(AnyWrite, "api/webhooks/{binid}", "application/json", AnyJson(), "Any payload; it is captured in the bin.", JsonNode.Parse("""{"event":"order.created","id":42}""")),
            new(AnyWrite, "api/webhooks/{binid}/{rest}", "application/json", AnyJson(), "Any payload; it is captured in the bin."),
        };

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var method = context.ApiDescription.HttpMethod?.ToUpperInvariant() ?? string.Empty;
            var path = Normalise(context.ApiDescription.RelativePath);

            foreach (var h in Headers.Where(h => h.Method == method && h.Path == path))
            {
                operation.Parameters ??= new List<IOpenApiParameter>();
                if (operation.Parameters.Any(p => string.Equals(p.Name, h.Name, StringComparison.OrdinalIgnoreCase)))
                    continue;

                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = h.Name,
                    In = ParameterLocation.Header,
                    Required = h.Required,
                    Description = h.Description,
                    Schema = Text(),
                    Example = h.Example == null ? null : JsonValue.Create(h.Example)
                });
            }

            // Only fill in bodies the generator couldn't infer from [FromBody]/[FromForm].
            if (operation.RequestBody != null) return;

            var bodies = Bodies
                .Where(b => b.Path == path && (b.Method == method || (b.Method == AnyWrite && method is "POST" or "PUT" or "PATCH")))
                .ToList();
            if (bodies.Count == 0) return;

            var content = new Dictionary<string, OpenApiMediaType>();
            foreach (var b in bodies)
                content[b.ContentType] = new OpenApiMediaType { Schema = b.Schema, Example = b.Example?.DeepClone() };

            operation.RequestBody = new OpenApiRequestBody
            {
                Required = false,
                Description = bodies[0].Description,
                Content = content
            };
        }

        private static string Normalise(string? relativePath)
        {
            var p = (relativePath ?? string.Empty).Split('?')[0].Trim('/').ToLowerInvariant();
            p = p.Replace("{**", "{").Replace("{*", "{");
            return Regex.Replace(p, @"\{([^}:=?]+)[^}]*\}", "{$1}");
        }

        private static OpenApiSchema Text() => new() { Type = JsonSchemaType.String };

        private static OpenApiSchema Binary() => new() { Type = JsonSchemaType.String, Format = "binary" };

        private static OpenApiSchema AnyJson() => new() { Description = "Any JSON value" };

        private static OpenApiSchema FreeForm() => new() { Type = JsonSchemaType.Object, AdditionalPropertiesAllowed = true };

        private static OpenApiSchema MultipartWithFile() => new()
        {
            Type = JsonSchemaType.Object,
            AdditionalPropertiesAllowed = true,
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["file"] = Binary(),
                ["description"] = Text()
            }
        };

        private static OpenApiSchema Form(params string[] fields) => new()
        {
            Type = JsonSchemaType.Object,
            Properties = fields.ToDictionary(f => f, _ => (IOpenApiSchema)Text())
        };
    }
}
