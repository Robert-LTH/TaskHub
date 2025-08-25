using FileSystemServicePlugin;
using System;
using System.IO;
using TaskHub.Abstractions;
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

    [Fact]
    public void CanReadWriteAndDeleteFile()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        dynamic service = new FileSystemServicePlugin().GetService();

        OperationResult write = service.Write(tempFile, "hello");
        Assert.Equal("success", write.Result);
        Assert.True(File.Exists(tempFile));

        OperationResult read = service.Read(tempFile);
        Assert.Equal("hello", read.Payload?.GetString());

        OperationResult delete = service.Delete(tempFile);
        Assert.Equal("success", delete.Result);
        Assert.False(File.Exists(tempFile));
    }

    [Fact]
    public void RestrictedPathsThrow()
    {
        dynamic service = new FileSystemServicePlugin().GetService();

        Assert.Throws<InvalidOperationException>(() => service.Read("/etc/passwd"));
        Assert.Throws<InvalidOperationException>(() => service.Write("/etc/passwd", "test"));
        Assert.Throws<InvalidOperationException>(() => service.Delete("/etc/passwd"));
    }
}
