using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Abstractions;

namespace TaskHub.Server;

public class PluginManager
{
    // Store concrete implementation types rather than interface instances so the
    // dependency injection container can create fresh objects with their
    // dependencies satisfied on each request.
    private readonly Dictionary<string, (Type HandlerType, PluginLoadContext Context, string AssemblyPath)> _handlers = new();
    private readonly Dictionary<string, (Type ServiceType, PluginLoadContext Context, string AssemblyPath)> _services = new();
    private readonly List<string> _assemblies = new();
    private readonly IServiceProvider _provider;

    public PluginManager(IServiceProvider provider)
    {
        _provider = provider;
    }

    public void Load(string root)
    {
        var config = _provider.GetRequiredService<IConfiguration>();
        var serviceRoot = Path.Combine(root, "services");
        if (Directory.Exists(serviceRoot))
        {
            foreach (var dir in Directory.GetDirectories(serviceRoot))
            {
                var name = Path.GetFileName(dir).Replace("ServicePlugin", string.Empty);
                if (!config.GetSection($"PluginSettings:{name}").Exists()) continue;
                var dll = Directory.GetFiles(dir, "*.dll", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (dll == null) continue;
                var context = new PluginLoadContext(dll);
                var asm = context.LoadFromAssemblyPath(dll);
                var type = asm.GetTypes().FirstOrDefault(t => typeof(IServicePlugin).IsAssignableFrom(t) && !t.IsAbstract);
                if (type != null)
                {
                    var plugin = (IServicePlugin)ActivatorUtilities.CreateInstance(_provider, type)!;
                    _services[plugin.Name] = (type, context, dll);
                    _assemblies.Add(dll);
                }
            }
        }

        var handlerRoot = Path.Combine(root, "handlers");
        if (Directory.Exists(handlerRoot))
        {
            foreach (var dir in Directory.GetDirectories(handlerRoot))
            {
                var name = Path.GetFileName(dir).Replace("Handler", string.Empty);
                if (!config.GetSection($"PluginSettings:{name}").Exists()) continue;
                var dll = Directory.GetFiles(dir, "*.dll", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (dll == null) continue;
                var context = new PluginLoadContext(dll);
                var asm = context.LoadFromAssemblyPath(dll);
                var type = asm.GetTypes().FirstOrDefault(t => typeof(ICommandHandler).IsAssignableFrom(t) && !t.IsAbstract);
                if (type == null) continue;
                var handler = (ICommandHandler)ActivatorUtilities.CreateInstance(_provider, type)!;
                foreach (var command in handler.Commands)
                {
                    _handlers[command] = (type, context, dll);
                }
                _assemblies.Add(dll);
            }
        }
    }

    public IEnumerable<string> LoadedAssemblies => _assemblies;

    public ICommandHandler? GetHandler(string name)
    {
        if (_handlers.TryGetValue(name, out var value))
        {
            return (ICommandHandler)ActivatorUtilities.CreateInstance(_provider, value.HandlerType)!;
        }

        return null;
    }

    public IServicePlugin GetService(string name)
    {
        if (_services.TryGetValue(name, out var value))
        {
            return (IServicePlugin)ActivatorUtilities.CreateInstance(_provider, value.ServiceType)!;
        }

        throw new InvalidOperationException($"Service plugin {name} not loaded");
    }
}

