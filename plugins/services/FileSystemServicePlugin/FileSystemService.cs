using System;
using System.IO;
using TaskHub.Abstractions;

namespace FileSystemServicePlugin;

public class FileSystemServicePlugin : IServicePlugin
{
    public string Name => "filesystem";

    public object GetService() => (Action<string>)CleanDirectory;

    private static void CleanDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(path))
        {
            File.Delete(file);
        }

        foreach (var directory in Directory.GetDirectories(path))
        {
            Directory.Delete(directory, true);
        }
    }
}
