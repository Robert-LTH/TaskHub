using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TaskHub.Abstractions;

namespace FileSystemServicePlugin;

public class FileSystemServicePlugin : IServicePlugin
{
    public string Name => "filesystem";

    public object GetService() => new FileSystemService();

    private class FileSystemService
    {
        private static readonly HashSet<string> RestrictedPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            Path.GetFullPath("/"),
            Path.GetFullPath("/etc"),
            Path.GetFullPath("/bin"),
            Path.GetFullPath("/usr"),
            Path.GetFullPath("/proc"),
            Path.GetFullPath("/sys")
        };

        private static void ValidatePath(string path)
        {
            var full = Path.GetFullPath(path);
            if (RestrictedPaths.Any(r => full.Equals(r, StringComparison.OrdinalIgnoreCase) ||
                                         full.StartsWith(r + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Access to path '{path}' is not allowed.");
            }
        }

        public OperationResult Read(string path)
        {
            ValidatePath(path);
            try
            {
                var content = File.ReadAllText(path);
                var element = JsonSerializer.SerializeToElement(content);
                return new OperationResult(element, "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to read '{path}': {ex.Message}");
            }
        }

        public OperationResult Write(string path, string content)
        {
            ValidatePath(path);
            try
            {
                File.WriteAllText(path, content);
                var element = JsonSerializer.SerializeToElement(content);
                return new OperationResult(element, "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to write '{path}': {ex.Message}");
            }
        }

        public OperationResult Delete(string path)
        {
            ValidatePath(path);
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                else
                {
                    return new OperationResult(null, $"Path '{path}' not found");
                }

                return new OperationResult(null, "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to delete '{path}': {ex.Message}");
            }
        }

        public OperationResult GetFreeSpace(string path)
        {
            try
            {
                var root = Path.GetPathRoot(Path.GetFullPath(path)) ?? path;
                var drive = new DriveInfo(root);

                var tempPath = Path.GetTempPath();
                var salvageable = new List<object>();

                try
                {
                    foreach (var entry in Directory.EnumerateFileSystemEntries(tempPath))
                    {
                        long size = 0;
                        try
                        {
                            if (File.Exists(entry))
                            {
                                size = new FileInfo(entry).Length;
                            }
                            else if (Directory.Exists(entry))
                            {
                                size = GetDirectorySize(entry);
                            }
                        }
                        catch
                        {
                        }

                        salvageable.Add(new { path = entry, sizeBytes = size });
                    }
                }
                catch
                {
                }

                var element = JsonSerializer.SerializeToElement(new
                {
                    freeBytes = drive.AvailableFreeSpace,
                    salvageable
                });

                return new OperationResult(element, "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to get free space for '{path}': {ex.Message}");
            }
        }

        private static long GetDirectorySize(string path)
        {
            long size = 0;
            try
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        size += new FileInfo(file).Length;
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }

            return size;
        }
    }
}

