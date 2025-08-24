using HttpServicePlugin;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HttpServicePlugin.Tests;

public class HttpServicePluginTests
{
    [Fact]
    public void NameIsHttp()
    {
        var plugin = new HttpServicePlugin(NullLogger<HttpServicePlugin>.Instance);
        Assert.Equal("http", plugin.Name);
    }
}
