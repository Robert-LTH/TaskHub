using System;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;
using TaskHub.Abstractions;

namespace BitLockerServicePlugin;

[SupportedOSPlatform("windows")]
public class BitLockerServicePlugin : IServicePlugin
{
    private readonly BitLockerService _service;

    public IServiceProvider Services { get; private set; } = default!;

    public BitLockerServicePlugin(ILogger<BitLockerService> logger)
    {
        _service = new BitLockerService(logger);
    }

    public string Name => "bitlocker";

    public void OnLoaded(IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public object GetService() => _service;
}



