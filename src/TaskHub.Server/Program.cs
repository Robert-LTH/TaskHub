using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Dashboard;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using TaskHub.Server;
using TaskHub.Abstractions;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer();
builder.Services.AddHttpClient("msgraph").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseDefaultCredentials = true });

builder.Services.AddLogging();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddLogging();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddSingleton<PluginManager>();
builder.Services.AddSingleton<CommandExecutor>();
builder.Services.AddOpenApiDocument();

var app = builder.Build();

app.UseOpenApi();
app.UseSwaggerUi3();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new BasicAuthAuthorizationFilter("admin", "password") }
});

var plugins = app.Services.GetRequiredService<PluginManager>();
plugins.Load(Path.Combine(AppContext.BaseDirectory, "plugins"));

app.MapGet("/dlls", () => plugins.LoadedAssemblies);

app.MapPost("/commands/{command}", (string command, JsonElement payload, IBackgroundJobClient client) =>
{
    var jobId = client.Enqueue<CommandExecutor>(exec => exec.Execute(command, payload, CancellationToken.None));
    return Results.Ok(new EnqueuedCommandResult(jobId, command, DateTimeOffset.UtcNow));
}).Produces<EnqueuedCommandResult>();

app.MapPost("/commands/{id}/cancel", (string id, IBackgroundJobClient client) =>
{
    return client.Delete(id) ? Results.Ok() : Results.NotFound();
});

app.Run();

