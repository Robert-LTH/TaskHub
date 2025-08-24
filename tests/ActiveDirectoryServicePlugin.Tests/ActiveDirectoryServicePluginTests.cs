using ActiveDirectoryServicePlugin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ActiveDirectoryServicePlugin.Tests;

public class ActiveDirectoryServicePluginTests
{
    [Fact]
    public void NameIsActiveDirectory()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var plugin = new ActiveDirectoryServicePlugin(config, NullLogger<ActiveDirectoryServicePlugin>.Instance);
        Assert.Equal("activedirectory", plugin.Name);
    }
}
