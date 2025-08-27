using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;

namespace MonitorHandler;

public class MonitorCommandHandler : CommandHandlerBase, ICommandHandler<MonitorInfoCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "monitor-info" };
    public override string ServiceName => "monitor";
    private IReportingContainer? _reporting;

    public MonitorInfoCommand Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<MonitorInfoRequest>(payload.GetRawText()) ?? new MonitorInfoRequest();
        return new MonitorInfoCommand(request, _reporting);
    }

    public override ICommand Create(JsonElement payload) => Create(payload);

    public override void OnLoaded(IServiceProvider services)
    {
        _reporting = services.GetService<IReportingContainer>();
    }
}

