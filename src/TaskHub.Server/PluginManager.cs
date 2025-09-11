using System;
using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<string, (Type HandlerType, PluginLoadContext Context, string AssemblyPath, Version? Version)> _handlers = new();
    private readonly ConcurrentDictionary<string, (Type ServiceType, PluginLoadContext Context, string AssemblyPath, Version? Version)> _services = new();
    private readonly ConcurrentDictionary<string, CommandInfo> _commandInfos = new();
    private readonly ConcurrentDictionary<string, byte> _assemblies = new();
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
        var loadAll = config.GetValue<bool>("PluginSettings:LoadAll");
        var serviceRoot = Path.Combine(root, "services");
        var preferNames = new HashSet<string>(StringComparer.Ordinal) { "TaskHub.Abstractions" };
        var serviceAssemblyNames = new HashSet<string>(StringComparer.Ordinal);
        // If services were preloaded via PluginCatalog, reuse them and skip scanning
        if (PluginCatalog.Services.Count > 0)
        {
            foreach (var kv in PluginCatalog.Services)
            {
                // Prefer the equivalent type from the default load context if available
                var resolvedType = TryResolveDefaultContextType(kv.Value.ServiceType) ?? kv.Value.ServiceType;
                try
                {
                    var instance = (IServicePlugin)CreateInstanceWithFallback(resolvedType)!;
                    // Check optional prerequisites
                    if (instance is TaskHub.Abstractions.IPluginPrerequisites pre && !pre.ShouldLoad(_provider, out var reason))
                    {
                        _logger.LogInformation("Skipping service plugin {Type} due to prerequisites not met: {Reason}", resolvedType.FullName, reason ?? "unspecified");
                        continue;
                    }

                    _services[instance.Name] = (resolvedType, kv.Value.Context, kv.Value.AssemblyPath, kv.Value.Version);
                    _assemblies[kv.Value.AssemblyPath] = 0;

                    var asmName = AssemblyName.GetAssemblyName(kv.Value.AssemblyPath).Name;
                    if (!string.IsNullOrEmpty(asmName))
                    {
                        serviceAssemblyNames.Add(asmName!);
                        preferNames.Add(asmName!);
                    }

                    _logger.LogInformation("Loaded service plugin {Name} v{Version} from {Path}", instance.Name, kv.Value.Version?.ToString() ?? string.Empty, kv.Value.AssemblyPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to activate preloaded service plugin type {Type}", kv.Value.ServiceType.FullName);
                }
            }
        }
        else if (Directory.Exists(serviceRoot))
        {
            foreach (var dir in Directory.GetDirectories(serviceRoot))
            {
                var name = Path.GetFileName(dir).Replace("ServicePlugin", string.Empty);
                var enabled = loadAll || config.GetSection($"PluginSettings:{name}").Exists();
                if (!enabled)
                {
                    _logger.LogDebug("Skipping service plugin {Plugin} (no configuration section found)", name);
                    continue;
                }
                try
                {
                    var pluginDir = GetLatestVersionDirectory(dir);
                    var dll = Directory.GetFiles(pluginDir, $"{Path.GetFileName(dir)}.dll", SearchOption.TopDirectoryOnly).FirstOrDefault();
                    if (dll == null) continue;
                    var context = new PluginLoadContext(dll, preferNames);
                    var asm = context.LoadFromAssemblyPath(dll);
                    var spiName = typeof(IServicePlugin).FullName;
                    var type = asm.GetTypes().FirstOrDefault(t => !t.IsAbstract &&
                        t.GetInterfaces().Any(i => string.Equals(i.FullName, spiName, StringComparison.Ordinal)));
                    if (type != null)
                    {
                        // Prefer the equivalent type from the default load context if available
                        var resolvedType = TryResolveDefaultContextType(type) ?? type;
                        var instance = CreateInstanceWithFallback(resolvedType)!;
                        // Check optional prerequisites
                        if (instance is TaskHub.Abstractions.IPluginPrerequisites pre && !pre.ShouldLoad(_provider, out var reason))
                        {
                            _logger.LogInformation("Skipping service plugin {Type} due to prerequisites not met: {Reason}", resolvedType.FullName, reason ?? "unspecified");
                            continue;
                        }

                        var plugin = (IServicePlugin)instance;
                        var version = GetDirectoryVersion(pluginDir);
                        _services[plugin.Name] = (resolvedType, context, dll, version);
                        _assemblies[dll] = 0;
                        // Track assembly simple name to prefer it for handlers
                        try
                        {
                            var asmName = AssemblyName.GetAssemblyName(dll).Name;
                            if (!string.IsNullOrEmpty(asmName))
                            {
                                serviceAssemblyNames.Add(asmName!);
                                preferNames.Add(asmName!);
                            }
                        }
                        catch { }
                        _logger.LogInformation("Loaded service plugin {Name} v{Version} from {Path}", plugin.Name, version?.ToString() ?? string.Empty, dll);
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
                var enabled = loadAll || config.GetSection($"PluginSettings:{name}").Exists();
                if (!enabled)
                {
                    _logger.LogDebug("Skipping handler plugin {Plugin} (no configuration section found)", name);
                    continue;
                }
                try
                {
                    var pluginDir = GetLatestVersionDirectory(dir);
                    var expectedDll = $"{Path.GetFileName(dir)}.dll";
                    var dll = Directory.GetFiles(pluginDir, expectedDll, SearchOption.TopDirectoryOnly).FirstOrDefault();
                    if (dll == null)
                    {
                        _logger.LogWarning("Handler plugin folder {Folder} missing expected dll {Dll}", pluginDir, expectedDll);
                        continue;
                    }
                    // Prefer already loaded service plugin assemblies so handler and service share types
                    var context = new PluginLoadContext(dll, preferNames.Concat(serviceAssemblyNames));
                    var asm = context.LoadFromAssemblyPath(dll);
                    var ichName = typeof(ICommandHandler).FullName;
                    var ichGenericName = typeof(ICommandHandler<>).FullName;
                    var type = asm.GetTypes().FirstOrDefault(t => !t.IsAbstract &&
                        t.GetInterfaces().Any(i => string.Equals(i.FullName, ichName, StringComparison.Ordinal) ||
                            (i.IsGenericType && string.Equals(i.GetGenericTypeDefinition().FullName, ichGenericName, StringComparison.Ordinal))));
                    if (type == null)
                    {
                        _logger.LogWarning("No ICommandHandler implementation found in {Path}", dll);
                        continue;
                    }
                    // Prefer the equivalent type from the default load context if available
                    var resolvedType = TryResolveDefaultContextType(type) ?? type;
                    var handlerInstance = CreateHandlerInstance(resolvedType)!;
                    if (handlerInstance is TaskHub.Abstractions.IPluginPrerequisites pre && !pre.ShouldLoad(_provider, out var reason))
                    {
                        _logger.LogInformation("Skipping handler plugin {Type} due to prerequisites not met: {Reason}", resolvedType.FullName, reason ?? "unspecified");
                        continue;
                    }
                    var handler = (ICommandHandler)handlerInstance;
                    handler.OnLoaded(_provider);
                    var version = GetDirectoryVersion(pluginDir);
                    var inputs = DescribeInputs(type);
                    foreach (var command in handler.Commands)
                    {
                        _handlers[command] = (resolvedType, context, dll, version);
                        _commandInfos[command] = new CommandInfo(command, handler.ServiceName, inputs);
                    }
                    _assemblies[dll] = 0;
                    _logger.LogInformation("Loaded handler plugin {Type} v{Version} for commands: {Commands}", resolvedType.FullName, version?.ToString() ?? string.Empty, string.Join(", ", handler.Commands));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load handler plugin from {Directory}", dir);
                }
            }
        }
    }

    private static Type? TryResolveDefaultContextType(Type pluginType)
    {
        try
        {
            var asmName = pluginType.Assembly.GetName().Name;
            var fullName = pluginType.FullName;
            if (asmName == null || fullName == null) return null;
            foreach (var loaded in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!string.Equals(loaded.GetName().Name, asmName, StringComparison.Ordinal)) continue;
                var candidate = loaded.GetType(fullName, throwOnError: false, ignoreCase: false);
                if (candidate != null)
                {
                    return candidate;
                }
            }
        }
        catch
        {
        }
        return null;
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

    public IEnumerable<string> LoadedAssemblies => _assemblies.Keys;

    public string? GetHandlerVersion(string command)
    {
        return _handlers.TryGetValue(command, out var value) ? value.Version?.ToString() : null;
    }

    public IEnumerable<CommandInfo> GetCommandInfos() => _commandInfos.Values;

    public ICommandHandler? GetHandler(string name)
    {
        if (_handlers.TryGetValue(name, out var value))
        {
            return (ICommandHandler)CreateHandlerInstance(value.HandlerType)!;
        }

        return null;
    }

    public IServicePlugin GetService(string name)
    {
        if (_services.TryGetValue(name, out var value))
        {
            return (IServicePlugin)CreateInstanceWithFallback(value.ServiceType)!;
        }

        throw new InvalidOperationException($"Service plugin {name} not loaded");
    }

    private object? CreateInstanceWithFallback(Type type)
    {
        try
        {
            return ActivatorUtilities.CreateInstance(_provider, type);
        }
        catch (InvalidOperationException)
        {
            // Fall back to parameterless construction if DI cannot satisfy dependencies
            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor != null)
            {
                return Activator.CreateInstance(type);
            }
            throw; // rethrow if no suitable fallback
        }
    }

    // Handlers sometimes (incorrectly) inject concrete service plugin types.
    // To improve compatibility, try to satisfy such constructor parameters by
    // providing instances of the loaded service plugins as explicit arguments.
    private object? CreateHandlerInstance(Type handlerType)
    {
        try
        {
            return ActivatorUtilities.CreateInstance(_provider, handlerType);
        }
        catch (InvalidOperationException)
        {
            // Build a candidate extras bag with instances for any constructor
            // parameter that matches a loaded service plugin type.
            var extras = new List<object>();

            // Collect distinct parameter types across constructors to avoid duplicates
            var paramTypes = handlerType.GetConstructors()
                .SelectMany(c => c.GetParameters().Select(p => p.ParameterType))
                .Distinct()
                .ToArray();

            foreach (var pType in paramTypes)
            {
                // Find a loaded service plugin whose concrete type matches this parameter
                var match = _services.Values.FirstOrDefault(s => pType.IsAssignableFrom(s.ServiceType));
                if (match.ServiceType != null)
                {
                    try
                    {
                        var svcInstance = CreateInstanceWithFallback(match.ServiceType);
                        if (svcInstance != null)
                        {
                            extras.Add(svcInstance);
                        }
                    }
                    catch
                    {
                        // Ignore and continue; DI may still resolve via other means
                    }
                }
            }

            if (extras.Count > 0)
            {
                try
                {
                    return ActivatorUtilities.CreateInstance(_provider, handlerType, extras.ToArray());
                }
                catch (InvalidOperationException)
                {
                    // Fall through to final fallback
                }
            }

            // Final fallback: parameterless ctor if present
            var ctor = handlerType.GetConstructor(Type.EmptyTypes);
            if (ctor != null)
            {
                return Activator.CreateInstance(handlerType);
            }
            // Rethrow the original style of failure for clarity
            return ActivatorUtilities.CreateInstance(_provider, handlerType);
        }
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
            _handlers.TryRemove(kv.Key, out _);
        }

        foreach (var kv in _services.Where(kv => kv.Value.AssemblyPath == assemblyPath).ToList())
        {
            contexts.Add(kv.Value.Context);
            _services.TryRemove(kv.Key, out _);
        }

        _assemblies.TryRemove(assemblyPath, out _);

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

