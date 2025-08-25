using System;
using RegistryServicePlugin;
using TaskHub.Abstractions;
using Xunit;

namespace RegistryServicePlugin.Tests;

public class RegistryServicePluginTests
{
    [Fact]
    public void NameIsRegistry()
    {
        var plugin = new RegistryServicePlugin();
        Assert.Equal("registry", plugin.Name);
    }

    [Fact]
    public void ReadMissingKeyReturnsError()
    {
        dynamic service = new RegistryServicePlugin().GetService();
        OperationResult result = service.Read("HKEY_CURRENT_USER\\Software\\TaskHub_Missing", "Value");
        Assert.Null(result.Payload);
        Assert.Contains("Registry key", result.Result);
    }

    [Fact]
    public void WriteWithNullPropertyReturnsError()
    {
        dynamic service = new RegistryServicePlugin().GetService();
        OperationResult result = service.Write("HKEY_CURRENT_USER\\Software\\TaskHub_Test", null!, "value");
        Assert.Null(result.Payload);
        Assert.Contains("Failed to write", result.Result);
    }

    [Fact]
    public void DeleteMissingKeyReturnsError()
    {
        dynamic service = new RegistryServicePlugin().GetService();
        OperationResult result = service.Delete("HKEY_CURRENT_USER\\Software\\TaskHub_Missing", "Value");
        Assert.Null(result.Payload);
        Assert.Contains("Failed to delete", result.Result);
    }
}
