using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace IntuneManagementExtensionHandler;

public class IntuneManagementExtensionCommandHandler : CommandHandlerBase, ICommandHandler<TriggerSyncCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "intune-sync" };
    public override string ServiceName => "powershell";
    public override CommandExecutionContext ExecutionContext => CommandExecutionContext.System;

    TriggerSyncCommand ICommandHandler<TriggerSyncCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<SyncRequest>(payload.GetRawText()) ?? new SyncRequest();
        return new TriggerSyncCommand(request, logger);
    }

    public override ICommand Create(JsonElement payload, ILogger logger) =>
        ((ICommandHandler<TriggerSyncCommand>)this).Create(payload, logger);

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
    }
}


