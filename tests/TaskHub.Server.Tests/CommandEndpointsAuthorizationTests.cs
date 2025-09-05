using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
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
using TaskHub.Abstractions;
using TaskHub.Server;
using Xunit;

namespace TaskHub.Server.Tests;

public class CommandEndpointsAuthorizationTests
{
    private TestServer CreateServer()
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
                services.AddAuthorization(options =>
                {
                    options.AddPolicy("CommandExecutor", p => p.RequireRole("CommandExecutor"));
                });
                services.AddSingleton<IBackgroundJobClient>(new BackgroundJobClient(new MemoryStorage()));
                services.AddSingleton<PluginManager>(new PluginManager(new ServiceCollection().BuildServiceProvider()));
                services.AddSingleton<CommandExecutor>(sp => new CommandExecutor(sp.GetRequiredService<PluginManager>(), Array.Empty<IResultPublisher>(), NullLogger<CommandExecutor>.Instance));
                services.AddSingleton<PayloadVerifier>(sp => new PayloadVerifier(new ConfigurationBuilder().Build()));
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
    public async Task PostCommands_EnforcesRoles()
    {
        using var server = CreateServer();
        var client = server.CreateClient();
        var body = new { commands = new[] { "echo" }, payload = new { } };

        // Unauthenticated request
        var res1 = await client.PostAsJsonAsync("/commands", body);
        Assert.Equal(HttpStatusCode.Unauthorized, res1.StatusCode);

        // Authenticated without role
        var req2 = new HttpRequestMessage(HttpMethod.Post, "/commands");
        req2.Headers.Add("Test-Auth", "1");
        req2.Content = JsonContent.Create(body);
        var res2 = await client.SendAsync(req2);
        Assert.Equal(HttpStatusCode.Forbidden, res2.StatusCode);

        // Authenticated with role
        var req3 = new HttpRequestMessage(HttpMethod.Post, "/commands");
        req3.Headers.Add("Test-Auth", "1");
        req3.Headers.Add("Test-Role", "CommandExecutor");
        req3.Content = JsonContent.Create(body);
        var res3 = await client.SendAsync(req3);
        Assert.Equal(HttpStatusCode.OK, res3.StatusCode);
    }
}

internal class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Test-Auth"))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing"));
        }
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, "test") };
        if (Request.Headers.TryGetValue("Test-Role", out var roles))
        {
            foreach (var role in roles.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
