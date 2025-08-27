using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;
using Hangfire.Server;
using Microsoft.Extensions.Logging;

namespace TaskHub.Server;

public class CommandExecutor
{
    private readonly PluginManager _manager;
    private readonly IEnumerable<IResultPublisher> _publishers;
    private readonly ILogger<CommandExecutor> _logger;

    private const int MaxHistoryEntries = 100;
    private static readonly ConcurrentDictionary<string, List<ExecutedCommandResult>> _history = new();
    private static readonly ConcurrentQueue<string> _historyOrder = new();
    private static readonly JsonElement NullElement = JsonDocument.Parse("null").RootElement;

    private static readonly ConcurrentDictionary<string, string?> _callbacks = new();

    public CommandExecutor(PluginManager manager, IEnumerable<IResultPublisher> publishers, ILogger<CommandExecutor> logger)
    {
        _manager = manager;
        _publishers = publishers;
        _logger = logger;
    }

    public static void SetCallback(string jobId, string? connectionId) => _callbacks[jobId] = connectionId;
    private static string? GetCallback(string jobId) => _callbacks.TryRemove(jobId, out var id) ? id : null;

    public static IReadOnlyList<ExecutedCommandResult>? GetHistory(string jobId)
    {
        return _history.TryRemove(jobId, out var list) ? list : null;
    }

    private async Task<(OperationResult Result, string? Version)> ExecuteInternal(string command, JsonElement payload, CancellationToken token)
    {
        var handler = _manager.GetHandler(command);
        if (handler == null)
        {
            return (new OperationResult(null, $"Handler {command} not found."), null);
        }

        var service = _manager.GetService(handler.ServiceName);
        var result = await handler.ExecuteAsync(payload, service, token);
        var version = _manager.GetHandlerVersion(command);
        return (result, version);
    }

    public async Task<OperationResult> Execute(string command, JsonElement payload, CancellationToken token)
    {
        var (result, _) = await ExecuteInternal(command, payload, token);
        return result;
    }

    public async Task<OperationResult> ExecuteChain(IEnumerable<string> commands, JsonElement payload, string? requestedBy, PerformContext context, CancellationToken token)
    {
        var commandList = commands as string[] ?? commands.ToArray();
        var jobId = context?.BackgroundJob?.Id ?? Guid.NewGuid().ToString();
        _logger.LogInformation("Job {JobId} started for user {User} with commands {Commands}", jobId, requestedBy ?? "unknown", string.Join(", ", commandList));

        var current = payload;
        var results = new List<ExecutedCommandResult>();
        OperationResult lastResult = new OperationResult(null, "success");
        var running = new List<Task<(ExecutedCommandResult Record, OperationResult Result)>>();

        async Task DrainAsync()
        {
            if (running.Count == 0) return;
            var finished = await Task.WhenAll(running);
            foreach (var item in finished)
            {
                results.Add(item.Record);
            }
            var last = finished[^1];
            lastResult = last.Result;
            current = last.Record.Output;
            running.Clear();
        }

        foreach (var command in commandList)
        {
            var handler = _manager.GetHandler(command);
            if (handler == null)
            {
                await DrainAsync();
                var ranAtMissing = DateTimeOffset.UtcNow;
                var missing = new ExecutedCommandResult(command, ranAtMissing, NullElement, null);
                results.Add(missing);
                lastResult = new OperationResult(null, $"Handler {command} not found.");
                current = NullElement;
                continue;
            }

            var service = _manager.GetService(handler.ServiceName);
            var cmd = handler.Create(current);
            var version = _manager.GetHandlerVersion(command);

            if (cmd.WaitForPrevious)
            {
                await DrainAsync();
            }

            running.Add(Task.Run(async () =>
            {
                var ranAt = DateTimeOffset.UtcNow;
                var result = await cmd.ExecuteAsync(service, token);
                var output = result.Payload ?? NullElement;
                return (new ExecutedCommandResult(command, ranAt, output, version), result);
            }, token));
        }

        await DrainAsync();

        _history[jobId] = results;
        _historyOrder.Enqueue(jobId);
        while (_historyOrder.Count > MaxHistoryEntries && _historyOrder.TryDequeue(out var oldId))
        {
            _history.TryRemove(oldId, out _);
        }

        var statusResult = new CommandStatusResult(jobId, lastResult.Result, results.ToArray());
        var callbackId = GetCallback(jobId);
        foreach (var publisher in _publishers)
        {
            try
            {
                await publisher.PublishResultAsync(statusResult, callbackId, token);
            }
            catch
            {
                // ignore publishing errors to avoid failing the job
            }
        }

        _logger.LogInformation("Job {JobId} finished for user {User} with result {Result}", jobId, requestedBy ?? "unknown", lastResult.Result);

        return lastResult;
    }
}
