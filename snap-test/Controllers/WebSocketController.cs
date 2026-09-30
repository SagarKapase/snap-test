using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using snap_test.Helpers;

namespace snap_test.Controllers
{
    /// <summary>
    /// WebSocket test endpoints (app.UseWebSockets is registered in Program.cs):
    ///   /ws/echo   — echoes every text/binary message; send "close" to be disconnected.
    ///   /ws/ticker — pushes a deterministic stock tick every second (?count=, max 300).
    /// Plain HTTP requests get a 400 explaining how to connect.
    /// </summary>
    [ApiController]
    public class WebSocketController : ControllerBase
    {
        private const int MaxMessageBytes = 64 * 1024;
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        // -------------------- ECHO --------------------
        /// <summary>Open a WebSocket that echoes every message back.</summary>
        /// <remarks>
        /// On connect the server sends a JSON <c>welcome</c> message. Text and binary messages are echoed unchanged;
        /// sending the text <c>close</c> makes the server close with 1000 "Bye!". Messages over 64KB are rejected with
        /// close code 1009.
        /// <para>Swagger UI cannot open WebSockets. Use a WebSocket client instead:</para>
        /// <code>wscat -c ws://localhost:5251/ws/echo</code>
        /// </remarks>
        /// <response code="101">Switching Protocols: the WebSocket is open.</response>
        /// <response code="400">The request was plain HTTP rather than a WebSocket upgrade.</response>
        [Route("/ws/echo")]
        public async Task Echo()
        {
            if (!await EnsureWebSocketAsync("/ws/echo")) return;

            using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var ct = HttpContext.RequestAborted;

            try
            {
                await SendJsonAsync(ws, new
                {
                    type = "welcome",
                    message = "Connected to APIBee echo. Every text/binary message is echoed back. Send \"close\" to disconnect."
                }, ct);

                while (ws.State == WebSocketState.Open)
                {
                    var (type, data, tooBig) = await ReceiveMessageAsync(ws, ct);

                    if (type == WebSocketMessageType.Close)
                    {
                        await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                        break;
                    }

                    if (tooBig)
                    {
                        await CloseGracefullyAsync(ws, WebSocketCloseStatus.MessageTooBig, "Message exceeds 64KB");
                        break;
                    }

                    if (type == WebSocketMessageType.Text &&
                        Encoding.UTF8.GetString(data).Trim().Equals("close", StringComparison.OrdinalIgnoreCase))
                    {
                        await CloseGracefullyAsync(ws, WebSocketCloseStatus.NormalClosure, "Bye!");
                        break;
                    }

                    await ws.SendAsync(data, type, true, ct);
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException or WebSocketException)
            {
                // Client vanished without a close handshake.
            }
        }

        // -------------------- TICKER --------------------
        /// <summary>Open a WebSocket that pushes a stock tick every second.</summary>
        /// <param name="count">Number of ticks before the server closes (1-300, default 30).</param>
        /// <remarks>
        /// Each message is <c>{"type":"tick","data":{...}}</c>. After the last tick the server sends
        /// <c>{"type":"complete"}</c> and closes with 1000 "Ticker finished". Closing from the client stops the ticker early.
        /// <para>Swagger UI cannot open WebSockets. Use a WebSocket client instead:</para>
        /// <code>wscat -c "ws://localhost:5251/ws/ticker?count=10"</code>
        /// </remarks>
        /// <response code="101">Switching Protocols: the WebSocket is open.</response>
        /// <response code="400">The request was plain HTTP rather than a WebSocket upgrade.</response>
        [Route("/ws/ticker")]
        public async Task Ticker([FromQuery] int count = 30)
        {
            if (!await EnsureWebSocketAsync("/ws/ticker")) return;

            count = Math.Clamp(count, 1, 300);
            using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var ct = HttpContext.RequestAborted;

            // Only this task receives; the loop below only sends — WebSockets allow one of each at a time.
            var clientClosed = WaitForCloseAsync(ws, ct);

            try
            {
                for (var i = 1; i <= count && ws.State == WebSocketState.Open && !clientClosed.IsCompleted; i++)
                {
                    await SendJsonAsync(ws, new { type = "tick", data = StreamingController.StockTick(i) }, ct);
                    if (i < count) await Task.WhenAny(Task.Delay(1000, ct), clientClosed);
                }

                if (ws.State == WebSocketState.Open)
                {
                    await SendJsonAsync(ws, new { type = "complete", ticks = count }, ct);
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Ticker finished", CancellationToken.None);
                }
                else if (ws.State == WebSocketState.CloseReceived)
                {
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                }

                await Task.WhenAny(clientClosed, Task.Delay(5000, CancellationToken.None));
            }
            catch (Exception ex) when (ex is OperationCanceledException or WebSocketException)
            {
            }
        }

        private async Task<bool> EnsureWebSocketAsync(string path)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest) return true;

            Response.StatusCode = 400;
            await Response.WriteAsJsonAsync(ApiResponse.Error(400,
                $"This endpoint only accepts WebSocket connections. Connect with a WebSocket client to ws://{Request.Host}{path}"));
            return false;
        }

        private static async Task<(WebSocketMessageType type, byte[] data, bool tooBig)> ReceiveMessageAsync(
            WebSocket ws, CancellationToken ct)
        {
            var buffer = new byte[4096];
            using var message = new MemoryStream();
            var tooBig = false;
            WebSocketReceiveResult result;

            do
            {
                result = await ws.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                    return (WebSocketMessageType.Close, Array.Empty<byte>(), false);

                if (message.Length + result.Count > MaxMessageBytes) tooBig = true;
                else message.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            return (result.MessageType, message.ToArray(), tooBig);
        }

        private static async Task WaitForCloseAsync(WebSocket ws, CancellationToken ct)
        {
            var buffer = new byte[1024];
            try
            {
                while (ws.State is WebSocketState.Open or WebSocketState.CloseSent)
                {
                    var result = await ws.ReceiveAsync(buffer, ct);
                    if (result.MessageType == WebSocketMessageType.Close) return;
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException or WebSocketException)
            {
            }
        }

        /// <summary>Server-initiated close: send the close frame, then wait (briefly) for the client's reply.</summary>
        private static async Task CloseGracefullyAsync(WebSocket ws, WebSocketCloseStatus status, string reason)
        {
            await ws.CloseOutputAsync(status, reason, CancellationToken.None);

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var buffer = new byte[1024];
            try
            {
                while (ws.State == WebSocketState.CloseSent)
                    await ws.ReceiveAsync(buffer, timeout.Token);
            }
            catch (Exception ex) when (ex is OperationCanceledException or WebSocketException)
            {
            }
        }

        private static Task SendJsonAsync(WebSocket ws, object payload, CancellationToken ct) =>
            ws.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, Json)), WebSocketMessageType.Text, true, ct);
    }
}
