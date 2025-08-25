using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;

namespace MonitorHandler;

public class MonitorCommandHandler : ICommandHandler<MonitorInfoCommand>
{
    public IReadOnlyCollection<string> Commands => new[] { "monitor-info" };
    public string ServiceName => "monitor";

    public MonitorInfoCommand Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<MonitorInfoRequest>(payload.GetRawText()) ?? new MonitorInfoRequest();
        return new MonitorInfoCommand(request);
    }

    ICommand ICommandHandler.Create(JsonElement payload) => Create(payload);

    public void OnLoaded(IServiceProvider services) { }
}

