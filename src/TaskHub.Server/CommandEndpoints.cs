using System;
using System.Linq;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TaskHub.Abstractions;

namespace TaskHub.Server;

public static class CommandEndpoints
{
    public static IEndpointRouteBuilder MapCommandEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/commands", (CommandChainRequest request, IBackgroundJobClient client) =>
        {
            var jobId = client.Enqueue<CommandExecutor>(exec => exec.ExecuteChain(request.Commands, request.Payload, null!, CancellationToken.None));
            return Results.Ok(new EnqueuedCommandResult(jobId, Array.Empty<ExecutedCommandResult>(), DateTimeOffset.UtcNow));
        }).Produces<EnqueuedCommandResult>();

        app.MapPost("/commands/recurring", (RecurringCommandChainRequest request, IBackgroundJobClient client) =>
        {
            var jobId = Guid.NewGuid().ToString();
            client.Schedule(() => RecurringJob.AddOrUpdate<CommandExecutor>(jobId, exec => exec.ExecuteChain(request.Commands, request.Payload, null!, CancellationToken.None), request.CronExpression), request.Delay);
            return Results.Ok(new EnqueuedCommandResult(jobId, Array.Empty<ExecutedCommandResult>(), DateTimeOffset.UtcNow.Add(request.Delay)));
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

        return app;
    }
}
