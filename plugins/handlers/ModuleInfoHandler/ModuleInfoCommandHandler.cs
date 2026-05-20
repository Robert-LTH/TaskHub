using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;

namespace ModuleInfoHandler;

public class ModuleInfoCommandHandler : CommandHandlerBase, ICommandHandler<ModuleInfoCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "loaded-modules" };
    public override string ServiceName => "appdomain";
    private IReportingContainer? _reporting;

    ModuleInfoCommand ICommandHandler<ModuleInfoCommand>.Create(JsonElement payload)
    {
        return new ModuleInfoCommand(_reporting);
    }

    public override ICommand Create(JsonElement payload) =>
        ((ICommandHandler<ModuleInfoCommand>)this).Create(payload);

    public override void OnLoaded(IServiceProvider services)
    {
        _reporting = (IReportingContainer?)services.GetService(typeof(IReportingContainer));
    }
}
