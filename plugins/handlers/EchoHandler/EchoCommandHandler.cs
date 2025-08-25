using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;

namespace EchoHandler;

public class EchoCommandHandler : CommandHandlerBase, ICommandHandler<EchoCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "echo" };
    public override string ServiceName => "http";

    public EchoCommand Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<EchoRequest>(payload.GetRawText())
                      ?? new EchoRequest();
        return new EchoCommand(request);
    }

    public override ICommand Create(JsonElement payload) => Create(payload);

    public override void OnLoaded(IServiceProvider services) { }
}

