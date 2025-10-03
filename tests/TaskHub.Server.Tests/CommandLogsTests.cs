using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using System.Security.Claims;
using TaskHub.Abstractions;
using TaskHub.Server;
using Xunit;
using System.Threading.Tasks;

namespace TaskHub.Server.Tests;

public class CommandLogsTests
{
    private TestServer CreateServer()
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddRouting();
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
                var config = new ConfigurationBuilder().Build();
                services.AddSingleton<IConfiguration>(config);
                services.AddAuthorization(options =>
                {
                    options.AddPolicy("CommandExecutor", p => p.RequireRole("CommandExecutor"));
                });
                services.AddSingleton<IBackgroundJobClient>(new BackgroundJobClient(new MemoryStorage()));
                services.AddSingleton<PluginManager>(new PluginManager(new ServiceCollection().BuildServiceProvider()));
                services.AddSingleton<IJobLogStore, JobLogStore>();
                services.AddSingleton<CommandExecutor>(sp => new CommandExecutor(sp.GetRequiredService<PluginManager>(), Array.Empty<IResultPublisher>(), NullLoggerFactory.Instance));
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
    public async Task GetLogs_ReturnsStoredLogs()
    {
        using var server = CreateServer();
        var store = server.Services.GetRequiredService<IJobLogStore>();
        store.Append("job1", "hello");
        var client = server.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/commands/job1/logs");
        req.Headers.Add("Test-Auth", "1");
        req.Headers.Add("Test-Role", "CommandExecutor");
        var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var logs = await res.Content.ReadFromJsonAsync<string[]>();
        Assert.Contains("hello", logs);
        return;
    }
}

//internal class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
//{
//    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
//        : base(options, logger, encoder, clock) { }

//    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
//    {
//        if (!Request.Headers.ContainsKey("Test-Auth"))
//        {
//            return Task.FromResult(AuthenticateResult.Fail("Missing"));
//        }
//        var claims = new List<Claim> { new Claim(ClaimTypes.Name, "test") };
//        if (Request.Headers.TryGetValue("Test-Role", out var roles))
//        {
//            foreach (var role in roles.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries))
//            {
//                claims.Add(new Claim(ClaimTypes.Role, role));
//            }
//        }
//        var identity = new ClaimsIdentity(claims, Scheme.Name);
//        var principal = new ClaimsPrincipal(identity);
//        var ticket = new AuthenticationTicket(principal, Scheme.Name);
//        return Task.FromResult(AuthenticateResult.Success(ticket));
//    }
//}
