using ConfigurationManagerServicePlugin;
using TaskHub.Abstractions;
using System.Collections.Generic;
using Xunit;

namespace ConfigurationManagerServicePlugin.Tests;

public class ConfigurationManagerServicePluginTests
{
    [Fact]
    public void NameIsConfigurationManager()
    {
        var plugin = new ConfigurationManagerServicePlugin();
        Assert.Equal("configurationmanager", plugin.Name);
    }

    [Fact]
    public void QueryWithMissingDeviceReturnsError()
    {
        dynamic service = new ConfigurationManagerServicePlugin().GetService();
        const string query = "SELECT ConfigManagerErrorCode FROM Win32_PnPEntity WHERE PNPDeviceID = 'NON_EXISTING_DEVICE'";
        OperationResult result = service.Query(".", "root\\cimv2", query);
        Assert.Null(result.Payload);
        Assert.NotEqual("success", result.Result);
    }

    [Fact]
    public void InvokeWithMissingMethodReturnsError()
    {
        dynamic service = new ConfigurationManagerServicePlugin().GetService();
        var parameters = new Dictionary<string, object?>();
        OperationResult result = service.InvokeMethod(".", "root\\cimv2", "Win32_Process", "NonExistingMethod", parameters);
        Assert.Null(result.Payload);
        Assert.NotEqual("success", result.Result);
    }
}
