using FileSystemServicePlugin;
using System;
using System.IO;
using System.Collections.Generic;
using TaskHub.Abstractions;
using Microsoft.Extensions.Configuration;
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

    [Fact]
    public void CanGetFreeSpace()
    {
        dynamic service = new FileSystemServicePlugin().GetService();
        OperationResult result = service.GetFreeSpace(Path.GetTempPath());
        Assert.Equal("success", result.Result);
        Assert.True(result.Payload?.GetProperty("freeBytes").GetInt64() > 0);
    }

    [Fact]
    public void GetFreeSpaceIncludesSalvageableTempFilesFromConfiguredPaths()
    {
        var tempDir1 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var tempDir2 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir1);
        Directory.CreateDirectory(tempDir2);

        var tempFile1 = Path.Combine(tempDir1, Path.GetRandomFileName());
        var tempFile2 = Path.Combine(tempDir2, Path.GetRandomFileName());
        File.WriteAllBytes(tempFile1, new byte[123]);
        File.WriteAllBytes(tempFile2, new byte[456]);

        var config = new ConfigurationBuilder().AddInMemoryCollection(new()
        {
            ["PluginSettings:FileSystem:TempPaths:0"] = tempDir1,
            ["PluginSettings:FileSystem:TempPaths:1"] = tempDir2
        }).Build();

        dynamic service = new FileSystemServicePlugin(config).GetService();
        OperationResult result = service.GetFreeSpace(tempDir1);

        var salvageable = result.Payload?.GetProperty("salvageable");
        Assert.NotNull(salvageable);

        bool found1 = false;
        bool found2 = false;
        foreach (var item in salvageable!.EnumerateArray())
        {
            var p = item.GetProperty("path").GetString();
            if (p == tempFile1)
            {
                found1 = true;
                Assert.Equal(123, item.GetProperty("sizeBytes").GetInt64());
            }
            else if (p == tempFile2)
            {
                found2 = true;
                Assert.Equal(456, item.GetProperty("sizeBytes").GetInt64());
            }
        }

        File.Delete(tempFile1);
        File.Delete(tempFile2);
        Directory.Delete(tempDir1);
        Directory.Delete(tempDir2);

        Assert.True(found1);
        Assert.True(found2);
    }
}
