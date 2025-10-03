using System;
using TaskHub.Abstractions;

namespace MonitorServicePlugin;

public class MonitorServicePlugin : IServicePlugin
{
    private readonly MonitorService _service = new();

    public IServiceProvider Services { get; private set; } = default!;

    public string Name => "monitor";

    public void OnLoaded(IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public object GetService() => _service;
}


