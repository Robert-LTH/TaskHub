using Hangfire;
using Hangfire.MemoryStorage;
using TaskHub.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer();

builder.Services.AddSingleton<PluginManager>();
builder.Services.AddSingleton<CommandExecutor>();

var app = builder.Build();

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
