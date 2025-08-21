using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;

namespace EchoHandler;

public class EchoCommandHandler : ICommandHandler<EchoCommand>
{
    public IReadOnlyCollection<string> Commands => new[] { "echo" };
    public string ServiceName => "http";

    public EchoCommand Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<EchoRequest>(payload.GetRawText())
                      ?? new EchoRequest();
        return new EchoCommand(request);
    }
}

