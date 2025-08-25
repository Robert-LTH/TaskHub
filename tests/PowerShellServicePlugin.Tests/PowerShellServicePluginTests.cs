using PowerShellServicePlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using TaskHub.Abstractions;
using Xunit;

namespace PowerShellServicePlugin.Tests;

public class PowerShellServicePluginTests
{
    [Fact]
    public void NameIsPowerShell()
    {
        var plugin = new PowerShellServicePlugin();
        Assert.Equal("powershell", plugin.Name);
    }

    [Fact]
    public void ExecutesScriptAndRespectsProperties()
    {
        dynamic service = new PowerShellServicePlugin().GetService();

        string engineVersion;
        using (var ps = PowerShell.Create())
        {
            engineVersion = ps.Runspace.Version.ToString();
        }

        var props = new Dictionary<string, object> { ["greeting"] = "Hello" };
        var script = "Write-Output $greeting";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(script));

        OperationResult result = service.Execute(base64, engineVersion, props);
        Assert.Equal("success", result.Result);
        var element = result.Payload!.Value;
        var first = element.EnumerateArray().First();
        Assert.Equal("Hello", first.GetString());
    }
}

