using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace DiskSpaceHandler;

public class DiskSpaceCommandHandler : CommandHandlerBase, ICommandHandler<DiskSpaceCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "disk-free" };
    public override string ServiceName => "filesystem";
    public override CommandExecutionContext ExecutionContext => CommandExecutionContext.RegularUserOrSystem;
    private IReportingContainer? _reporting;

    DiskSpaceCommand ICommandHandler<DiskSpaceCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<DiskSpaceRequest>(payload.GetRawText()) ?? new DiskSpaceRequest();
        return new DiskSpaceCommand(request, _reporting, logger);
    }

    public override ICommand Create(JsonElement payload, ILogger logger) =>
        ((ICommandHandler<DiskSpaceCommand>)this).Create(payload, logger);

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
        _reporting = (IReportingContainer?)services.GetService(typeof(IReportingContainer));
    }
}

