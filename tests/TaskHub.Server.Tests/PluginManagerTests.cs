using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HttpPlugin = HttpServicePlugin.HttpServicePlugin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Abstractions;
using TaskHub.Server;
using Xunit;

namespace TaskHub.Server.Tests;

public class PluginManagerTests
{
    private static PluginManager CreateManager()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        return new PluginManager(provider);
    }

    [Fact]
    public void GetHandler_ReturnsInstance_WhenRegistered()
    {
        var manager = CreateManager();
        var handlersField = typeof(PluginManager).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var handlers = (Dictionary<string, (Type HandlerType, PluginLoadContext Context, string AssemblyPath, Version? Version)>)handlersField.GetValue(manager)!;
        var path = typeof(StubHandler).Assembly.Location;
        handlers["stub"] = (typeof(StubHandler), new PluginLoadContext(path), path, new Version(1, 0));

        var handler = manager.GetHandler("stub");

        Assert.NotNull(handler);
        Assert.IsType<StubHandler>(handler);
    }

    [Fact]
    public void GetHandler_ReturnsNull_ForUnknownCommand()
    {
        var manager = CreateManager();
        var handler = manager.GetHandler("missing");
        Assert.Null(handler);
    }

    [Fact]
    public void GetHandlerVersion_ReturnsVersion_WhenRegistered()
    {
        var manager = CreateManager();
        var handlersField = typeof(PluginManager).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var handlers = (Dictionary<string, (Type HandlerType, PluginLoadContext Context, string AssemblyPath, Version? Version)>)handlersField.GetValue(manager)!;
        var path = typeof(StubHandler).Assembly.Location;
        handlers["stub"] = (typeof(StubHandler), new PluginLoadContext(path), path, new Version(2, 1, 3));

        var version = manager.GetHandlerVersion("stub");

        Assert.Equal("2.1.3", version);
    }

    [Fact]
    public void GetService_ReturnsInstance_WhenRegistered()
    {
        var manager = CreateManager();
        var servicesField = typeof(PluginManager).GetField("_services", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var servicesDict = (Dictionary<string, (Type ServiceType, PluginLoadContext Context, string AssemblyPath, Version? Version)>)servicesField.GetValue(manager)!;
        var path = typeof(StubServicePlugin).Assembly.Location;
        servicesDict["Stub"] = (typeof(StubServicePlugin), new PluginLoadContext(path), path, new Version(1, 0));

        var service = manager.GetService("Stub");

        Assert.IsType<StubServicePlugin>(service);
    }

    [Fact]
    public void GetService_Throws_WhenNotLoaded()
    {
        var manager = CreateManager();
        Assert.Throws<InvalidOperationException>(() => manager.GetService("unknown"));
    }

    [Fact]
    public void Unload_RemovesPlugins()
    {
        var manager = CreateManager();
        var handlersField = typeof(PluginManager).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var handlers = (Dictionary<string, (Type HandlerType, PluginLoadContext Context, string AssemblyPath, Version? Version)>)handlersField.GetValue(manager)!;
        var assembliesField = typeof(PluginManager).GetField("_assemblies", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var assemblies = (List<string>)assembliesField.GetValue(manager)!;
        var path = typeof(StubHandler).Assembly.Location;
        handlers["stub"] = (typeof(StubHandler), new PluginLoadContext(path), path, new Version(1, 0));
        assemblies.Add(path);
        manager.Unload(path);

        var handler = manager.GetHandler("stub");
        Assert.Null(handler);
        Assert.DoesNotContain(path, manager.LoadedAssemblies);
    }

    [Fact]
    public void Load_UsesHighestVersion_WhenMultipleExist()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PluginSettings:http"] = string.Empty
            })
            .Build();
        services.AddSingleton<IConfiguration>(config);
        var provider = services.BuildServiceProvider();
        var manager = new PluginManager(provider);

        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var pluginRoot = Path.Combine(root, "services", "HttpServicePlugin");
            var v1 = Path.Combine(pluginRoot, "1.0.0");
            var v2 = Path.Combine(pluginRoot, "2.0.0");
            Directory.CreateDirectory(v1);
            Directory.CreateDirectory(v2);

            var sourceDir = Path.GetDirectoryName(typeof(HttpPlugin).Assembly.Location)!;
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var name = Path.GetFileName(file);
                File.Copy(file, Path.Combine(v1, name));
                File.Copy(file, Path.Combine(v2, name));
            }

            manager.Load(root);

            var asm = Assert.Single(manager.LoadedAssemblies);
            Assert.Contains("2.0.0", asm);

            var service = manager.GetService("http");
            Assert.IsType<HttpPlugin>(service);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    private class StubServicePlugin : IServicePlugin
    {
        public string Name => "Stub";
        public object GetService() => new object();
    }

    private class StubCommand : ICommand
    {
        public System.Threading.Tasks.Task<OperationResult> ExecuteAsync(
            IServicePlugin service,
            System.Threading.CancellationToken cancellationToken)
        {
            var element = System.Text.Json.JsonDocument.Parse("{}" ).RootElement;
            return System.Threading.Tasks.Task.FromResult(new OperationResult(element, "success"));
        }
    }

    private class StubHandler : CommandHandlerBase, ICommandHandler<StubCommand>
    {
        public override IReadOnlyCollection<string> Commands => new[] { "stub" };
        public override string ServiceName => "Stub";
        public StubCommand Create(System.Text.Json.JsonElement payload) => new StubCommand();
        public override ICommand Create(System.Text.Json.JsonElement payload) => Create(payload);
        public override void OnLoaded(IServiceProvider services) { }
    }
}
