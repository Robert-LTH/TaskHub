using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;

namespace CcmExecHandler;

public class CcmExecCommandHandler : ICommandHandler<TriggerScheduleCommand>
{
    public IReadOnlyCollection<string> Commands => new[] { "ccmexwc" };
    public string ServiceName => "configurationmanager";

    public TriggerScheduleCommand Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<TriggerScheduleRequest>(payload.GetRawText())
                      ?? new TriggerScheduleRequest();
        return new TriggerScheduleCommand(request);
    }

    public ICommand Create(JsonElement payload) => Create(payload);

    public void OnLoaded(IServiceProvider services) { }
}
