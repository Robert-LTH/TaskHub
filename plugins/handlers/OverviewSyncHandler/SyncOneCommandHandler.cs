using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Abstractions;

namespace OverviewSyncHandler;

public class SyncOneCommandHandler : CommandHandlerBase, ICommandHandler<SyncOneCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "syncone" };
    public override string ServiceName => "overview";

    SyncOneCommand ICommandHandler<SyncOneCommand>.Create(JsonElement payload)
    {
        return new SyncOneCommand(Services);
    }

    public override ICommand Create(JsonElement payload) => ((ICommandHandler<SyncOneCommand>)this).Create(payload);

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
    }
}

