using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;

namespace DiskSpaceHandler;

public class DiskSpaceCommandHandler : CommandHandlerBase, ICommandHandler<DiskSpaceCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "disk-free" };
    public override string ServiceName => "filesystem";
    private IReportingContainer? _reporting;

    DiskSpaceCommand ICommandHandler<DiskSpaceCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<DiskSpaceRequest>(payload.GetRawText()) ?? new DiskSpaceRequest();
        return new DiskSpaceCommand(request, _reporting);
    }

    public override ICommand Create(JsonElement payload) =>
        ((ICommandHandler<DiskSpaceCommand>)this).Create(payload);

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
        _reporting = (IReportingContainer?)services.GetService(typeof(IReportingContainer));
    }
}


