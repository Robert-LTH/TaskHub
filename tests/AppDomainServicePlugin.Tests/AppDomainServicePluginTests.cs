using System;
using AppDomainServicePlugin;
using Xunit;

namespace AppDomainServicePlugin.Tests;

public class AppDomainServicePluginTests
{
    [Fact]
    public void NameIsAppDomainAndReturnsCurrentDomain()
    {
        var plugin = new AppDomainServicePlugin();
        Assert.Equal("appdomain", plugin.Name);
        Assert.IsType<AppDomain>(plugin.GetService());
    }
}

