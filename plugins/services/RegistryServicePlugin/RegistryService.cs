using System;
using System.Text.Json;
using Microsoft.Win32;
using TaskHub.Abstractions;

namespace RegistryServicePlugin;

public class RegistryServicePlugin : IServicePlugin
{
    public string Name => "registry";

    public object GetService() => new RegistryService();

    private class RegistryService
    {
        public OperationResult Read(string keyPath, string property)
        {
            try
            {
                var (hive, subKey) = SplitHive(keyPath);
                using var key = hive.OpenSubKey(subKey);
                if (key == null)
                {
                    return new OperationResult(null, $"Registry key '{keyPath}' not found");
                }

                var value = key.GetValue(property);
                if (value == null)
                {
                    return new OperationResult(null, $"Property '{property}' not found in '{keyPath}'");
                }

                var element = JsonSerializer.SerializeToElement(value);
                return new OperationResult(element, "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to read '{property}' from '{keyPath}': {ex.Message}");
            }
        }

        public OperationResult Write(string keyPath, string property, object value)
        {
            try
            {
                var (hive, subKey) = SplitHive(keyPath);
                using var key = hive.CreateSubKey(subKey);
                if (key == null)
                {
                    return new OperationResult(null, $"Registry key '{keyPath}' could not be opened");
                }

                key.SetValue(property, value);
                var element = JsonSerializer.SerializeToElement(value);
                return new OperationResult(element, "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to write '{property}' to '{keyPath}': {ex.Message}");
            }
        }

        public OperationResult Delete(string keyPath, string property)
        {
            try
            {
                var (hive, subKey) = SplitHive(keyPath);
                using var key = hive.OpenSubKey(subKey, writable: true);
                if (key == null)
                {
                    return new OperationResult(null, $"Registry key '{keyPath}' not found");
                }

                key.DeleteValue(property, false);
                return new OperationResult(null, "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to delete '{property}' from '{keyPath}': {ex.Message}");
            }
        }

        private static (RegistryKey hive, string subKey) SplitHive(string keyPath)
        {
            var parts = keyPath.Split(new[] {'\\'}, 2);
            var hiveName = parts[0].ToUpperInvariant();
            var subKey = parts.Length > 1 ? parts[1] : string.Empty;
            RegistryKey hive = hiveName switch
            {
                "HKEY_CLASSES_ROOT" or "HKCR" => Registry.ClassesRoot,
                "HKEY_CURRENT_USER" or "HKCU" => Registry.CurrentUser,
                "HKEY_LOCAL_MACHINE" or "HKLM" => Registry.LocalMachine,
                "HKEY_USERS" or "HKU" => Registry.Users,
                "HKEY_CURRENT_CONFIG" or "HKCC" => Registry.CurrentConfig,
                _ => throw new ArgumentException($"Unknown hive: {hiveName}", nameof(keyPath))
            };
            return (hive, subKey);
        }
    }
}
