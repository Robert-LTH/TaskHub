using System;
using System.Management;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BitLockerServicePlugin;

[SupportedOSPlatform("windows")]
public class BitLockerService
{
    private readonly ILogger<BitLockerService> _logger;
    private ManagementEventWatcher? _watcher;

    public event Action<string, string>? KeyAvailable;

    public BitLockerService(ILogger<BitLockerService> logger)
    {
        _logger = logger;
        if (!OperatingSystem.IsWindows())
        {
            _logger.LogInformation("BitLocker service is only available on Windows.");
            return;
        }

        _ = Task.Run(WatchAsync);
    }

    private async Task WatchAsync()
    {
        try
        {
            var query = new WqlEventQuery(
                "__InstanceModificationEvent",
                TimeSpan.FromSeconds(1),
                "TargetInstance ISA 'Win32_EncryptableVolume' AND TargetInstance.LockStatus = 0");
            _watcher = new ManagementEventWatcher(query);
            _watcher.EventArrived += (_, e) =>
            {
                try
                {
                    var volume = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                    var deviceId = (string)volume["DeviceID"];
                    using var vol = new ManagementObject($"Win32_EncryptableVolume.DeviceID='{deviceId}'");
                    if (vol.InvokeMethod("GetKeyProtectors", new object?[] { 0, null }) is ManagementBaseObject ids &&
                        ids["VolumeKeyProtectorID"] is string[] protectorIds && protectorIds.Length > 0)
                    {
                        foreach (var id in protectorIds)
                        {
                            if (vol.InvokeMethod("GetKeyProtectorNumericalPassword", new object?[] { id, null }) is ManagementBaseObject pw &&
                                pw["NumericalPassword"] is string key)
                            {
                                KeyAvailable?.Invoke(deviceId, key);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process BitLocker event");
                }
            };
            _watcher.Start();
            await Task.Delay(Timeout.Infinite);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start BitLocker watcher");
        }
    }
}

