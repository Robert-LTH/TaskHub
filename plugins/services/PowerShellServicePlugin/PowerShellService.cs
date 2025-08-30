using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using TaskHub.Abstractions;

namespace PowerShellServicePlugin;

public class PowerShellServicePlugin : IServicePlugin
{
    public string Name => "powershell";

    public object GetService() => new PowerShellService();

    public class PowerShellService
    {
        public OperationResult Execute(string scriptBase64, string? version = null, Dictionary<string, object>? properties = null)
        {
            try
            {
                var script = Encoding.UTF8.GetString(Convert.FromBase64String(scriptBase64));
                var initial = InitialSessionState.CreateDefault();

                if (properties != null)
                {
                    foreach (var kvp in properties)
                    {
                        initial.Variables.Add(new SessionStateVariableEntry(kvp.Key, kvp.Value, string.Empty));
                    }
                }

                using var ps = System.Management.Automation.PowerShell.Create(initial);

                if (!string.IsNullOrEmpty(version))
                {
                    var engineVersion = ps.Runspace.Version.ToString();
                    if (!engineVersion.StartsWith(version, StringComparison.OrdinalIgnoreCase))
                    {
                        return new OperationResult(null, $"Requested PowerShell version {version} is not available (engine is {engineVersion})");
                    }
                }

                ps.AddScript(script);
                var results = ps.Invoke();

                if (ps.HadErrors)
                {
                    var error = string.Join("; ", ps.Streams.Error.Select(e => e.ToString()));
                    return new OperationResult(null, error);
                }

                var output = new List<object?>();
                foreach (var item in results)
                {
                    output.Add(item?.BaseObject);
                }
                var element = JsonSerializer.SerializeToElement(output);
                return new OperationResult(element, "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to execute script: {ex.Message}");
            }
        }
    }
}

