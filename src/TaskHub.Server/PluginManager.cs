using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Abstractions;

namespace TaskHub.Server;

public class PluginManager
{
    private readonly Dictionary<string, (ICommandHandler Handler, PluginLoadContext Context, string AssemblyPath)> _handlers = new();
    private readonly Dictionary<string, (IServicePlugin Plugin, PluginLoadContext Context, string AssemblyPath)> _services = new();
    private readonly List<string> _assemblies = new();
    private readonly IServiceProvider _provider;

    public PluginManager(IServiceProvider provider)
    {
        _provider = provider;
    }

    public void Load(string root)
    {
        var serviceRoot = Path.Combine(root, "services");
        if (Directory.Exists(serviceRoot))
        {
            foreach (var dir in Directory.GetDirectories(serviceRoot))
            {
                var dll = Directory.GetFiles(dir, "*.dll", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (dll == null) continue;
                var context = new PluginLoadContext(dll);
                var asm = context.LoadFromAssemblyPath(dll);
                var type = asm.GetTypes().FirstOrDefault(t => typeof(IServicePlugin).IsAssignableFrom(t) && !t.IsAbstract);
                if (type != null)
                {
                    var plugin = (IServicePlugin)ActivatorUtilities.CreateInstance(_provider, type)!;
                    _services[plugin.Name] = (plugin, context, dll);
                    _assemblies.Add(dll);
                }
            }
        }

        var handlerRoot = Path.Combine(root, "handlers");
        if (Directory.Exists(handlerRoot))
        {
            foreach (var dir in Directory.GetDirectories(handlerRoot))
            {
                var dll = Directory.GetFiles(dir, "*.dll", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (dll == null) continue;
                var context = new PluginLoadContext(dll);
                var asm = context.LoadFromAssemblyPath(dll);
                var type = asm.GetTypes().FirstOrDefault(t => typeof(ICommandHandler).IsAssignableFrom(t) && !t.IsAbstract);
                if (type == null) continue;
                var handler = (ICommandHandler)ActivatorUtilities.CreateInstance(_provider, type)!;
                _handlers[handler.Name] = (handler, context, dll);
                _assemblies.Add(dll);
            }
        }
    }

    public IEnumerable<string> LoadedAssemblies => _assemblies;

    public ICommandHandler? GetHandler(string name) => _handlers.TryGetValue(name, out var value) ? value.Handler : null;

    public IServicePlugin GetService(string name) => _services.TryGetValue(name, out var value)
        ? value.Plugin
        : throw new InvalidOperationException($"Service plugin {name} not loaded");
}
