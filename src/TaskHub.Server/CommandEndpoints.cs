using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        app.MapPost("/commands", async (HttpRequest httpRequest, IBackgroundJobClient client, PayloadVerifier verifier, HttpContext context, ILoggerFactory loggerFactory, CommandExecutor executor) =>
        {
            var logger = loggerFactory.CreateLogger("CommandEndpoints");
            // Buffer and parse body with more tolerant options
            httpRequest.EnableBuffering();
            string body;
            using (var reader = new StreamReader(httpRequest.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
            {
                body = await reader.ReadToEndAsync();
            }
            httpRequest.Body.Position = 0;
            System.Text.Json.JsonDocument doc;
            try
            {
                doc = System.Text.Json.JsonDocument.Parse(body, new System.Text.Json.JsonDocumentOptions { AllowTrailingCommas = true });
            }
            catch
            {
                return Results.BadRequest("Invalid JSON body");
            }
            using var _ = doc;
            var root = doc.RootElement;
            if (!CommandRequestParser.TryReadCommands(root, out var commands, out var itemsJson, out var parseError))
            {
                return Results.BadRequest(parseError);
            }
            var signature = root.TryGetProperty("signature", out var sigEl) && sigEl.ValueKind == System.Text.Json.JsonValueKind.String ? sigEl.GetString() : null;
            var verificationElement = System.Text.Json.JsonSerializer.SerializeToElement(commands);
            if (!verifier.Verify(verificationElement, signature))
            {
                return Results.Unauthorized();
            }
            var delay = CommandRequestParser.ReadDelay(root);
            var callbackId = CommandRequestParser.ReadString(root, "callbackConnectionId");

            var requestedBy = context.User.Identity?.Name ?? "anonymous";
            if (delay.HasValue)
            {
                var jobId = client.Schedule<CommandExecutor>(exec => exec.ExecuteChain(itemsJson, requestedBy, callbackId, null, CancellationToken.None), delay.Value);
                logger.LogInformation("User {User} scheduled job {JobId}", requestedBy, jobId);
                var enqueueTime = DateTimeOffset.UtcNow + delay.Value;
                return Results.Ok(new EnqueuedCommandResult(jobId, Array.Empty<ExecutedCommandResult>(), enqueueTime));
            }

            var result = await executor.ExecuteChainForApi(itemsJson, requestedBy, callbackId, context.RequestAborted);
            logger.LogInformation("User {User} executed command request {JobId} with result {Result}", requestedBy, result.Id, result.Status);
            return Results.Ok(result);
        }).RequireAuthorization("CommandExecutor")
            .Accepts<CommandChainRequest>("application/json")
          .Produces<CommandStatusResult>()
          .Produces<EnqueuedCommandResult>();

        app.MapPost("/commands/recurring", async (HttpRequest httpRequest, IBackgroundJobClient client, PayloadVerifier verifier, HttpContext context, ILoggerFactory loggerFactory, CommandExecutor executor) =>
        {
            var logger = loggerFactory.CreateLogger("CommandEndpoints");
            httpRequest.EnableBuffering();
            string body;
            using (var reader = new StreamReader(httpRequest.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
            {
                body = await reader.ReadToEndAsync();
            }
            httpRequest.Body.Position = 0;
            System.Text.Json.JsonDocument doc;
            try
            {
                doc = System.Text.Json.JsonDocument.Parse(body, new System.Text.Json.JsonDocumentOptions { AllowTrailingCommas = true });
            }
            catch
            {
                return Results.BadRequest("Invalid JSON body");
            }
            using var _ = doc;
            var root = doc.RootElement;
            if (!CommandRequestParser.TryReadCommands(root, out var commands, out var itemsJson, out var parseError))
            {
                return Results.BadRequest(parseError);
            }
            var signature = root.TryGetProperty("signature", out var sigEl) && sigEl.ValueKind == System.Text.Json.JsonValueKind.String ? sigEl.GetString() : null;
            var verificationElement = System.Text.Json.JsonSerializer.SerializeToElement(commands);
            if (!verifier.Verify(verificationElement, signature))
            {
                return Results.Unauthorized();
            }
            var cron = root.TryGetProperty("cronExpression", out var cronEl) && cronEl.ValueKind == System.Text.Json.JsonValueKind.String ? (cronEl.GetString() ?? "* * * * *") : "* * * * *";
            var delay = CommandRequestParser.ReadDelay(root) ?? TimeSpan.Zero;
            var callbackId = CommandRequestParser.ReadString(root, "callbackConnectionId");

            var jobId = Guid.NewGuid().ToString();
            var requestedBy = context.User.Identity?.Name ?? "anonymous"; 
            client.Schedule(() => RecurringJob.AddOrUpdate<CommandExecutor>(
                jobId,
                exec => exec.ExecuteChain(itemsJson, requestedBy, callbackId, null, CancellationToken.None),
                cron,
                new RecurringJobOptions()),
                delay);
            logger.LogInformation("User {User} scheduled recurring job {JobId}", requestedBy, jobId);
            return Results.Ok(new EnqueuedCommandResult(jobId, Array.Empty<ExecutedCommandResult>(), DateTimeOffset.UtcNow.Add(delay)));
        }).RequireAuthorization("CommandExecutor")
          .Produces<EnqueuedCommandResult>();

        app.MapPut("/commands/{id}", async (string id, HttpRequest httpRequest, IBackgroundJobClient client, PayloadVerifier verifier, HttpContext context, ILoggerFactory loggerFactory, CommandExecutor executor) =>
        {
            var logger = loggerFactory.CreateLogger("CommandEndpoints");
            httpRequest.EnableBuffering();
            string body;
            using (var reader = new StreamReader(httpRequest.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
            {
                body = await reader.ReadToEndAsync();
            }
            httpRequest.Body.Position = 0;
            System.Text.Json.JsonDocument doc;
            try
            {
                doc = System.Text.Json.JsonDocument.Parse(body, new System.Text.Json.JsonDocumentOptions { AllowTrailingCommas = true });
            }
            catch
            {
                return Results.BadRequest("Invalid JSON body");
            }
            using var _ = doc;
            var root = doc.RootElement;
            if (!CommandRequestParser.TryReadCommands(root, out var commands, out var itemsJson, out var parseError))
            {
                return Results.BadRequest(parseError);
            }
            var signature = root.TryGetProperty("signature", out var sigEl) && sigEl.ValueKind == System.Text.Json.JsonValueKind.String ? sigEl.GetString() : null;
            var verificationElement = System.Text.Json.JsonSerializer.SerializeToElement(commands);
            if (!verifier.Verify(verificationElement, signature))
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
            var callbackId = CommandRequestParser.ReadString(root, "callbackConnectionId");
            string jobId;
            var delay = CommandRequestParser.ReadDelay(root);
            if (delay.HasValue)
            {
                jobId = client.Schedule<CommandExecutor>(exec => exec.ExecuteChain(itemsJson, requestedBy, callbackId, null, CancellationToken.None), delay.Value);
            }
            else
            {
                jobId = client.Enqueue<CommandExecutor>(exec => exec.ExecuteChain(itemsJson, requestedBy, callbackId, null, CancellationToken.None));
            }
            logger.LogInformation("User {User} modified job {OldJob} to new job {JobId}", requestedBy, id, jobId);
            var enqueueTime = DateTimeOffset.UtcNow + (delay ?? TimeSpan.Zero);
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

        app.MapGet("/commands/{id}/logs", (string id, [FromServices] IJobLogStore store) =>
        {
            var logs = store.GetLogs(id);
            return logs is null ? Results.NotFound() : Results.Ok(logs);
        }).RequireAuthorization("CommandExecutor")
          .Produces<string[]>();

        return app;
    }
}
