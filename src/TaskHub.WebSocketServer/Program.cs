using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var connections = new ConcurrentDictionary<string, ClientConnection>();

app.UseWebSockets();

app.MapGet("/", () => "WebSocket server running");

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    var id = context.Request.Query["id"].FirstOrDefault();
    var criteria = context.Request.Query
        .Where(q => q.Key != "id")
        .ToDictionary(q => q.Key, q => q.Value.ToString());

    if (string.IsNullOrEmpty(id))
    {
        id = Guid.NewGuid().ToString();
    }

    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    var connection = new ClientConnection(webSocket, criteria);
    connections.TryAdd(id, connection);

    // inform client of assigned connection id when none was provided
    if (!context.Request.Query.ContainsKey("id"))
    {
        var idMsg = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { connectionId = id }));
        await webSocket.SendAsync(new ArraySegment<byte>(idMsg), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    var buffer = new byte[1024 * 4];
    var sb = new StringBuilder();
    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    while (!result.CloseStatus.HasValue)
    {
        sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
        if (result.EndOfMessage)
        {
            var message = sb.ToString();
            sb.Clear();
            try
            {
                var doc = JsonDocument.Parse(message);
                if (doc.RootElement.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "result")
                {
                    if (doc.RootElement.TryGetProperty("connectionId", out var connEl))
                    {
                        var targetId = connEl.GetString();
                        if (!string.IsNullOrEmpty(targetId) && connections.TryGetValue(targetId, out var target) && target.Socket.State == WebSocketState.Open)
                        {
                            var bytes = Encoding.UTF8.GetBytes(message);
                            await target.Socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                }
            }
            catch
            {
                // ignore malformed messages
            }
        }
        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    }

    connections.TryRemove(id, out _);
    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
});

app.MapPost("/command", async (JsonElement request) =>
{
    if (!request.TryGetProperty("command", out var commandEl))
    {
        return Results.BadRequest();
    }

    var message = Encoding.UTF8.GetBytes(commandEl.GetRawText());

    IEnumerable<ClientConnection> targets = connections.Values;

    if (request.TryGetProperty("connectionId", out var idEl) && idEl.GetString() is { } connectionId)
    {
        if (connections.TryGetValue(connectionId, out var connection))
        {
            targets = new[] { connection };
        }
        else
        {
            return Results.NotFound();
        }
    }
    else if (request.TryGetProperty("criteria", out var criteriaEl) && criteriaEl.ValueKind == JsonValueKind.Object)
    {
        var filters = criteriaEl.EnumerateObject()
            .Where(p => p.Value.ValueKind == JsonValueKind.String)
            .ToDictionary(p => p.Name, p => p.Value.GetString()!);

        targets = connections.Values.Where(c => filters.All(f => c.Criteria.TryGetValue(f.Key, out var v) && v == f.Value));
    }

    foreach (var target in targets)
    {
        if (target.Socket.State == WebSocketState.Open)
        {
            await target.Socket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    return Results.Ok();
});

app.Run();

record ClientConnection(WebSocket Socket, IDictionary<string, string> Criteria);
