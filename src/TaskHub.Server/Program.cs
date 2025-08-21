using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Dashboard;
using TaskHub.Server;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer();
builder.Services.AddHttpClient("msgraph").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseDefaultCredentials = true });

builder.Services.AddSingleton<PluginManager>();
builder.Services.AddSingleton<CommandExecutor>();

var app = builder.Build();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new BasicAuthAuthorizationFilter("admin", "password") }
});

var plugins = app.Services.GetRequiredService<PluginManager>();
plugins.Load(Path.Combine(AppContext.BaseDirectory, "plugins"));

app.MapGet("/dlls", () => plugins.LoadedAssemblies);

app.MapPost("/commands/{handler}", (string handler, string? arg, IBackgroundJobClient client) =>
{
    var jobId = client.Enqueue<CommandExecutor>(exec => exec.Execute(handler, arg ?? string.Empty, CancellationToken.None));
    return Results.Ok(jobId);
});

app.MapPost("/commands/{id}/cancel", (string id, IBackgroundJobClient client) =>
{
    return client.Delete(id) ? Results.Ok() : Results.NotFound();
});

app.Run();
