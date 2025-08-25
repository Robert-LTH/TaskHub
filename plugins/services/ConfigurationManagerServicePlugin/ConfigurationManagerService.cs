using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text.Json;
using TaskHub.Abstractions;

namespace ConfigurationManagerServicePlugin;

public class ConfigurationManagerServicePlugin : IServicePlugin
{
    public string Name => "configurationmanager";

    public object GetService() => new ConfigurationManagerService();

    private class ConfigurationManagerService
    {
        private static readonly ConcurrentDictionary<string, ManagementScope> ScopePool = new();

        private static ManagementScope GetScope(string host, string wmiNamespace)
        {
            var key = $"{host}\\{wmiNamespace}";
            return ScopePool.GetOrAdd(key, _ =>
            {
                var path = new ManagementPath
                {
                    Server = host,
                    NamespacePath = wmiNamespace
                };

                var scope = new ManagementScope(path);
                scope.Connect();
                return scope;
            });
        }

        public OperationResult Query(string host, string wmiNamespace, string query)
        {
            try
            {
                var scope = GetScope(host, wmiNamespace);
                using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query));
                using var collection = searcher.Get();

                var results = new List<Dictionary<string, object?>>();
                foreach (ManagementObject obj in collection)
                {
                    var dict = obj.Properties
                        .Cast<PropertyData>()
                        .ToDictionary(p => p.Name, p => (object?)obj[p.Name]);
                    results.Add(dict);
                }

                if (results.Count == 0)
                {
                    return new OperationResult(null, $"Query '{query}' returned no results");
                }

                var element = JsonSerializer.SerializeToElement(results);
                return new OperationResult(element, "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to execute WMI query '{query}': {ex.Message}");
            }
        }

        public OperationResult InvokeMethod(string host, string wmiNamespace, string path, string method, Dictionary<string, object?>? parameters = null)
        {
            try
            {
                var scope = GetScope(host, wmiNamespace);
                var managementPath = new ManagementPath(path);
                ManagementBaseObject? inParams = null;
                ManagementBaseObject? outParams;

                if (managementPath.IsClass)
                {
                    using var cls = new ManagementClass(scope, managementPath, null);
                    if (parameters != null && parameters.Count > 0)
                    {
                        inParams = cls.GetMethodParameters(method);
                        foreach (var kvp in parameters)
                        {
                            inParams[kvp.Key] = kvp.Value;
                        }
                    }

                    outParams = cls.InvokeMethod(method, inParams, null);
                }
                else
                {
                    using var obj = new ManagementObject(scope, managementPath, null);
                    if (parameters != null && parameters.Count > 0)
                    {
                        inParams = obj.GetMethodParameters(method);
                        foreach (var kvp in parameters)
                        {
                            inParams[kvp.Key] = kvp.Value;
                        }
                    }

                    outParams = obj.InvokeMethod(method, inParams, null);
                }

                var result = new Dictionary<string, object?>();
                if (outParams != null)
                {
                    foreach (PropertyData p in outParams.Properties)
                    {
                        result[p.Name] = p.Value;
                    }
                }

                var element = JsonSerializer.SerializeToElement(result);
                return new OperationResult(element, "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to invoke WMI method '{method}' on '{path}': {ex.Message}");
            }
        }

        public OperationResult GetErrorCode(string host, string wmiNamespace, string @class, string pnpDeviceId)
        {
            try
            {
                var escapedId = pnpDeviceId.Replace("\\", "\\\\");
                var query = $"SELECT ConfigManagerErrorCode FROM {@class} WHERE PNPDeviceID = '{escapedId}'";
                var result = Query(host, wmiNamespace, query);
                if (result.Payload is JsonElement element &&
                    element.ValueKind == JsonValueKind.Array &&
                    element.GetArrayLength() > 0)
                {
                    var first = element[0];
                    if (first.TryGetProperty("ConfigManagerErrorCode", out var code))
                    {
                        return new OperationResult(code, "success");
                    }
                }

                return new OperationResult(null, $"Device '{pnpDeviceId}' not found");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to get ConfigManagerErrorCode for '{pnpDeviceId}': {ex.Message}");
            }
        }
    }
}

