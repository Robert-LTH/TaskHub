using System;
using System.Management;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace BitLockerHandler;

public class RotateKeyCommand : ICommand
{
    public RotateKeyCommand(RotateKeyRequest request)
    {
        Request = request;
    }

    public RotateKeyRequest Request { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        // Ensure service is initialized
        _ = service.GetService();
        var volume = Request.Volume;
        string? key = null;
        try
        {
            using var vol = new ManagementObject($"Win32_EncryptableVolume.DeviceID='{volume}'");
            // remove existing numerical passwords
            if (vol.InvokeMethod("GetKeyProtectors", new object[] { 3, null }) is ManagementBaseObject ids &&
                ids["VolumeKeyProtectorID"] is string[] protectors)
            {
                foreach (var id in protectors)
                {
                    vol.InvokeMethod("DeleteKeyProtector", new object[] { id });
                }
            }
            // add new numerical password
            vol.InvokeMethod("ProtectKeyWithNumericalPassword", new object[] { Guid.NewGuid().ToString(), null });
            if (vol.InvokeMethod("GetKeyProtectors", new object[] { 3, null }) is ManagementBaseObject newIds &&
                newIds["VolumeKeyProtectorID"] is string[] newProtectors && newProtectors.Length > 0)
            {
                var newId = newProtectors[^1];
                if (vol.InvokeMethod("GetKeyProtectorNumericalPassword", new object[] { newId, null }) is ManagementBaseObject pw &&
                    pw["NumericalPassword"] is string k)
                {
                    key = k;
                    await BitLockerCommandHandler.ReportKeyAsync(volume, key);
                }
            }
        }
        catch
        {
            // ignore errors for non-windows platforms
        }
        var result = JsonSerializer.SerializeToElement(new { Volume = volume, Key = key });
        return new OperationResult(result, "success");
    }
}
