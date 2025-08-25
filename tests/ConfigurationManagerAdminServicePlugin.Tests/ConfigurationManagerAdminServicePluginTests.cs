using ConfigurationManagerAdminServicePlugin;
using Xunit;

namespace ConfigurationManagerAdminServicePlugin.Tests;

public class ConfigurationManagerAdminServicePluginTests
{
    [Fact]
    public void NameIsConfigurationManagerAdmin()
    {
        var plugin = new ConfigurationManagerAdminServicePlugin();
        Assert.Equal("configurationmanageradmin", plugin.Name);
    }
}
