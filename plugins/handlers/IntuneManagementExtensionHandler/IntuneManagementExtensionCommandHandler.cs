using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;

namespace IntuneManagementExtensionHandler;

public class IntuneManagementExtensionCommandHandler : CommandHandlerBase, ICommandHandler<TriggerSyncCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "intune-sync" };
    public override string ServiceName => "powershell";

    TriggerSyncCommand ICommandHandler<TriggerSyncCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<SyncRequest>(payload.GetRawText()) ?? new SyncRequest();
        return new TriggerSyncCommand(request);
    }

    public override ICommand Create(JsonElement payload) =>
        ((ICommandHandler<TriggerSyncCommand>)this).Create(payload);

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
    }
}



