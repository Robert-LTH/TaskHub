using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TaskHub.Server;

// A lightweight catalog that scans service plugins early (before ServiceProvider build)
// and registers their concrete types into DI so handlers can constructor-inject them.
public static class PluginCatalog
{
    // Expose discovered service plugins so PluginManager can reuse them
    internal static readonly ConcurrentDictionary<string, (Type ServiceType, PluginLoadContext Context, string AssemblyPath, Version? Version)> Services = new();

    public static void Register(IServiceCollection services, IConfiguration config, string root)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (config is null) throw new ArgumentNullException(nameof(config));

        var loadAll = config.GetValue<bool>("PluginSettings:LoadAll");
        var serviceRoot = Path.Combine(root, "services");
        if (!Directory.Exists(serviceRoot)) return;

        foreach (var dir in Directory.GetDirectories(serviceRoot))
        {
            var name = Path.GetFileName(dir).Replace("ServicePlugin", string.Empty);
            var enabled = loadAll || config.GetSection($"PluginSettings:{name}").Exists();
            if (!enabled) continue;

            try
            {
                var pluginDir = GetLatestVersionDirectory(dir);
                var dll = Directory.GetFiles(pluginDir, $"{Path.GetFileName(dir)}.dll", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (dll == null) continue;

                var context = new PluginLoadContext(dll, new[] { "TaskHub.Abstractions" });
                var asm = context.LoadFromAssemblyPath(dll);
                var spiName = typeof(TaskHub.Abstractions.IServicePlugin).FullName;
                var type = asm.GetTypes().FirstOrDefault(t => !t.IsAbstract &&
                    t.GetInterfaces().Any(i => string.Equals(i.FullName, spiName, StringComparison.Ordinal)));
                if (type == null) continue;

                var version = GetDirectoryVersion(pluginDir);
                Services[name] = (type, context, dll, version);

                // Register the concrete service plugin type so DI can inject it into handlers
                services.AddSingleton(type);
            }
            catch
            {
                // Swallow at catalog stage; runtime loader will log failures
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
}

