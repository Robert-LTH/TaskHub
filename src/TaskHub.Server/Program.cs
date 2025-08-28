using System;
using System.IO;
using System.Threading;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Dashboard;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using TaskHub.Abstractions;
using TaskHub.Server;
using NSwag.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer();
builder.Services.AddHttpClient("msgraph").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseDefaultCredentials = true });

builder.Services.AddLogging();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddSingleton<PluginManager>();
builder.Services.AddSingleton<CommandExecutor>();
builder.Services.AddSingleton<PayloadVerifier>();
builder.Services.AddOpenApiDocument();
builder.Services.AddSingleton<IReportingContainer, ReportingContainer>();
builder.Services.AddHostedService<ReportingService>();

var jobHandlingMode = builder.Configuration.GetValue<string>("JobHandling:Mode");
if (string.Equals(jobHandlingMode, "WebSocket", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<WebSocketJobService>();
    builder.Services.AddSingleton<IResultPublisher>(sp => sp.GetRequiredService<WebSocketJobService>());
    builder.Services.AddHostedService(sp => sp.GetRequiredService<WebSocketJobService>());
}

var app = builder.Build();

app.UseOpenApi();
app.UseSwaggerUi3();

var dashboardUser = builder.Configuration["Hangfire:Username"] ?? string.Empty;
var dashboardPass = builder.Configuration["Hangfire:Password"] ?? string.Empty;
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new BasicAuthAuthorizationFilter(dashboardUser, dashboardPass) }
});

var pluginManager = app.Services.GetRequiredService<PluginManager>();
pluginManager.Load(Path.Combine(AppContext.BaseDirectory, "plugins"));

app.MapPluginEndpoints();

if (!string.Equals(jobHandlingMode, "WebSocket", StringComparison.OrdinalIgnoreCase))
{
    app.MapCommandEndpoints();
}

app.Run();

