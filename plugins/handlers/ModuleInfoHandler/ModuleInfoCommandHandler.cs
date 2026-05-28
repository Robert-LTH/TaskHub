using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace ModuleInfoHandler;

public class ModuleInfoCommandHandler : CommandHandlerBase, ICommandHandler<ModuleInfoCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "loaded-modules" };
    public override string ServiceName => "appdomain";
    public override CommandExecutionContext ExecutionContext => CommandExecutionContext.RegularUserOrSystem;
    private IReportingContainer? _reporting;

    ModuleInfoCommand ICommandHandler<ModuleInfoCommand>.Create(JsonElement payload, ILogger logger)
    {
        return new ModuleInfoCommand(_reporting, logger);
    }

    public override ICommand Create(JsonElement payload, ILogger logger) =>
        ((ICommandHandler<ModuleInfoCommand>)this).Create(payload, logger);

    public override void OnLoaded(IServiceProvider services)
    {
        _reporting = (IReportingContainer?)services.GetService(typeof(IReportingContainer));
    }
}
