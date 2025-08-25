using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MonitorServicePlugin;
using TaskHub.Abstractions;

namespace MonitorHandler;

public class MonitorInfoCommand : ICommand
{
    public MonitorInfoCommand(MonitorInfoRequest request)
    {
        Request = request;
    }

    public MonitorInfoRequest Request { get; }

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken, ClientWebSocket? socket = null)
    {
        var monitorService = (MonitorService)service.GetService();
        var monitors = monitorService.GetMonitors();
        var element = JsonSerializer.SerializeToElement(monitors);
        return Task.FromResult(new OperationResult(element, "success"));
    }
}

