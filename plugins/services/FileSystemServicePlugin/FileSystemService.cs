using System;
using System.IO;
using System.Text.Json;
using TaskHub.Abstractions;

namespace FileSystemServicePlugin;

public class FileSystemServicePlugin : IServicePlugin
{
    public string Name => "filesystem";

    public object GetService() => new FileSystemService();

    private class FileSystemService
    {
        public OperationResult Read(string path)
        {
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
    }
}

