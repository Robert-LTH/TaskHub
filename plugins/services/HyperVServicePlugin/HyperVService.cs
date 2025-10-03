using System;
using System.Linq;
using System.Management.Automation;
using TaskHub.Abstractions;

namespace HyperVServicePlugin;

public class HyperVServicePlugin : IServicePlugin
{
    public IServiceProvider Services { get; private set; } = default!;

    public string Name => "hyperv";

    public void OnLoaded(IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public object GetService() => new HyperVService();

    public class HyperVService
    {
        public OperationResult CreateVSwitch(string name, string switchType)
        {
            try
            {
                using var ps = PowerShell.Create();
                ps.AddCommand("New-VMSwitch")
                  .AddParameter("Name", name)
                  .AddParameter("SwitchType", switchType);
                ps.Invoke();
                if (ps.HadErrors)
                {
                    var error = string.Join("; ", ps.Streams.Error.Select(e => e.ToString()));
                    return new OperationResult(null, error);
                }
                return new OperationResult(null, "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to create vSwitch: {ex.Message}");
            }
        }

        public OperationResult CreateVhdx(string path, long sizeBytes, bool dynamic)
        {
            try
            {
                using var ps = PowerShell.Create();
                ps.AddCommand("New-VHD")
                  .AddParameter("Path", path)
                  .AddParameter("SizeBytes", sizeBytes);
                if (dynamic)
                    ps.AddParameter("Dynamic");
                else
                    ps.AddParameter("Fixed");
                ps.Invoke();
                if (ps.HadErrors)
                {
                    var error = string.Join("; ", ps.Streams.Error.Select(e => e.ToString()));
                    return new OperationResult(null, error);
                }
                return new OperationResult(null, "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to create VHDX: {ex.Message}");
            }
        }

        public OperationResult CreateVm(string name, string vhdPath, string switchName, long memoryStartupBytes)
        {
            try
            {
                using var ps = PowerShell.Create();
                ps.AddCommand("New-VM")
                  .AddParameter("Name", name)
                  .AddParameter("MemoryStartupBytes", memoryStartupBytes)
                  .AddParameter("VHDPath", vhdPath)
                  .AddParameter("SwitchName", switchName);
                ps.Invoke();
                if (ps.HadErrors)
                {
                    var error = string.Join("; ", ps.Streams.Error.Select(e => e.ToString()));
                    return new OperationResult(null, error);
                }
                return new OperationResult(null, "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to create VM: {ex.Message}");
            }
        }
    }
}


