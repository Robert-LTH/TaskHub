using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Tasks;
using TaskHub.Abstractions;
using TaskHub.Server;
using Xunit;

namespace TaskHub.Server.Tests;

public class CommandModifyTests
{
    private TestServer CreateServer(out MemoryStorage storage)
    {
        storage = new MemoryStorage();
        JobStorage.Current = storage;
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddRouting();
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
                // Configure authorization policies from in-memory configuration
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authorization:Policies:CommandExecutor:Roles:0"] = "CommandExecutor"
                    })
                    .Build();
                services.AddSingleton<IConfiguration>(config);
                services.AddAuthorization(options =>
                {
                    var policiesSection = config.GetSection("Authorization:Policies");
                    foreach (var policySection in policiesSection.GetChildren())
                    {
                        var roles = policySection.GetSection("Roles").Get<string[]>() ?? Array.Empty<string>();
                        if (roles.Length > 0)
                        {
                            options.AddPolicy(policySection.Key, p => p.RequireRole(roles));
                        }
                    }
                    if (options.GetPolicy("CommandExecutor") is null)
                    {
                        options.AddPolicy("CommandExecutor", p => p.RequireRole("CommandExecutor"));
                    }
                });
                services.AddSingleton<IBackgroundJobClient>(new BackgroundJobClient(JobStorage.Current));
                services.AddSingleton<PluginManager>(new PluginManager(new ServiceCollection().BuildServiceProvider()));
                services.AddSingleton<CommandExecutor>(sp => new CommandExecutor(sp.GetRequiredService<PluginManager>(), Array.Empty<IResultPublisher>(), NullLogger<CommandExecutor>.Instance));
                services.AddSingleton<PayloadVerifier>(sp => new PayloadVerifier(config));
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(e =>
                {
                    e.MapCommandEndpoints();
                });
            });
        return new TestServer(builder);
    }

    [Fact]
    public async Task PutCommands_ModifiesAndReschedulesJob()
    {
        using var server = CreateServer(out var storage);
        var client = server.CreateClient();
        var createReq = new HttpRequestMessage(HttpMethod.Post, "/commands");
        createReq.Headers.Add("Test-Auth", "1");
        createReq.Headers.Add("Test-Role", "CommandExecutor");
        createReq.Content = JsonContent.Create(new { commands = new[] { "cmd1" }, payload = new { }, delay = TimeSpan.FromMinutes(1) });
        var createRes = await client.SendAsync(createReq);
        createRes.EnsureSuccessStatusCode();
        var created = await createRes.Content.ReadFromJsonAsync<EnqueuedCommandResult>();
        var oldId = created!.Id;

        var modifyReq = new HttpRequestMessage(HttpMethod.Put, $"/commands/{oldId}");
        modifyReq.Headers.Add("Test-Auth", "1");
        modifyReq.Headers.Add("Test-Role", "CommandExecutor");
        modifyReq.Content = JsonContent.Create(new { commands = new[] { "cmd2" }, payload = new { }, delay = TimeSpan.FromMinutes(2) });
        var modifyRes = await client.SendAsync(modifyReq);
        Assert.Equal(HttpStatusCode.OK, modifyRes.StatusCode);
        var modified = await modifyRes.Content.ReadFromJsonAsync<EnqueuedCommandResult>();
        var newId = modified!.Id;
        Assert.NotEqual(oldId, newId);

        var api = storage.GetMonitoringApi();
        Assert.Null(api.JobDetails(oldId));
        Assert.NotNull(api.JobDetails(newId));
    }
}
