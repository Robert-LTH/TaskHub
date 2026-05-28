using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace ModuleInfoHandler;

public class ModuleInfoCommand : ICommand
{
    private readonly IReportingContainer? _container;
    private readonly ILogger _logger;

    public ModuleInfoCommand(IReportingContainer? container, ILogger logger)
    {
        _container = container;
        _logger = logger;
    }

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        var domain = (AppDomain)service.GetService();
        var modules = domain.GetAssemblies()
            .Select(a => new { Name = a.GetName().Name, Version = a.GetName().Version?.ToString() })
            .ToArray();
        var element = JsonSerializer.SerializeToElement(modules);
        _container?.AddReport("loaded-modules", element);
        return Task.FromResult(new OperationResult(element, "success"));
    }
}

