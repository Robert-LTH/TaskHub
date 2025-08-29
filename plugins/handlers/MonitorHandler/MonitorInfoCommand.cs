using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MonitorServicePlugin;
using TaskHub.Abstractions;

namespace MonitorHandler;

public class MonitorInfoCommand : ICommand
{
    private readonly IReportingContainer? _container;

    public MonitorInfoCommand(MonitorInfoRequest request, IReportingContainer? container)
    {
        Request = request;
        _container = container;
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

