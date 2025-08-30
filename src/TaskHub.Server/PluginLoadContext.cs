using System;
using System.Reflection;
using System.Runtime.Loader;

namespace TaskHub.Server;

public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Prefer assemblies already loaded in the default context for shared types
        // so interfaces and DI types unify across contexts.
        if (assemblyName.Name is not null)
        {
            if (assemblyName.Name == "TaskHub.Abstractions" ||
                assemblyName.Name.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal) ||
                assemblyName.Name.StartsWith("Hangfire", StringComparison.Ordinal) ||
                assemblyName.Name.StartsWith("System.", StringComparison.Ordinal))
            {
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
