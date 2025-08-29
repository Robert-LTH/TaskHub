using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TaskHub.Abstractions;
using TaskHub.Server;
using Xunit;

namespace TaskHub.Server.Tests;

public class CommandExecutorTests
{
    private static PluginManager CreateManager(Dictionary<string, Type> handlers, Type serviceType)
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var manager = new PluginManager(provider);

        var handlersField = typeof(PluginManager).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var handlersDict = (System.Collections.Concurrent.ConcurrentDictionary<string, (Type HandlerType, PluginLoadContext Context, string AssemblyPath, Version? Version)>)handlersField.GetValue(manager)!;
        var servicesField = typeof(PluginManager).GetField("_services", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var servicesDict = (System.Collections.Concurrent.ConcurrentDictionary<string, (Type ServiceType, PluginLoadContext Context, string AssemblyPath, Version? Version)>)servicesField.GetValue(manager)!;
        var path = typeof(CommandExecutorTests).Assembly.Location;
        foreach (var pair in handlers)
        {
            handlersDict[pair.Key] = (pair.Value, new PluginLoadContext(path), path, null);
        }
        var serviceInstance = (IServicePlugin)Activator.CreateInstance(serviceType)!;
        servicesDict[serviceInstance.Name] = (serviceType, new PluginLoadContext(path), path, null);
        return manager;
    }

    private static CommandExecutor CreateExecutor(Dictionary<string, Type> handlers, Type serviceType)
    {
        var manager = CreateManager(handlers, serviceType);
        return new CommandExecutor(manager, Array.Empty<IResultPublisher>(), NullLogger<CommandExecutor>.Instance);
    }

    [Fact]
    public async Task ExecuteChain_RunsCommandsInParallel()
    {
        var handlers = new Dictionary<string, Type>
        {
            ["cmd1"] = typeof(Cmd1Handler),
            ["cmd2"] = typeof(Cmd2Handler)
        };
        var executor = CreateExecutor(handlers, typeof(StubService));
        var payload = JsonDocument.Parse("{}").RootElement;
        var sw = Stopwatch.StartNew();
        await executor.ExecuteChain(new[] { "cmd1", "cmd2" }, payload, null, null!, CancellationToken.None);
        sw.Stop();
        Assert.True(sw.Elapsed < TimeSpan.FromMilliseconds(350));
    }

    [Fact]
    public async Task ExecuteChain_WaitsWhenRequested()
    {
        var handlers = new Dictionary<string, Type>
        {
            ["cmd1"] = typeof(Cmd1Handler),
            ["cmdWait"] = typeof(CmdWaitHandler),
            ["cmd2"] = typeof(Cmd2Handler)
        };
        var executor = CreateExecutor(handlers, typeof(StubService));
        var payload = JsonDocument.Parse("{}").RootElement;
        var sw = Stopwatch.StartNew();
        await executor.ExecuteChain(new[] { "cmd1", "cmdWait", "cmd2" }, payload, null, null!, CancellationToken.None);
        sw.Stop();
        Assert.True(sw.Elapsed >= TimeSpan.FromMilliseconds(350));
    }

    private class StubService : IServicePlugin
    {
        public string Name => "Stub";
        public object GetService() => new object();
    }

    private class DelayCommand : ICommand
    {
        private readonly int _delay;
        public bool WaitForPrevious { get; }
        public DelayCommand(int delay, bool wait)
        {
            _delay = delay;
            WaitForPrevious = wait;
        }
        public async Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
        {
            await Task.Delay(_delay, cancellationToken);
            return new OperationResult(JsonDocument.Parse("null").RootElement, "ok");
        }
    }

    private class Cmd1Handler : CommandHandlerBase, ICommandHandler<DelayCommand>
    {
        public override IReadOnlyCollection<string> Commands => new[] { "cmd1" };
        public override string ServiceName => "Stub";
        public override void OnLoaded(IServiceProvider services) { }
        public override ICommand Create(JsonElement payload) => new DelayCommand(200, false);
        DelayCommand ICommandHandler<DelayCommand>.Create(JsonElement payload) => new DelayCommand(200, false);
    }

    private class Cmd2Handler : CommandHandlerBase, ICommandHandler<DelayCommand>
    {
        public override IReadOnlyCollection<string> Commands => new[] { "cmd2" };
        public override string ServiceName => "Stub";
        public override void OnLoaded(IServiceProvider services) { }
        public override ICommand Create(JsonElement payload) => new DelayCommand(200, false);
        DelayCommand ICommandHandler<DelayCommand>.Create(JsonElement payload) => new DelayCommand(200, false);
    }

    private class CmdWaitHandler : CommandHandlerBase, ICommandHandler<DelayCommand>
    {
        public override IReadOnlyCollection<string> Commands => new[] { "cmdWait" };
        public override string ServiceName => "Stub";
        public override void OnLoaded(IServiceProvider services) { }
        public override ICommand Create(JsonElement payload) => new DelayCommand(200, true);
        DelayCommand ICommandHandler<DelayCommand>.Create(JsonElement payload) => new DelayCommand(200, true);
    }
}
