using System;
using System.IO;
using System.Threading;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Dashboard;
using System.Net.Http;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using TaskHub.Abstractions;
using TaskHub.Server;
using NSwag.AspNetCore;
using Hangfire.Console;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Configure logging to ensure plugin load messages are visible
builder.Logging.ClearProviders();
// Mirror ILogger to Hangfire job console
builder.Logging.AddProvider(new TaskHub.Server.PerformContextLoggerProvider());
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddHangfire(config => { config.UseConsole(); config.UseMemoryStorage(); });
builder.Services.AddHangfireServer();
builder.Services.AddHttpClient("msgraph").ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (OperatingSystem.IsWindows())
    {
        handler.UseDefaultCredentials = true;
    }
    return handler;
});



// Mirror Trace.Write/WriteLine into Hangfire job console when available
Trace.Listeners.Add(new TaskHub.Server.PerformContextTraceListener());
Trace.AutoFlush = true;
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddSingleton<PluginManager>();
builder.Services.AddSingleton<CommandExecutor>();
builder.Services.AddSingleton<PayloadVerifier>();
builder.Services.AddSingleton<ScriptsRepository>();
builder.Services.AddSingleton<ScriptSignatureVerifier>();
builder.Services.AddSingleton<IJobLogStore, JobLogStore>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();
builder.Services.AddSingleton<IReportingContainer, ReportingContainer>();
builder.Services.AddHostedService<ReportingService>();
// Support both Windows (Negotiate) and Bearer auth. Choose based on Authorization header.
builder.Services.AddAuthentication("smart")
    .AddPolicyScheme("smart", "smart", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return BearerTokenDefaults.AuthenticationScheme;
            return NegotiateDefaults.AuthenticationScheme;
        };
    })
    .AddNegotiate()
    .AddBearerToken();
builder.Services.AddSingleton<IClaimsTransformation, SidToRoleClaimsTransformer>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, LoggingAuthorizationMiddlewareResultHandler>();
builder.Services.AddAuthorization(options =>
{
    // 1) Preferred: Policy-based config mapping
    var policiesSection = builder.Configuration.GetSection("Authorization:Policies");
    foreach (var policySection in policiesSection.GetChildren())
    {
        var policyName = policySection.Key;
        if (options.GetPolicy(policyName) != null) continue; // avoid duplicates
        var roles = policySection.GetSection("Roles").Get<string[]>() ?? Array.Empty<string>();
        if (roles.Length > 0)
        {
            options.AddPolicy(policyName, p => p.RequireRole(roles));
        }
    }

    // 2) Back-compat: simple roles array mapped to CommandExecutor
    if (options.GetPolicy("CommandExecutor") is null)
    {
        var rolesArray = builder.Configuration.GetSection("Authorization:Roles").Get<string[]>();
        if (rolesArray != null && rolesArray.Length > 0)
        {
            options.AddPolicy("CommandExecutor", p => p.RequireRole(rolesArray));
        }
    }

    // 3) Fallback: ensure CommandExecutor exists with a sensible default
    if (options.GetPolicy("CommandExecutor") is null)
    {
        options.AddPolicy("CommandExecutor", p => p.RequireRole("CommandExecutor"));
    }
});

var jobHandlingMode = builder.Configuration.GetValue<string>("JobHandling:Mode");
if (string.Equals(jobHandlingMode, "WebSocket", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<WebSocketJobService>();
    builder.Services.AddSingleton<IResultPublisher>(sp => sp.GetRequiredService<WebSocketJobService>());
    builder.Services.AddSingleton<ILogPublisher>(sp => sp.GetRequiredService<WebSocketJobService>());
    builder.Services.AddHostedService(sp => sp.GetRequiredService<WebSocketJobService>());
}

// Pre-scan and register service plugins so handlers can inject them
try
{
    var pluginsRoot = Path.Combine(AppContext.BaseDirectory, "plugins");
    PluginCatalog.Register(builder.Services, builder.Configuration, pluginsRoot);
}
catch
{
}

var app = builder.Build();

app.UseOpenApi();
app.UseSwaggerUi();

app.UseAuthentication();
app.UseAuthorization();

var dashboardUser = builder.Configuration["Hangfire:Username"] ?? string.Empty;
var dashboardPass = builder.Configuration["Hangfire:Password"] ?? string.Empty;
var dashboardAuthFilters = new List<Hangfire.Dashboard.IDashboardAuthorizationFilter>();
// Always apply application authorization policy
dashboardAuthFilters.Add(new PolicyDashboardAuthorizationFilter("CommandExecutor"));
// Optionally allow basic auth if credentials provided
if (!string.IsNullOrWhiteSpace(dashboardUser) || !string.IsNullOrWhiteSpace(dashboardPass))
{
    dashboardAuthFilters.Add(new BasicAuthAuthorizationFilter(dashboardUser, dashboardPass));
}

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = dashboardAuthFilters.ToArray()
});

var pluginManager = app.Services.GetRequiredService<PluginManager>();
pluginManager.Load(Path.Combine(AppContext.BaseDirectory, "plugins"));

app.MapPluginEndpoints();
app.MapScriptEndpoints();

if (!string.Equals(jobHandlingMode, "WebSocket", StringComparison.OrdinalIgnoreCase))
{
    app.MapCommandEndpoints();
}

app.Run();

