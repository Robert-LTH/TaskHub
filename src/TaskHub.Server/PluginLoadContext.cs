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
