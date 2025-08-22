using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Dashboard;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Linq;
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

var payload = JsonSerializer.Deserialize<JsonElement>("{}");
RecurringJob.AddOrUpdate<CommandExecutor>(
    "clean-temp",
    exec => exec.Execute("clean-temp", payload, CancellationToken.None),
    Cron.HourInterval(7));

app.MapGet("/dlls", () => plugins.LoadedAssemblies);

app.MapPost("/commands", (CommandChainRequest request, IBackgroundJobClient client) =>
{
    var jobId = client.Enqueue<CommandExecutor>(exec => exec.ExecuteChain(request.Commands, request.Payload, null!, CancellationToken.None));
    return Results.Ok(new EnqueuedCommandResult(jobId, Array.Empty<ExecutedCommandResult>(), DateTimeOffset.UtcNow));
}).Produces<EnqueuedCommandResult>();

app.MapPost("/commands/{id}/cancel", (string id, IBackgroundJobClient client) =>
{
    return client.Delete(id) ? Results.Ok() : Results.NotFound();
});

app.MapGet("/commands/{id}", (string id) =>
{
    var jobDetails = JobStorage.Current.GetMonitoringApi().JobDetails(id);
    if (jobDetails == null)
    {
        return Results.NotFound();
    }

    var state = jobDetails.History.FirstOrDefault()?.StateName ?? "Unknown";
    var commands = CommandExecutor.GetHistory(id)?.ToArray() ?? Array.Empty<ExecutedCommandResult>();
    return Results.Ok(new CommandStatusResult(id, state, commands));
}).Produces<CommandStatusResult>();

app.Run();

