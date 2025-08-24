using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var sockets = new ConcurrentDictionary<WebSocket, byte>();

app.UseWebSockets();

app.MapGet("/", () => "WebSocket server running");

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    sockets.TryAdd(webSocket, 0);

    var buffer = new byte[1024 * 4];
    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    while (!result.CloseStatus.HasValue)
    {
        await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    }

    sockets.TryRemove(webSocket, out _);
    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
});

app.MapPost("/command", async (JsonElement request) =>
{
    if (!request.TryGetProperty("command", out var commandEl))
    {
        return Results.BadRequest();
    }

    var message = Encoding.UTF8.GetBytes(commandEl.GetRawText());
    foreach (var socket in sockets.Keys)
    {
        if (socket.State == WebSocketState.Open)
        {
            await socket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    return Results.Ok();
});

app.Run();
