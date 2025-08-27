using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TaskHub.Abstractions;

namespace TaskHub.Server;

public class PluginManager
{
    // Store concrete implementation types rather than interface instances so the
    // dependency injection container can create fresh objects with their
    // dependencies satisfied on each request.
    private readonly Dictionary<string, (Type HandlerType, PluginLoadContext Context, string AssemblyPath, Version? Version)> _handlers = new();
    private readonly Dictionary<string, (Type ServiceType, PluginLoadContext Context, string AssemblyPath, Version? Version)> _services = new();
    private readonly Dictionary<string, CommandInfo> _commandInfos = new();
    private readonly List<string> _assemblies = new();
    private readonly IServiceProvider _provider;
    private readonly ILogger<PluginManager> _logger;

    public PluginManager(IServiceProvider provider)
    {
        _provider = provider;
        _logger = provider.GetService<ILogger<PluginManager>>() ?? NullLogger<PluginManager>.Instance;
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
                try
                {
                    var pluginDir = GetLatestVersionDirectory(dir);
                    var dll = Directory.GetFiles(pluginDir, "*.dll", SearchOption.TopDirectoryOnly).FirstOrDefault();
                    if (dll == null) continue;
                    var context = new PluginLoadContext(dll);
                    var asm = context.LoadFromAssemblyPath(dll);
                    var type = asm.GetTypes().FirstOrDefault(t => typeof(IServicePlugin).IsAssignableFrom(t) && !t.IsAbstract);
                    if (type != null)
                    {
                        var plugin = (IServicePlugin)ActivatorUtilities.CreateInstance(_provider, type)!;
                        var version = GetDirectoryVersion(pluginDir);
                        _services[plugin.Name] = (type, context, dll, version);
                        _assemblies.Add(dll);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load service plugin from {Directory}", dir);
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
                try
                {
                    var pluginDir = GetLatestVersionDirectory(dir);
                    var dll = Directory.GetFiles(pluginDir, "*.dll", SearchOption.TopDirectoryOnly).FirstOrDefault();
                    if (dll == null) continue;
                    var context = new PluginLoadContext(dll);
                    var asm = context.LoadFromAssemblyPath(dll);
                    var type = asm.GetTypes().FirstOrDefault(t => typeof(ICommandHandler).IsAssignableFrom(t) && !t.IsAbstract);
                    if (type == null) continue;
                    var handler = (ICommandHandler)ActivatorUtilities.CreateInstance(_provider, type)!;
                    handler.OnLoaded(_provider);
                    var version = GetDirectoryVersion(pluginDir);
                    var inputs = DescribeInputs(type);
                    foreach (var command in handler.Commands)
                    {
                        _handlers[command] = (type, context, dll, version);
                        _commandInfos[command] = new CommandInfo(command, handler.ServiceName, inputs);
                    }
                    _assemblies.Add(dll);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load handler plugin from {Directory}", dir);
                }
            }
        }
    }

    private static string GetLatestVersionDirectory(string dir)
    {
        var versions = Directory.GetDirectories(dir)
            .Select(d => new { Path = d, Name = Path.GetFileName(d) })
            .Where(d => Version.TryParse(d.Name, out _))
            .Select(d => (Path: d.Path, Version: Version.Parse(d.Name)))
            .OrderByDescending(d => d.Version)
            .FirstOrDefault();

        return versions.Path ?? dir;
    }

    private static Version? GetDirectoryVersion(string dir)
    {
        var name = Path.GetFileName(dir);
        return Version.TryParse(name, out var version) ? version : null;
    }

    public IEnumerable<string> LoadedAssemblies => _assemblies;

    public string? GetHandlerVersion(string command)
    {
        return _handlers.TryGetValue(command, out var value) ? value.Version?.ToString() : null;
    }

    public IEnumerable<CommandInfo> GetCommandInfos() => _commandInfos.Values;

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

    private static IReadOnlyList<CommandInput> DescribeInputs(Type handlerType)
    {
        var handlerInterface = handlerType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>));
        if (handlerInterface == null) return Array.Empty<CommandInput>();

        var commandType = handlerInterface.GetGenericArguments()[0];
        var ctor = commandType.GetConstructors().FirstOrDefault();
        if (ctor == null) return Array.Empty<CommandInput>();

        var paramType = ctor.GetParameters().FirstOrDefault()?.ParameterType;
        if (paramType == null) return Array.Empty<CommandInput>();

        var inputs = new List<CommandInput>();
        foreach (var prop in paramType.GetProperties())
        {
            var name = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? prop.Name;
            inputs.Add(new CommandInput(name, prop.PropertyType.Name));
        }
        return inputs;
    }

    public void Unload(string assemblyPath)
    {
        var contexts = new List<PluginLoadContext>();

        foreach (var kv in _handlers.Where(kv => kv.Value.AssemblyPath == assemblyPath).ToList())
        {
            contexts.Add(kv.Value.Context);
            _handlers.Remove(kv.Key);
        }

        foreach (var kv in _services.Where(kv => kv.Value.AssemblyPath == assemblyPath).ToList())
        {
            contexts.Add(kv.Value.Context);
            _services.Remove(kv.Key);
        }

        _assemblies.RemoveAll(a => a == assemblyPath);

        foreach (var ctx in contexts.Distinct())
        {
            ctx.Unload();
        }
    }

    public void UnloadAll()
    {
        foreach (var ctx in _handlers.Values.Select(v => v.Context)
            .Concat(_services.Values.Select(v => v.Context)).Distinct())
        {
            ctx.Unload();
        }

        _handlers.Clear();
        _services.Clear();
        _assemblies.Clear();
    }
}

