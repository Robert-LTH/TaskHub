using BitLockerServicePlugin;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BitLockerServicePlugin.Tests;

public class BitLockerServicePluginTests
{
    [Fact]
    public void NameIsBitLocker()
    {
        var plugin = new BitLockerServicePlugin(NullLogger<BitLockerService>.Instance);
        Assert.Equal("bitlocker", plugin.Name);
    }

    [Fact]
    public void ServiceIsExposed()
    {
        var plugin = new BitLockerServicePlugin(NullLogger<BitLockerService>.Instance);
        Assert.IsType<BitLockerService>(plugin.GetService());
    }
}
