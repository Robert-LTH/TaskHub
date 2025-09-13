using EventLogServicePlugin;
using TaskHub.Abstractions;
using Xunit;

namespace EventLogServicePlugin.Tests;

public class EventLogServicePluginTests
{
    [Fact]
    public void NameIsEventLog()
    {
        var plugin = new EventLogServicePlugin();
        Assert.Equal("eventlog", plugin.Name);
    }

    [Fact]
    public void WriteOnUnsupportedPlatformReturnsError()
    {
        dynamic service = new EventLogServicePlugin().GetService();
        OperationResult result = service.Write("TaskHub", "test message");
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            Assert.Null(result.Payload);
            Assert.Contains("EventLog", result.Result);
        }
    }

    [Fact]
    public void ReadOnUnsupportedPlatformReturnsError()
    {
        dynamic service = new EventLogServicePlugin().GetService();
        OperationResult result = service.Read("Application");
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            Assert.Null(result.Payload);
            Assert.Contains("EventLog", result.Result);
        }
    }

    [Fact]
    public void ReadWithXPathOnUnsupportedPlatformReturnsError()
    {
        dynamic service = new EventLogServicePlugin().GetService();
        OperationResult result = service.Read("Application", "*[System/Level=2]");
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            Assert.Null(result.Payload);
            Assert.Contains("EventLog", result.Result);
        }
    }
}
