using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;

namespace IntuneManagementExtensionHandler;

public class IntuneManagementExtensionCommandHandler : ICommandHandler<TriggerSyncCommand>
{
    public IReadOnlyCollection<string> Commands => new[] { "intune-sync" };
    public string ServiceName => "powershell";

    public TriggerSyncCommand Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<SyncRequest>(payload.GetRawText()) ?? new SyncRequest();
        return new TriggerSyncCommand(request);
    }

    public ICommand Create(JsonElement payload) => Create(payload);

    public void OnLoaded(IServiceProvider services) { }
}
