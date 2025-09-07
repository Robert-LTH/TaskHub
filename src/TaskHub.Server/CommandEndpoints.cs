using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace TaskHub.Server;

public static class CommandEndpoints
{
    public static IEndpointRouteBuilder MapCommandEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/commands/available", (PluginManager manager) => manager.GetCommandInfos())
           .RequireAuthorization("CommandExecutor")
           .Produces<IEnumerable<CommandInfo>>();

        app.MapPost("/commands", (CommandChainRequest request, IBackgroundJobClient client, PayloadVerifier verifier, HttpContext context, ILoggerFactory loggerFactory, CommandExecutor executor) =>
        {
            var logger = loggerFactory.CreateLogger("CommandEndpoints");
            if (!verifier.Verify(request.Payload, request.Signature))
            {
                return Results.Unauthorized();
            }
            var requestedBy = context.User.Identity?.Name ?? "anonymous";
            string jobId;
            var payloadJson = request.Payload.GetRawText();
            if (request.Delay.HasValue)
            {
                jobId = client.Schedule<CommandExecutor>(exec => exec.ExecuteChain(request.Commands, payloadJson, requestedBy, null!, CancellationToken.None), request.Delay.Value);
            }
            else
            {
                jobId = client.Enqueue<CommandExecutor>(exec => exec.ExecuteChain(request.Commands, payloadJson, requestedBy, null!, CancellationToken.None));
            }
            executor.SetCallback(jobId, request.CallbackConnectionId);
            logger.LogInformation("User {User} scheduled job {JobId} for commands {Commands}", requestedBy, jobId, request.Commands);
            var enqueueTime = DateTimeOffset.UtcNow + (request.Delay ?? TimeSpan.Zero);
            return Results.Ok(new EnqueuedCommandResult(jobId, Array.Empty<ExecutedCommandResult>(), enqueueTime));
        }).RequireAuthorization("CommandExecutor")
          .Produces<EnqueuedCommandResult>();

        app.MapPost("/commands/recurring", (RecurringCommandChainRequest request, IBackgroundJobClient client, PayloadVerifier verifier, HttpContext context, ILoggerFactory loggerFactory, CommandExecutor executor) =>
        {
            var logger = loggerFactory.CreateLogger("CommandEndpoints");
            if (!verifier.Verify(request.Payload, request.Signature))
            {
                return Results.Unauthorized();
            }
            var jobId = Guid.NewGuid().ToString();
            var requestedBy = context.User.Identity?.Name ?? "anonymous"; 
            var payloadJsonRecurring = request.Payload.GetRawText();
            client.Schedule(() => RecurringJob.AddOrUpdate<CommandExecutor>(
                jobId,
                exec => exec.ExecuteChain(request.Commands, payloadJsonRecurring, requestedBy, null!, CancellationToken.None),
                request.CronExpression,
                new RecurringJobOptions()),
                request.Delay);
            executor.SetCallback(jobId, request.CallbackConnectionId);
            logger.LogInformation("User {User} scheduled recurring job {JobId} for commands {Commands}", requestedBy, jobId, request.Commands);
            return Results.Ok(new EnqueuedCommandResult(jobId, Array.Empty<ExecutedCommandResult>(), DateTimeOffset.UtcNow.Add(request.Delay)));
        }).RequireAuthorization("CommandExecutor")
          .Produces<EnqueuedCommandResult>();

        app.MapPut("/commands/{id}", (string id, CommandChainRequest request, IBackgroundJobClient client, PayloadVerifier verifier, HttpContext context, ILoggerFactory loggerFactory, CommandExecutor executor) =>
        {
            var logger = loggerFactory.CreateLogger("CommandEndpoints");
            if (!verifier.Verify(request.Payload, request.Signature))
            {
                return Results.Unauthorized();
            }

            var jobDetails = JobStorage.Current.GetMonitoringApi().JobDetails(id);
            if (jobDetails == null)
            {
                return Results.NotFound();
            }

            client.Delete(id);
            var requestedBy = context.User.Identity?.Name ?? "anonymous";
            var payloadJson = request.Payload.GetRawText();
            string jobId;
            if (request.Delay.HasValue)
            {
                jobId = client.Schedule<CommandExecutor>(exec => exec.ExecuteChain(request.Commands, payloadJson, requestedBy, null!, CancellationToken.None), request.Delay.Value);
            }
            else
            {
                jobId = client.Enqueue<CommandExecutor>(exec => exec.ExecuteChain(request.Commands, payloadJson, requestedBy, null!, CancellationToken.None));
            }
            executor.SetCallback(jobId, request.CallbackConnectionId);
            logger.LogInformation("User {User} modified job {OldJob} to new job {JobId} for commands {Commands}", requestedBy, id, jobId, request.Commands);
            var enqueueTime = DateTimeOffset.UtcNow + (request.Delay ?? TimeSpan.Zero);
            return Results.Ok(new EnqueuedCommandResult(jobId, Array.Empty<ExecutedCommandResult>(), enqueueTime));
        }).RequireAuthorization("CommandExecutor")
          .Produces<EnqueuedCommandResult>();

        app.MapPost("/commands/{id}/cancel", (string id, IBackgroundJobClient client) =>
        {
            return client.Delete(id) ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization("CommandExecutor");

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
        }).RequireAuthorization("CommandExecutor")
          .Produces<CommandStatusResult>();

        return app;
    }
}
