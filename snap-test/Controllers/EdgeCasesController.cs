using System.Text;
using Microsoft.AspNetCore.Mvc;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// JSON edge cases for stress-testing parsers, assertions and UI rendering. Most bodies are raw,
    /// hand-written JSON text so the exact bytes (literal emoji, -0, huge numbers, duplicate keys) survive.
    /// </summary>
    [ApiController]
    [Route("api/edge-cases")]
    public class EdgeCasesController : ControllerBase
    {
        private const int MaxDepth = 500;
        private const int MaxKeys = 5000;
        private const int MaxArray = 10000;

        private ContentResult Raw(string json) => Content(json, "application/json; charset=utf-8");

        // -------------------- INDEX --------------------
        /// <summary>List the available edge-case endpoints.</summary>
        /// <response code="200">Success.</response>
        [HttpGet]
        public IActionResult Index() => Ok(new
        {
            endpoints = new[]
            {
                "types", "numbers", "strings", "dates", "nulls", "empty-object", "empty-array", "mixed-array",
                "special-keys", "duplicate-keys", "deep?depth=100", "wide?keys=1000", "large-array?count=1000",
                "top-level/string", "top-level/number", "top-level/boolean", "top-level/null", "top-level/array"
            }
        });

        // -------------------- EVERY JSON TYPE --------------------
        /// <summary>Return one of every JSON value type, including empty and look-alike values.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("types")]
        public IActionResult Types() => Raw("""
            {
              "string": "hello",
              "emptyString": "",
              "integer": 42,
              "negativeInteger": -7,
              "float": 3.14159,
              "exponent": 6.022e23,
              "booleanTrue": true,
              "booleanFalse": false,
              "nullValue": null,
              "array": [1, 2, 3],
              "emptyArray": [],
              "object": { "key": "value" },
              "emptyObject": {},
              "arrayOfObjects": [ { "id": 1, "name": "Alice" }, { "id": 2, "name": "Bob" } ],
              "nestedArrays": [[1, 2], [3, [4, [5]]]],
              "stringThatLooksLikeNumber": "123",
              "stringThatLooksLikeBoolean": "true",
              "stringThatLooksLikeNull": "null"
            }
            """);

        // -------------------- NUMBERS --------------------
        /// <summary>Return numeric edge cases: 64-bit limits, unsafe integers, -0 and float precision.</summary>
        /// <remarks>Integers beyond 2^53 lose precision in JavaScript; string versions are included for comparison.</remarks>
        /// <response code="200">Success.</response>
        [HttpGet("numbers")]
        public IActionResult Numbers() => Raw("""
            {
              "zero": 0,
              "negativeZero": -0,
              "int32Max": 2147483647,
              "int32Min": -2147483648,
              "int64Max": 9223372036854775807,
              "int64MaxAsString": "9223372036854775807",
              "int64Min": -9223372036854775808,
              "maxSafeInteger": 9007199254740991,
              "beyondSafeInteger": 9007199254740993,
              "beyondSafeIntegerAsString": "9007199254740993",
              "bigInteger": 123456789012345678901234567890,
              "pointOnePlusPointTwo": 0.30000000000000004,
              "manyDecimals": 0.1234567890123456789012345,
              "money": 19.99,
              "moneyAsString": "19.99",
              "trailingZeros": 1.50,
              "huge": 1e308,
              "tiny": 5e-324,
              "exponentUpper": 1.5E+10,
              "exponentNegative": 2.5e-8,
              "negativeFloat": -273.15,
              "numberAsString": "42",
              "nanAsString": "NaN",
              "infinityAsString": "Infinity"
            }
            """);

        // -------------------- STRINGS --------------------
        /// <summary>Return string edge cases: unicode, emoji, RTL, zero-width, escapes, injection-looking text and a ~10 KB string.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("strings")]
        public IActionResult Strings()
        {
            var longString = string.Concat(Enumerable.Repeat("TestingAPIs ", 1500)).TrimEnd(); // ~10.5 KB
            var json = new StringBuilder();
            json.Append("{\n");
            json.Append("  \"empty\": \"\",\n");
            json.Append("  \"whitespace\": \"   \",\n");
            json.Append("  \"leadingTrailingSpaces\": \"  padded  \",\n");
            json.Append("  \"unicode\": \"Café, naïve, Straße, Ærø, Ελληνικά, Русский, 中文, 日本語, 한국어, हिन्दी\",\n");
            json.Append("  \"emoji\": \"🐝🚀🔥👍🏽👨‍👩‍👧‍👦🏳️‍🌈\",\n");
            json.Append("  \"rtl\": \"مرحبا بالعالم — שלום עולם\",\n");
            json.Append("  \"zeroWidth\": \"zero​width‌joiner‍here﻿\",\n");
            json.Append("  \"combiningCharacters\": \"é vs é\",\n");
            json.Append("  \"escapes\": \"quote:\\\" backslash:\\\\ slash:\\/ newline:\\n tab:\\t cr:\\r backspace:\\b formfeed:\\f\",\n");
            json.Append("  \"unicodeEscapes\": \"\\u0041\\u00e9\\u4e2d\\ud83d\\udc1d\",\n");
            json.Append("  \"controlCharacter\": \"bell:\\u0007 null:\\u0000 end\",\n");
            json.Append("  \"multiline\": \"line one\\nline two\\nline three\",\n");
            json.Append("  \"html\": \"<script>alert('xss')</script><b>bold</b> &amp; &lt;tag&gt;\",\n");
            json.Append("  \"sqlInjection\": \"Robert'); DROP TABLE Students;--\",\n");
            json.Append("  \"pathTraversal\": \"../../etc/passwd\",\n");
            json.Append("  \"template\": \"{{user.name}} ${env.HOME} <%= secret %>\",\n");
            json.Append("  \"url\": \"https://example.com/search?q=bee&lang=en#results\",\n");
            json.Append("  \"email\": \"alice+test@example.com\",\n");
            json.Append("  \"base64\": \"VGVzdGluZ0FQSXMgcm9ja3M=\",\n");
            json.Append("  \"longStringLength\": ").Append(longString.Length).Append(",\n");
            json.Append("  \"longString\": \"").Append(longString).Append("\"\n");
            json.Append("}\n");
            return Raw(json.ToString());
        }

        // -------------------- DATES --------------------
        /// <summary>Return one instant in many date formats, plus invalid and boundary dates.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("dates")]
        public IActionResult Dates() => Raw("""
            {
              "iso8601Utc": "2025-07-15T09:30:00Z",
              "iso8601Millis": "2025-07-15T09:30:00.123Z",
              "iso8601Micros": "2025-07-15T09:30:00.123456Z",
              "iso8601PositiveOffset": "2025-07-15T15:00:00+05:30",
              "iso8601NegativeOffset": "2025-07-15T05:30:00-04:00",
              "iso8601NoZone": "2025-07-15T09:30:00",
              "dateOnly": "2025-07-15",
              "timeOnly": "09:30:00",
              "weekDate": "2025-W29-2",
              "ordinalDate": "2025-196",
              "unixSeconds": 1752571800,
              "unixMilliseconds": 1752571800000,
              "unixSecondsAsString": "1752571800",
              "rfc1123": "Tue, 15 Jul 2025 09:30:00 GMT",
              "rfc2822": "Tue, 15 Jul 2025 09:30:00 +0000",
              "usFormat": "07/15/2025",
              "euFormat": "15/07/2025",
              "leapDay": "2024-02-29T00:00:00Z",
              "invalidLeapDay": "2025-02-29T00:00:00Z",
              "endOfYear": "2025-12-31T23:59:59.999Z",
              "leapSecond": "2016-12-31T23:59:60Z",
              "epoch": "1970-01-01T00:00:00Z",
              "beforeEpoch": "1969-07-20T20:17:40Z",
              "year2038": "2038-01-19T03:14:08Z",
              "farFuture": "9999-12-31T23:59:59Z",
              "isoDuration": "P1Y2M10DT2H30M",
              "nullDate": null,
              "emptyDate": ""
            }
            """);

        // -------------------- NULLS / EMPTIES --------------------
        /// <summary>Return nulls alongside null-like values such as <c>"null"</c>, empty strings, 0 and false.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("nulls")]
        public IActionResult Nulls() => Raw("""
            {
              "nullValue": null,
              "nestedNull": { "a": null, "b": { "c": null } },
              "arrayOfNulls": [null, null, null],
              "arrayWithNull": [1, null, 3],
              "nullAsString": "null",
              "undefinedAsString": "undefined",
              "noneAsString": "None",
              "emptyString": "",
              "emptyArray": [],
              "emptyObject": {},
              "zero": 0,
              "false": false
            }
            """);

        /// <summary>Return exactly <c>{}</c>.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("empty-object")]
        public IActionResult EmptyObject() => Raw("{}");

        /// <summary>Return exactly <c>[]</c>.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("empty-array")]
        public IActionResult EmptyArray() => Raw("[]");

        /// <summary>Return an array that mixes every JSON type.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("mixed-array")]
        public IActionResult MixedArray() => Raw("""[1, "two", 3.0, true, false, null, { "six": 6 }, [7, 8], "", -0, 1e3]""");

        // -------------------- KEYS --------------------
        /// <summary>Return an object with awkward keys: empty, dotted, <c>__proto__</c>, quoted, emoji and case variants.</summary>
        /// <response code="200">Success.</response>
        [HttpGet("special-keys")]
        public IActionResult SpecialKeys() => Raw("""
            {
              "": "empty key",
              " ": "space key",
              "key with spaces": 1,
              "key.with.dots": 2,
              "key-with-dashes": 3,
              "key_with_underscores": 4,
              "key/with/slashes": 5,
              "key[0]": 6,
              "123": "numeric key",
              "0": "zero key",
              "$ref": "#/definitions/user",
              "@type": "Person",
              "__proto__": { "polluted": true },
              "constructor": "prototype-pollution probe",
              "toString": "shadowed",
              "key\"with\"quotes": 7,
              "key\\with\\backslashes": 8,
              "ключ": "cyrillic key",
              "キー": "japanese key",
              "🐝": "emoji key",
              "UPPER": "case 1",
              "upper": "case 2",
              "camelCase": 9,
              "PascalCase": 10,
              "snake_case": 11
            }
            """);

        /// <summary>Return raw JSON text that contains duplicate keys.</summary>
        /// <remarks>Most parsers keep the last value for a repeated key; strict parsers reject the document.</remarks>
        /// <response code="200">Success.</response>
        [HttpGet("duplicate-keys")]
        public IActionResult DuplicateKeys() => Raw("""
            {
              "id": 1,
              "name": "first value",
              "name": "second value",
              "nested": { "flag": true, "flag": false },
              "id": 2
            }
            """);

        // -------------------- SIZE / SHAPE STRESS --------------------
        /// <summary>Return an object nested to the requested depth.</summary>
        /// <param name="depth">Nesting depth, 1–500, default 100. Many serializers stop at a default maximum depth of 64.</param>
        /// <response code="200">Success.</response>
        /// <response code="400"><c>depth</c> is outside 1–500 or not an integer.</response>
        [HttpGet("deep")]
        public IActionResult Deep([FromQuery] int depth = 100)
        {
            if (depth < 1 || depth > MaxDepth)
                return BadRequest(ApiResponse.Error(400, $"depth must be between 1 and {MaxDepth}."));

            var sb = new StringBuilder();
            for (var i = 1; i <= depth; i++)
                sb.Append("{\"level\":").Append(i).Append(i == depth ? ",\"leaf\":true" : ",\"child\":");
            sb.Append('}', depth);
            return Raw(sb.ToString());
        }

        /// <summary>Return an object with many keys.</summary>
        /// <param name="keys">Number of keys, 1–5000, default 1000.</param>
        /// <response code="200">Success.</response>
        /// <response code="400"><c>keys</c> is outside 1–5000.</response>
        [HttpGet("wide")]
        public IActionResult Wide([FromQuery] int keys = 1000)
        {
            if (keys < 1 || keys > MaxKeys)
                return BadRequest(ApiResponse.Error(400, $"keys must be between 1 and {MaxKeys}."));

            var sb = new StringBuilder("{");
            for (var i = 0; i < keys; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("\"key").Append(i).Append("\":").Append(i);
            }
            sb.Append('}');
            return Raw(sb.ToString());
        }

        /// <summary>Return a large array of objects.</summary>
        /// <param name="count">Number of items, 0–10000, default 1000. Also sent in the <c>X-Total-Count</c> header.</param>
        /// <response code="200">Success.</response>
        /// <response code="400"><c>count</c> is outside 0–10000.</response>
        [HttpGet("large-array")]
        public IActionResult LargeArray([FromQuery] int count = 1000)
        {
            if (count < 0 || count > MaxArray)
                return BadRequest(ApiResponse.Error(400, $"count must be between 0 and {MaxArray}."));

            string[] names = { "Alice", "Bob", "Carol", "David", "Eve", "Frank", "Grace", "Heidi" };
            string[] statuses = { "active", "inactive", "pending" };

            var items = Enumerable.Range(1, count).Select(i => new
            {
                id = i,
                name = $"{names[i % names.Length]} #{i}",
                status = statuses[i % statuses.Length],
                score = Math.Round((i * 37 % 1000) / 10.0, 1),
                active = i % 3 != 0
            });

            Response.Headers["X-Total-Count"] = count.ToString();
            return Ok(items);
        }

        // -------------------- TOP-LEVEL SCALARS --------------------
        /// <summary>Return a bare JSON scalar or array as the whole body.</summary>
        /// <param name="kind"><c>string</c>, <c>number</c>, <c>boolean</c>, <c>null</c> or <c>array</c>. Case-insensitive.</param>
        /// <response code="200">Success.</response>
        /// <response code="404">Unknown kind.</response>
        [HttpGet("top-level/{kind}")]
        public IActionResult TopLevel(string kind) => kind.ToLowerInvariant() switch
        {
            "string" => Raw("\"just a string\""),
            "number" => Raw("42"),
            "boolean" => Raw("true"),
            "null" => Raw("null"),
            "array" => Raw("[1,2,3]"),
            _ => NotFound(ApiResponse.Error(404, $"Unknown kind '{kind}'. Supported: string, number, boolean, null, array."))
        };
    }
}
