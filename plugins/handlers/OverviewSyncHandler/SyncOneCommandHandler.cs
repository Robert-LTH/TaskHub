using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace OverviewSyncHandler;

public class SyncOneCommandHandler : CommandHandlerBase, ICommandHandler<SyncOneCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "syncone" };
    public override string ServiceName => "overview";
    public override CommandExecutionContext ExecutionContext => CommandExecutionContext.RegularUserOrSystem;

    SyncOneCommand ICommandHandler<SyncOneCommand>.Create(JsonElement payload, ILogger logger)
    {
        return new SyncOneCommand(Services, logger);
    }

    public override ICommand Create(JsonElement payload, ILogger logger) => ((ICommandHandler<SyncOneCommand>)this).Create(payload, logger);

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
    }
}
