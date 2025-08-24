using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Dashboard;
using Microsoft.Extensions.Configuration;
using TaskHub.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer();
builder.Services.AddHttpClient("msgraph").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseDefaultCredentials = true });

builder.Services.AddLogging();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddSingleton<PluginManager>();
builder.Services.AddSingleton<CommandExecutor>();
builder.Services.AddOpenApiDocument();

var jobHandlingMode = builder.Configuration.GetValue<string>("JobHandling:Mode");
if (string.Equals(jobHandlingMode, "WebSocket", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHostedService<WebSocketJobService>();
}

var app = builder.Build();

app.UseOpenApi();
app.UseSwaggerUi3();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new BasicAuthAuthorizationFilter("admin", "password") }
});

var pluginManager = app.Services.GetRequiredService<PluginManager>();
pluginManager.Load(Path.Combine(AppContext.BaseDirectory, "plugins"));

app.MapPluginEndpoints();

if (!string.Equals(jobHandlingMode, "WebSocket", StringComparison.OrdinalIgnoreCase))
{
    app.MapCommandEndpoints();
}

app.Run();

