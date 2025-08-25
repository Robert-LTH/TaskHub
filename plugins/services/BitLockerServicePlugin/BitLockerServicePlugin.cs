using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace BitLockerServicePlugin;

public class BitLockerServicePlugin : IServicePlugin
{
    private readonly BitLockerService _service;

    public BitLockerServicePlugin(ILogger<BitLockerService> logger)
    {
        _service = new BitLockerService(logger);
    }

    public string Name => "bitlocker";

    public object GetService() => _service;
}

