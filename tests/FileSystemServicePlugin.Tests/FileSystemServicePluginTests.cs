using FileSystemServicePlugin;
using Xunit;

namespace FileSystemServicePlugin.Tests;

public class FileSystemServicePluginTests
{
    [Fact]
    public void NameIsFilesystem()
    {
        var plugin = new FileSystemServicePlugin();
        Assert.Equal("filesystem", plugin.Name);
    }
}
