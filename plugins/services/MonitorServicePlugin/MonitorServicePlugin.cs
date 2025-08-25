using TaskHub.Abstractions;

namespace MonitorServicePlugin;

public class MonitorServicePlugin : IServicePlugin
{
    private readonly MonitorService _service = new();

    public string Name => "monitor";

    public object GetService() => _service;
}

