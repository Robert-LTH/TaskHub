using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;

namespace TaskHub.Server;

public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly HashSet<string> _preferDefaultByName;

    public PluginLoadContext(string pluginPath, IEnumerable<string>? preferDefaultAssemblyNames = null) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
        _preferDefaultByName = new HashSet<string>(StringComparer.Ordinal);
        if (preferDefaultAssemblyNames != null)
        {
            foreach (var n in preferDefaultAssemblyNames)
            {
                if (!string.IsNullOrWhiteSpace(n)) _preferDefaultByName.Add(n);
            }
        }
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Prefer assemblies already loaded in the default context for shared types
        // so interfaces and DI types unify across contexts.
        var simpleName = assemblyName.Name;
        if (simpleName is not null)
        {
            if (simpleName == "TaskHub.Abstractions" ||
                simpleName.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal) ||
                simpleName.StartsWith("Hangfire", StringComparison.Ordinal) ||
                simpleName.StartsWith("System.", StringComparison.Ordinal) ||
                _preferDefaultByName.Contains(simpleName))
            {
                // First, if an assembly with this simple name is already loaded anywhere, reuse it
                var loaded = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, simpleName, StringComparison.Ordinal));
                if (loaded != null)
                {
                    return loaded;
                }
                try
                {
                    return Default.LoadFromAssemblyName(assemblyName);
                }
                catch
                {
                    // Fall through to resolver
                }
            }
        }

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        if (path != null)
        {
            return LoadFromAssemblyPath(path);
        }
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (path != null)
        {
            return LoadUnmanagedDllFromPath(path);
        }
        return IntPtr.Zero;
    }
}
