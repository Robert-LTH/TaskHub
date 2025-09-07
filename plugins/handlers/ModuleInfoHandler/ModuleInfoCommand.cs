using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace ModuleInfoHandler;

public class ModuleInfoCommand : ICommand
{
    private readonly IReportingContainer? _container;

    public ModuleInfoCommand(IReportingContainer? container)
    {
        _container = container;
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

