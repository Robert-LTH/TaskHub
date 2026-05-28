using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace EchoHandler;

public class EchoCommandHandler : CommandHandlerBase, ICommandHandler<EchoCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "echo" };
    public override string ServiceName => "http";
    public override CommandExecutionContext ExecutionContext => CommandExecutionContext.RegularUserOrSystem;
    private IReportingContainer? _reporting;

    EchoCommand ICommandHandler<EchoCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<EchoRequest>(payload.GetRawText())
                      ?? new EchoRequest();
        return new EchoCommand(request, _reporting, logger);
    }

    public override ICommand Create(JsonElement payload, ILogger logger) =>
        ((ICommandHandler<EchoCommand>)this).Create(payload, logger);

    public override void OnLoaded(IServiceProvider services)
    {
        _reporting = (IReportingContainer?)services.GetService(typeof(IReportingContainer));
    }
}
