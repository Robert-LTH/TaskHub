using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using HttpPlugin = HttpServicePlugin.HttpServicePlugin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Abstractions;
using TaskHub.Server;
using Xunit;
using Microsoft.Extensions.Logging;

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
        var handlers = (ConcurrentDictionary<string, (Type HandlerType, PluginLoadContext Context, string AssemblyPath, Version? Version)>)handlersField.GetValue(manager)!;
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
        var handlers = (ConcurrentDictionary<string, (Type HandlerType, PluginLoadContext Context, string AssemblyPath, Version? Version)>)handlersField.GetValue(manager)!;
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
        var servicesDict = (ConcurrentDictionary<string, (Type ServiceType, PluginLoadContext Context, string AssemblyPath, Version? Version)>)servicesField.GetValue(manager)!;
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
        var handlers = (ConcurrentDictionary<string, (Type HandlerType, PluginLoadContext Context, string AssemblyPath, Version? Version)>)handlersField.GetValue(manager)!;
        var assembliesField = typeof(PluginManager).GetField("_assemblies", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var assemblies = (ConcurrentDictionary<string, byte>)assembliesField.GetValue(manager)!;
        var path = typeof(StubHandler).Assembly.Location;
        handlers["stub"] = (typeof(StubHandler), new PluginLoadContext(path), path, new Version(1, 0));
        assemblies[path] = 0;
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
        services.AddLogging();
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
            var pluginDll = Path.Combine(sourceDir, Path.GetFileName(typeof(HttpPlugin).Assembly.Location));
            File.Copy(pluginDll, Path.Combine(v1, Path.GetFileName(pluginDll)));
            File.Copy(pluginDll, Path.Combine(v2, Path.GetFileName(pluginDll)));

            manager.Load(root);

            Assert.Contains(manager.LoadedAssemblies, asm => asm.Contains("2.0.0"));

            var service = manager.GetService("http");
            Assert.IsType<HttpPlugin>(service);
        }
        finally
        {
            // Ensure loaded assemblies are unloaded before cleanup
            manager.UnloadAll();
            // Trigger unload finalization to release file locks on Windows
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            if (Directory.Exists(root))
            {
                try
                {
                    Directory.Delete(root, true);
                }
                catch (UnauthorizedAccessException)
                {
                    // Ignore sporadic file lock issues in CI environments
                }
            }
        }
    }

    private class StubServicePlugin : IServicePlugin
    {
        public string Name => "Stub";
        public IServiceProvider Services { get; private set; } = default!;
        public void OnLoaded(IServiceProvider services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }
        public object GetService() => new object();
    }

    private class StubRequest
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }

    private class StubCommand : ICommand
    {
        public StubCommand(StubRequest request)
        {
            Request = request;
        }

        public StubRequest Request { get; }

        public System.Threading.Tasks.Task<OperationResult> ExecuteAsync(
            IServicePlugin service,
            ILogger logger,
            System.Threading.CancellationToken cancellationToken)
        {
            var element = JsonDocument.Parse("{}" ).RootElement;
            return System.Threading.Tasks.Task.FromResult(new OperationResult(element, "success"));
        }
    }

    private class StubHandler : CommandHandlerBase, ICommandHandler<StubCommand>
    {
        public override IReadOnlyCollection<string> Commands => new[] { "stub" };
        public override string ServiceName => "Stub";
        private static StubCommand CreateCommand(JsonElement payload) =>
            new StubCommand(JsonSerializer.Deserialize<StubRequest>(payload.GetRawText()) ?? new StubRequest());

        public override ICommand Create(JsonElement payload) => CreateCommand(payload);

        StubCommand ICommandHandler<StubCommand>.Create(JsonElement payload) => CreateCommand(payload);
        public override void OnLoaded(IServiceProvider services)
        {
            base.OnLoaded(services);
        }
    }

    [Fact]
    public void DescribeInputs_ReturnsRequestProperties()
    {
        var method = typeof(PluginManager).GetMethod("DescribeInputs", BindingFlags.NonPublic | BindingFlags.Static)!;
        var inputs = (IReadOnlyList<CommandInput>)method.Invoke(null, new object[] { typeof(StubHandler) })!;
        var input = Assert.Single(inputs);
        Assert.Equal("value", input.Name);
    }

    [Fact]
    public void GetCommandInfos_ReturnsRegisteredInfo()
    {
        var manager = CreateManager();
        var field = typeof(PluginManager).GetField("_commandInfos", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var dict = (ConcurrentDictionary<string, CommandInfo>)field.GetValue(manager)!;
        dict["stub"] = new CommandInfo("stub", "svc", new[] { new CommandInput("value", "string") });

        var info = Assert.Single(manager.GetCommandInfos());
        Assert.Equal("stub", info.Name);
        Assert.Equal("value", info.Inputs[0].Name);
    }
}



