using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MonitorServicePlugin;
using TaskHub.Abstractions;

namespace MonitorHandler;

public class MonitorInfoCommand : ICommand
{
    private readonly IReportingContainer? _container;
    private readonly ILogger _logger;

    public MonitorInfoCommand(MonitorInfoRequest request, IReportingContainer? container, ILogger logger)
    {
        Request = request;
        _container = container;
        _logger = logger;
    }

    public MonitorInfoRequest Request { get; }

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        var monitorService = (MonitorService)service.GetService();
        var monitors = MonitorService.GetMonitors();
        var element = JsonSerializer.SerializeToElement(monitors);
        _container?.AddReport("monitor", element);
        return Task.FromResult(new OperationResult(element, "success"));
    }
}

