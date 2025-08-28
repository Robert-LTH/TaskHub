using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;

namespace CcmExecHandler;

public class CcmExecCommandHandler : CommandHandlerBase, ICommandHandler<TriggerScheduleCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "ccmexwc" };
    public override string ServiceName => "configurationmanager";

    TriggerScheduleCommand ICommandHandler<TriggerScheduleCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<TriggerScheduleRequest>(payload.GetRawText())
                      ?? new TriggerScheduleRequest();
        return new TriggerScheduleCommand(request);
    }

    public override ICommand Create(JsonElement payload) =>
        ((ICommandHandler<TriggerScheduleCommand>)this).Create(payload);

    public override void OnLoaded(IServiceProvider services) { }
}
