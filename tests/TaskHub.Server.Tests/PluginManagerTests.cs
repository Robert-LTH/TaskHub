using System;
using System.Collections.Generic;
using System.Reflection;
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
        var handlers = (Dictionary<string, (Type HandlerType, PluginLoadContext Context, string AssemblyPath)>)handlersField.GetValue(manager)!;
        var path = typeof(StubHandler).Assembly.Location;
        handlers["stub"] = (typeof(StubHandler), new PluginLoadContext(path), path);

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
    public void GetService_ReturnsInstance_WhenRegistered()
    {
        var manager = CreateManager();
        var servicesField = typeof(PluginManager).GetField("_services", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var servicesDict = (Dictionary<string, (Type ServiceType, PluginLoadContext Context, string AssemblyPath)>)servicesField.GetValue(manager)!;
        var path = typeof(StubServicePlugin).Assembly.Location;
        servicesDict["Stub"] = (typeof(StubServicePlugin), new PluginLoadContext(path), path);

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
        var handlers = (Dictionary<string, (Type HandlerType, PluginLoadContext Context, string AssemblyPath)>)handlersField.GetValue(manager)!;
        var assembliesField = typeof(PluginManager).GetField("_assemblies", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var assemblies = (List<string>)assembliesField.GetValue(manager)!;
        var path = typeof(StubHandler).Assembly.Location;
        handlers["stub"] = (typeof(StubHandler), new PluginLoadContext(path), path);
        assemblies.Add(path);
        manager.Unload(path);

        var handler = manager.GetHandler("stub");
        Assert.Null(handler);
        Assert.DoesNotContain(path, manager.LoadedAssemblies);
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
