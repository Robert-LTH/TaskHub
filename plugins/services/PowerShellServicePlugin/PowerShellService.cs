using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using TaskHub.Abstractions;

namespace PowerShellServicePlugin;

public class PowerShellServicePlugin : IServicePlugin
{
    public IServiceProvider Services { get; private set; } = default!;

    public string Name => "powershell";

    public void OnLoaded(IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

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

                // Log PowerShell stream events (errors, warnings, verbose, debug, information)
                ps.Streams.Error.DataAdded += (s, e) =>
                {
                    try
                    {
                        var rec = ps.Streams.Error[e.Index];
                        Trace.WriteLine($"[PS:Error] {rec}");
                    }
                    catch { }
                };
                ps.Streams.Warning.DataAdded += (s, e) =>
                {
                    try
                    {
                        var rec = ps.Streams.Warning[e.Index];
                        Trace.WriteLine($"[PS:Warning] {rec}");
                    }
                    catch { }
                };
                ps.Streams.Verbose.DataAdded += (s, e) =>
                {
                    try
                    {
                        var rec = ps.Streams.Verbose[e.Index];
                        Trace.WriteLine($"[PS:Verbose] {rec}");
                    }
                    catch { }
                };
                ps.Streams.Debug.DataAdded += (s, e) =>
                {
                    try
                    {
                        var rec = ps.Streams.Debug[e.Index];
                        Trace.WriteLine($"[PS:Debug] {rec}");
                    }
                    catch { }
                };
                ps.Streams.Information.DataAdded += (s, e) =>
                {
                    try
                    {
                        var rec = ps.Streams.Information[e.Index];
                        var msg = rec?.MessageData?.ToString() ?? rec?.ToString();
                        if (!string.IsNullOrEmpty(msg)) Trace.WriteLine($"[PS:Info] {msg}");
                    }
                    catch { }
                };

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
                    var line = item?.BaseObject;
                    if (line != null)
                    {
                        Trace.WriteLine($"[PS:Output] {line}");
                    }
                    output.Add(line);
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


