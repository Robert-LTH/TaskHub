using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TaskHub.Abstractions;
using TaskHub.Server;
using Xunit;

namespace TaskHub.Server.Tests;

public class CommandEndpointsExecutionTests
{
    private static TestServer CreateServer(params (string Command, Type HandlerType)[] handlers)
    {
        var storage = new MemoryStorage();
        JobStorage.Current = storage;

        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddRouting();
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authorization:Policies:CommandExecutor:Roles:0"] = "CommandExecutor"
                    })
                    .Build();

                services.AddSingleton<IConfiguration>(config);
                services.AddAuthorization(options =>
                {
                    options.AddPolicy("CommandExecutor", p => p.RequireRole("CommandExecutor"));
                });
                services.AddSingleton<IBackgroundJobClient>(new BackgroundJobClient(storage));
                services.AddSingleton(CreateManager(handlers));
                services.AddSingleton<CommandExecutor>(sp => new CommandExecutor(
                    sp.GetRequiredService<PluginManager>(),
                    Array.Empty<IResultPublisher>(),
                    NullLoggerFactory.Instance,
                    new ScriptsRepository(),
                    new JobLogStore(),
                    Array.Empty<ILogPublisher>(),
                    config));
                services.AddSingleton<PayloadVerifier>(_ => new PayloadVerifier(config));
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(e => e.MapCommandEndpoints());
            });

        return new TestServer(builder);
    }

    [Fact]
    public async Task PostCommandsWithoutDelayReturnsCommandResult()
    {
        using var server = CreateServer(("api-success", typeof(ApiSuccessHandler)));
        var client = server.CreateClient();
        var request = CreateCommandRequest("api-success");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CommandStatusResult>();
        Assert.NotNull(result);
        Assert.Equal("success", result!.Status);
        var command = Assert.Single(result.Commands);
        Assert.Equal("api-success", command.Command);
        Assert.Equal("success", command.Result);
        Assert.Equal("api-result", command.Output.GetProperty("value").GetString());
    }

    [Fact]
    public async Task PostCommandsWithoutDelayReturnsCommandErrors()
    {
        using var server = CreateServer(("api-error", typeof(ApiErrorHandler)));
        var client = server.CreateClient();
        var request = CreateCommandRequest("api-error");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CommandStatusResult>();
        Assert.NotNull(result);
        Assert.Equal("api failure", result!.Status);
        var command = Assert.Single(result.Commands);
        Assert.Equal("api-error", command.Command);
        Assert.Equal("api failure", command.Result);
        Assert.Equal(JsonValueKind.Null, command.Output.ValueKind);
    }

    private static HttpRequestMessage CreateCommandRequest(string command)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/commands");
        request.Headers.Add("Test-Auth", "1");
        request.Headers.Add("Test-Role", "CommandExecutor");
        request.Content = JsonContent.Create(new
        {
            commands = new[] { new { command, payload = new { } } }
        });
        return request;
    }

    private static PluginManager CreateManager((string Command, Type HandlerType)[] handlers)
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var manager = new PluginManager(provider);
        var handlersField = typeof(PluginManager).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var handlersDict = (System.Collections.Concurrent.ConcurrentDictionary<string, (Type HandlerType, PluginLoadContext Context, string AssemblyPath, Version? Version)>)handlersField.GetValue(manager)!;
        var path = typeof(CommandEndpointsExecutionTests).Assembly.Location;

        foreach (var handler in handlers)
        {
            handlersDict[handler.Command] = (handler.HandlerType, new PluginLoadContext(path), path, null);
        }

        return manager;
    }

    private sealed class ApiSuccessHandler : CommandHandlerBase
    {
        public override IReadOnlyCollection<string> Commands => new[] { "api-success" };
        public override string ServiceName => string.Empty;
        public override CommandExecutionContext ExecutionContext => CommandExecutionContext.RegularUserOrSystem;
        public override ICommand Create(JsonElement payload) => new ApiSuccessCommand();
    }

    private sealed class ApiErrorHandler : CommandHandlerBase
    {
        public override IReadOnlyCollection<string> Commands => new[] { "api-error" };
        public override string ServiceName => string.Empty;
        public override CommandExecutionContext ExecutionContext => CommandExecutionContext.RegularUserOrSystem;
        public override ICommand Create(JsonElement payload) => new ApiErrorCommand();
    }

    private sealed class ApiSuccessCommand : ICommand
    {
        public Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.SerializeToElement(new { value = "api-result" });
            return Task.FromResult(new OperationResult(payload, "success"));
        }
    }

    private sealed class ApiErrorCommand : ICommand
    {
        public Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken) =>
            Task.FromResult(new OperationResult(null, "api failure"));
    }
}
