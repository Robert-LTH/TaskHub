using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace MonitorHandler;

public class MonitorCommandHandler : CommandHandlerBase, ICommandHandler<MonitorInfoCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "monitor-info" };
    public override string ServiceName => "monitor";
    public override CommandExecutionContext ExecutionContext => CommandExecutionContext.RegularUserOrSystem;
    private IReportingContainer? _reporting;

    MonitorInfoCommand ICommandHandler<MonitorInfoCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<MonitorInfoRequest>(payload.GetRawText()) ?? new MonitorInfoRequest();
        return new MonitorInfoCommand(request, _reporting, logger);
    }

    public override ICommand Create(JsonElement payload, ILogger logger) =>
        ((ICommandHandler<MonitorInfoCommand>)this).Create(payload, logger);

    public override void OnLoaded(IServiceProvider services)
    {
        _reporting = (IReportingContainer?)services.GetService(typeof(IReportingContainer));
    }
}
