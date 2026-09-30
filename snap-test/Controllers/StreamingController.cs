using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace snap_test.Controllers
{
    /// <summary>
    /// Streaming responses: Server-Sent Events, NDJSON, chunked transfer, byte drip and a streamed JSON array.
    /// Response buffering is disabled and every event is flushed, so clients see data as it is produced.
    /// Every stream stops as soon as the client disconnects.
    /// </summary>
    [ApiController]
    [Route("api/stream")]
    public class StreamingController : ControllerBase
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private static readonly (string Symbol, string Name, decimal Base)[] Stocks =
        {
            ("AAPL", "Apple Inc.", 227.52m),
            ("MSFT", "Microsoft Corp.", 438.11m),
            ("GOOGL", "Alphabet Inc.", 176.34m),
            ("AMZN", "Amazon.com Inc.", 186.90m),
            ("TSLA", "Tesla Inc.", 248.23m)
        };

        private static readonly string[] ItemNames =
        {
            "Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel", "India", "Juliet"
        };

        /// <summary>
        /// Deterministic ticker: the same tick number always yields the same symbol and price.
        /// Shared with the /ws/ticker WebSocket.
        /// </summary>
        internal static object StockTick(int tick)
        {
            tick = Math.Max(tick, 1);
            var stock = Stocks[(tick - 1) % Stocks.Length];
            var change = Math.Round(stock.Base * (decimal)(Math.Sin(tick * 0.9) * 0.015), 2);

            return new
            {
                tick,
                symbol = stock.Symbol,
                name = stock.Name,
                price = stock.Base + change,
                change,
                changePercent = Math.Round(change / stock.Base * 100, 2),
                volume = 10_000 + (tick * 137 % 5_000),
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }

        // -------------------- SERVER-SENT EVENTS --------------------
        /// <summary>Stream Server-Sent Events.</summary>
        /// <param name="count">Number of events to send (1-100, default 10).</param>
        /// <param name="interval">Delay between events in milliseconds (100-5000, default 1000).</param>
        /// <remarks>
        /// The stream opens with <c>retry: 3000</c>; each event has an <c>id</c>, <c>event: message</c> and a JSON
        /// <c>data</c> line, and the stream ends with <c>event: done</c>. Send a <c>Last-Event-ID: N</c> header to resume
        /// after event N.
        /// <para>Swagger UI buffers the whole response and cannot show events as they arrive. Use curl instead:</para>
        /// <code>curl -N "http://localhost:5251/api/stream/sse?count=5&amp;interval=500"</code>
        /// </remarks>
        /// <response code="200">A <c>text/event-stream</c> of events.</response>
        [HttpGet("sse")]
        public async Task Sse([FromQuery] int count = 10, [FromQuery] int interval = 1000)
        {
            count = Math.Clamp(count, 1, 100);
            interval = Math.Clamp(interval, 100, 5000);

            // Resume support: a reconnecting EventSource sends the last id it received.
            var start = int.TryParse(Request.Headers["Last-Event-ID"], out var lastId) && lastId >= 0 ? lastId + 1 : 1;

            await StreamAsync("text/event-stream", async ct =>
            {
                await WriteAsync($": connected, streaming events {start}..{count}\nretry: 3000\n\n", ct);

                for (var i = start; i <= count; i++)
                {
                    var data = JsonSerializer.Serialize(new
                    {
                        id = i,
                        message = $"Event {i} of {count}",
                        timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    }, Json);

                    await WriteAsync($"id: {i}\nevent: message\ndata: {data}\n\n", ct);
                    if (i < count) await Task.Delay(interval, ct);
                }

                await WriteAsync($"event: done\ndata: {{\"total\":{count}}}\n\n", ct);
            });
        }

        // -------------------- SSE STOCK TICKER --------------------
        /// <summary>Stream a deterministic stock ticker as Server-Sent Events.</summary>
        /// <param name="count">Number of ticks to send (1-100, default 20).</param>
        /// <param name="interval">Delay between ticks in milliseconds (100-5000, default 1000).</param>
        /// <remarks>
        /// Each <c>event: tick</c> carries symbol, price, change, changePercent, volume and timestamp. The same tick number
        /// always yields the same symbol and price. The stream ends with <c>event: done</c>.
        /// <para>Swagger UI cannot display streams. Use curl instead:</para>
        /// <code>curl -N "http://localhost:5251/api/stream/sse/stock?count=5"</code>
        /// </remarks>
        /// <response code="200">A <c>text/event-stream</c> of ticks.</response>
        [HttpGet("sse/stock")]
        public async Task SseStock([FromQuery] int count = 20, [FromQuery] int interval = 1000)
        {
            count = Math.Clamp(count, 1, 100);
            interval = Math.Clamp(interval, 100, 5000);

            await StreamAsync("text/event-stream", async ct =>
            {
                await WriteAsync("retry: 3000\n\n", ct);

                for (var i = 1; i <= count; i++)
                {
                    await WriteAsync($"id: {i}\nevent: tick\ndata: {JsonSerializer.Serialize(StockTick(i), Json)}\n\n", ct);
                    if (i < count) await Task.Delay(interval, ct);
                }

                await WriteAsync($"event: done\ndata: {{\"ticks\":{count}}}\n\n", ct);
            });
        }

        // -------------------- NDJSON --------------------
        /// <summary>Stream newline-delimited JSON, one object per line.</summary>
        /// <param name="n">Number of lines (1-1000; out-of-range values are clamped).</param>
        /// <param name="intervalMs">Delay between lines in milliseconds (0-5000, default 0).</param>
        /// <remarks>
        /// Content type is <c>application/x-ndjson</c>. Swagger UI shows the result only after the stream ends; to watch
        /// lines arrive use <c>curl -N "http://localhost:5251/api/stream/ndjson/20?intervalMs=200"</c>.
        /// </remarks>
        /// <response code="200">NDJSON lines.</response>
        [HttpGet("ndjson/{n:int}")]
        public async Task NdJson(int n, [FromQuery] int intervalMs = 0)
        {
            n = Math.Clamp(n, 1, 1000);
            intervalMs = Math.Clamp(intervalMs, 0, 5000);

            await StreamAsync("application/x-ndjson", async ct =>
            {
                for (var i = 1; i <= n; i++)
                {
                    await WriteAsync(JsonSerializer.Serialize(Item(i), Json) + "\n", ct);
                    if (intervalMs > 0 && i < n) await Task.Delay(intervalMs, ct);
                }
            });
        }

        // -------------------- CHUNKED TRANSFER --------------------
        /// <summary>Send a plain-text response in chunked transfer encoding.</summary>
        /// <param name="n">Number of chunks (1-100; out-of-range values are clamped).</param>
        /// <param name="intervalMs">Delay between chunks in milliseconds (0-5000, default 250).</param>
        /// <remarks>
        /// No Content-Length is sent, so HTTP/1.1 uses <c>Transfer-Encoding: chunked</c>; each line is flushed as its own
        /// chunk. Watch it with <c>curl -N "http://localhost:5251/api/stream/chunked/5"</c>.
        /// </remarks>
        /// <response code="200">Chunked text, one line per chunk.</response>
        [HttpGet("chunked/{n:int}")]
        public async Task Chunked(int n, [FromQuery] int intervalMs = 250)
        {
            n = Math.Clamp(n, 1, 100);
            intervalMs = Math.Clamp(intervalMs, 0, 5000);

            // No Content-Length is set, so HTTP/1.1 uses Transfer-Encoding: chunked; one flush = one chunk.
            await StreamAsync("text/plain; charset=utf-8", async ct =>
            {
                for (var i = 1; i <= n; i++)
                {
                    await WriteAsync($"chunk {i}/{n} sent at {DateTime.UtcNow:HH:mm:ss.fff}\n", ct);
                    if (intervalMs > 0 && i < n) await Task.Delay(intervalMs, ct);
                }
            });
        }

        // -------------------- DRIP (bytes spread over a duration) --------------------
        /// <summary>Drip bytes slowly over a duration.</summary>
        /// <param name="durationMs">Total time to spread the bytes over, in milliseconds (0-30000, default 2000).</param>
        /// <param name="bytes">Number of <c>*</c> bytes to send (1-10240, default 10).</param>
        /// <remarks>
        /// Useful for testing read timeouts and progress handling. Content-Length is set to <paramref name="bytes"/>.
        /// Example: <c>curl -N "http://localhost:5251/api/stream/drip?durationMs=3000&amp;bytes=30"</c>
        /// </remarks>
        /// <response code="200"><c>application/octet-stream</c> body of asterisks.</response>
        [HttpGet("drip")]
        public async Task Drip([FromQuery] int durationMs = 2000, [FromQuery] int bytes = 10)
        {
            durationMs = Math.Clamp(durationMs, 0, 30_000);
            bytes = Math.Clamp(bytes, 1, 10_240);

            Response.ContentLength = bytes;

            await StreamAsync("application/octet-stream", async ct =>
            {
                // Timer resolution is ~15ms, so send whatever is "due" at each tick instead of one byte per sleep.
                var clock = Stopwatch.StartNew();
                var sent = 0;
                var tick = Math.Max(durationMs / bytes, 15);

                while (sent < bytes)
                {
                    var due = durationMs == 0
                        ? bytes
                        : (int)Math.Min(bytes, Math.Max(1, clock.ElapsedMilliseconds * bytes / durationMs));

                    if (due > sent)
                    {
                        await Response.Body.WriteAsync(Encoding.ASCII.GetBytes(new string('*', due - sent)), ct);
                        await Response.Body.FlushAsync(ct);
                        sent = due;
                    }

                    if (sent < bytes) await Task.Delay(tick, ct);
                }
            });
        }

        // -------------------- STREAMED JSON ARRAY --------------------
        /// <summary>Stream a JSON array one element at a time.</summary>
        /// <param name="n">Number of elements (1-1000; out-of-range values are clamped).</param>
        /// <param name="intervalMs">Delay between elements in milliseconds (0-5000, default 100).</param>
        /// <remarks>
        /// The complete body is a valid JSON array, but it arrives incrementally, which is useful for testing streaming
        /// JSON parsers. Example: <c>curl -N "http://localhost:5251/api/stream/json-array/10"</c>
        /// </remarks>
        /// <response code="200">A JSON array, streamed.</response>
        [HttpGet("json-array/{n:int}")]
        public async Task JsonArray(int n, [FromQuery] int intervalMs = 100)
        {
            n = Math.Clamp(n, 1, 1000);
            intervalMs = Math.Clamp(intervalMs, 0, 5000);

            await StreamAsync("application/json", async ct =>
            {
                await WriteAsync("[\n", ct);
                for (var i = 1; i <= n; i++)
                {
                    await WriteAsync("  " + JsonSerializer.Serialize(Item(i), Json) + (i < n ? ",\n" : "\n"), ct);
                    if (intervalMs > 0 && i < n) await Task.Delay(intervalMs, ct);
                }
                await WriteAsync("]\n", ct);
            });
        }

        private static object Item(int i) => new
        {
            id = i,
            name = $"{ItemNames[(i - 1) % ItemNames.Length]}-{i:000}",
            value = Math.Round(i * 3.14159, 2),
            even = i % 2 == 0
        };

        private async Task StreamAsync(string contentType, Func<CancellationToken, Task> body)
        {
            Response.ContentType = contentType;
            Response.Headers.CacheControl = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no"; // stop reverse proxies (nginx) from buffering
            HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

            try
            {
                await body(HttpContext.RequestAborted);
            }
            catch (Exception ex) when (ex is OperationCanceledException or IOException)
            {
                // Client disconnected — nothing left to do.
            }
        }

        private async Task WriteAsync(string text, CancellationToken ct)
        {
            await Response.WriteAsync(text, ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}
