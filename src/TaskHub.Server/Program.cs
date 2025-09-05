using System;
using System.IO;
using System.Threading;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Dashboard;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using TaskHub.Abstractions;
using TaskHub.Server;
using NSwag.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure logging to ensure plugin load messages are visible
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddHangfire(config => config.UseMemoryStorage());
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

builder.Services.AddLogging();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddSingleton<PluginManager>();
builder.Services.AddSingleton<CommandExecutor>();
builder.Services.AddSingleton<PayloadVerifier>();
builder.Services.AddSingleton<ScriptsRepository>();
builder.Services.AddSingleton<ScriptSignatureVerifier>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();
builder.Services.AddSingleton<IReportingContainer, ReportingContainer>();
builder.Services.AddHostedService<ReportingService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CommandExecutor", policy => policy.RequireRole("CommandExecutor"));
});

var jobHandlingMode = builder.Configuration.GetValue<string>("JobHandling:Mode");
if (string.Equals(jobHandlingMode, "WebSocket", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<WebSocketJobService>();
    builder.Services.AddSingleton<IResultPublisher>(sp => sp.GetRequiredService<WebSocketJobService>());
    builder.Services.AddHostedService(sp => sp.GetRequiredService<WebSocketJobService>());
}

var app = builder.Build();

app.UseOpenApi();
app.UseSwaggerUi();

app.UseAuthentication();
app.UseAuthorization();

var dashboardUser = builder.Configuration["Hangfire:Username"] ?? string.Empty;
var dashboardPass = builder.Configuration["Hangfire:Password"] ?? string.Empty;
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new BasicAuthAuthorizationFilter(dashboardUser, dashboardPass) }
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

