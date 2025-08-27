using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace TaskHub.Server;

public static class CommandEndpoints
{
    public static IEndpointRouteBuilder MapCommandEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/commands/available", (PluginManager manager) => manager.GetCommandInfos()).Produces<IEnumerable<CommandInfo>>();

        app.MapPost("/commands", (CommandChainRequest request, IBackgroundJobClient client, PayloadVerifier verifier, HttpContext context, ILogger<CommandEndpoints> logger, CommandExecutor executor) =>
        {
            if (!verifier.Verify(request.Payload, request.Signature))
            {
                return Results.Unauthorized();
            }
            var requestedBy = context.User.Identity?.Name ?? "anonymous";
            string jobId;
            if (request.Delay.HasValue)
            {
                jobId = client.Schedule<CommandExecutor>(exec => exec.ExecuteChain(request.Commands, request.Payload, requestedBy, null!, CancellationToken.None), request.Delay.Value);
            }
            else
            {
                jobId = client.Enqueue<CommandExecutor>(exec => exec.ExecuteChain(request.Commands, request.Payload, requestedBy, null!, CancellationToken.None));
            }
            executor.SetCallback(jobId, request.CallbackConnectionId);
            logger.LogInformation("User {User} scheduled job {JobId} for commands {Commands}", requestedBy, jobId, request.Commands);
            var enqueueTime = DateTimeOffset.UtcNow + (request.Delay ?? TimeSpan.Zero);
            return Results.Ok(new EnqueuedCommandResult(jobId, Array.Empty<ExecutedCommandResult>(), enqueueTime));
        }).Produces<EnqueuedCommandResult>();

        app.MapPost("/commands/recurring", (RecurringCommandChainRequest request, IBackgroundJobClient client, PayloadVerifier verifier, HttpContext context, ILogger<CommandEndpoints> logger, CommandExecutor executor) =>
        {
            if (!verifier.Verify(request.Payload, request.Signature))
            {
                return Results.Unauthorized();
            }
            var jobId = Guid.NewGuid().ToString();
            var requestedBy = context.User.Identity?.Name ?? "anonymous";
            client.Schedule(() => RecurringJob.AddOrUpdate<CommandExecutor>(jobId, exec => exec.ExecuteChain(request.Commands, request.Payload, requestedBy, null!, CancellationToken.None), request.CronExpression), request.Delay);
            executor.SetCallback(jobId, request.CallbackConnectionId);
            logger.LogInformation("User {User} scheduled recurring job {JobId} for commands {Commands}", requestedBy, jobId, request.Commands);
            return Results.Ok(new EnqueuedCommandResult(jobId, Array.Empty<ExecutedCommandResult>(), DateTimeOffset.UtcNow.Add(request.Delay)));
        }).Produces<EnqueuedCommandResult>();

        app.MapPost("/commands/{id}/cancel", (string id, IBackgroundJobClient client) =>
        {
            return client.Delete(id) ? Results.Ok() : Results.NotFound();
        });

        app.MapGet("/commands/{id}", (string id, CommandExecutor executor) =>
        {
            var jobDetails = JobStorage.Current.GetMonitoringApi().JobDetails(id);
            if (jobDetails == null)
            {
                return Results.NotFound();
            }

            var state = jobDetails.History.FirstOrDefault()?.StateName ?? "Unknown";
            var commands = executor.GetHistory(id)?.ToArray() ?? Array.Empty<ExecutedCommandResult>();
            return Results.Ok(new CommandStatusResult(id, state, commands));
        }).Produces<CommandStatusResult>();

        return app;
    }
}
