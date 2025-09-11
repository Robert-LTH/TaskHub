using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    private readonly ScriptsRepository _scripts;
    private readonly ILogger<CommandExecutor> _logger;

    private const int MaxHistoryEntries = 100;
    private readonly ConcurrentDictionary<string, List<ExecutedCommandResult>> _history = new();
    private readonly ConcurrentQueue<string> _historyOrder = new();
    private static readonly JsonDocument __NullDoc = JsonDocument.Parse("null");
    private static readonly JsonElement NullElement = __NullDoc.RootElement;

    private readonly ConcurrentDictionary<string, string?> _callbacks = new();

    public CommandExecutor(PluginManager manager, IEnumerable<IResultPublisher> publishers, ILogger<CommandExecutor> logger, ScriptsRepository scripts)
    {
        _manager = manager;
        _publishers = publishers;
        _logger = logger;
        _scripts = scripts;
    }

    // Backwards-compatible overload used by some tests
    public CommandExecutor(PluginManager manager, IEnumerable<IResultPublisher> publishers, ILogger<CommandExecutor> logger)
        : this(manager, publishers, logger, new ScriptsRepository())
    {
    }

    public void SetCallback(string jobId, string? connectionId) => _callbacks[jobId] = connectionId;
    private string? GetCallback(string jobId) => _callbacks.TryRemove(jobId, out var id) ? id : null;

    public IReadOnlyList<ExecutedCommandResult>? GetHistory(string jobId)
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
        var effectivePayload = ResolvePayload(command, payload);
        var result = await handler.ExecuteAsync(effectivePayload, service, _logger, token);
        var version = _manager.GetHandlerVersion(command);
        return (result, version);
    }

    private JsonElement ResolvePayload(string command, JsonElement payload)
    {
        try
        {
            if (!string.Equals(command, "powershell-script", StringComparison.OrdinalIgnoreCase))
            {
                return payload;
            }
            if (payload.ValueKind != JsonValueKind.Object)
            {
                return payload;
            }
            if (!payload.TryGetProperty("scriptId", out var idProp) || idProp.ValueKind != JsonValueKind.String)
            {
                return payload;
            }
            var id = idProp.GetString();
            if (string.IsNullOrWhiteSpace(id)) return payload;
            if (!_scripts.TryGet(id!, out var item) || item == null) return payload;

            // Build a new object preserving version/properties, replacing Script
            using var doc = JsonDocument.Parse(payload.GetRawText());
            var root = doc.RootElement;
            var obj = new Dictionary<string, object?>();
            if (root.TryGetProperty("version", out var ver) && ver.ValueKind == JsonValueKind.String)
            {
                obj["version"] = ver.GetString();
            }
            if (root.TryGetProperty("properties", out var props) && props.ValueKind != JsonValueKind.Undefined && props.ValueKind != JsonValueKind.Null)
            {
                obj["properties"] = JsonSerializer.Deserialize<Dictionary<string, object?>>(props.GetRawText());
            }
            obj["script"] = item.Content;
            var json = JsonSerializer.SerializeToUtf8Bytes(obj);
            using var newDoc = JsonDocument.Parse(json);
            return newDoc.RootElement.Clone();
        }
        catch
        {
            return payload;
        }
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

        var originalPayload = payload;
        var current = NullElement; // previous command output; null for first command
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
            var merged = BuildCommandPayload(command, originalPayload, current, lastResult);
            var cmd = handler.Create(merged);
            var version = _manager.GetHandlerVersion(command);

            if (cmd.WaitForPrevious)
            {
                await DrainAsync();
            }

            running.Add(Task.Run(async () =>
            {
                var ranAt = DateTimeOffset.UtcNow;
                // Pass logger into handler execution; PerformContext is bound globally
                var result = await handler.ExecuteAsync(merged, service, _logger, token);
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

        if (!string.Equals(lastResult.Result, "success", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(lastResult.Result);
        }

        return lastResult;
    }

    // Overload that accepts JSON text to ensure payload survives background job serialization.
    public async Task<OperationResult> ExecuteChain(IEnumerable<string> commands, string payloadJson, string? requestedBy, PerformContext context, CancellationToken token)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrEmpty(payloadJson) ? "null" : payloadJson);
        return await ExecuteChain(commands, doc.RootElement, requestedBy, context, token);
    }

    // New API: execute per-item commands with individual payloads
public async Task<OperationResult> ExecuteChain(IEnumerable<CommandItem> items, string? requestedBy, PerformContext context, CancellationToken token)
    {
        var prevCtx = JobLogContext.Current;
        JobLogContext.Current = context;
        try
        {
        var list = items as CommandItem[] ?? items.ToArray();
        var jobId = context?.BackgroundJob?.Id ?? Guid.NewGuid().ToString();
        _logger.LogInformation("Job {JobId} started for user {User} with commands {Commands}", jobId, requestedBy ?? "unknown", string.Join(", ", list.Select(i => i.Command)));

        JsonElement previous = NullElement;
        OperationResult lastResult = new OperationResult(null, "success");
        var results = new List<ExecutedCommandResult>();
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
            previous = last.Record.Output;
            running.Clear();
        }

        foreach (var item in list)
        {
            var handler = _manager.GetHandler(item.Command);
            if (handler == null)
            {
                await DrainAsync();
                var ranAtMissing = DateTimeOffset.UtcNow;
                var missing = new ExecutedCommandResult(item.Command, ranAtMissing, NullElement, null);
                results.Add(missing);
                lastResult = new OperationResult(null, $"Handler {item.Command} not found.");
                previous = NullElement;
                continue;
            }

            var service = _manager.GetService(handler.ServiceName);
            JsonElement payloadElement;
            try
            {
                if (item.Payload is JsonElement je)
                {
                    payloadElement = je;
                }
                else
                {
                    payloadElement = JsonSerializer.SerializeToElement(item.Payload ?? new object());
                }
            }
            catch
            {
                payloadElement = NullElement;
            }
            var merged = BuildCommandPayload(item.Command, payloadElement, previous, lastResult);
            var cmd = handler.Create(merged);
            var version = _manager.GetHandlerVersion(item.Command);

            if (cmd.WaitForPrevious)
            {
                await DrainAsync();
            }

            running.Add(Task.Run(async () =>
            {
                var ranAt = DateTimeOffset.UtcNow;
                var result = await handler.ExecuteAsync(merged, service, _logger, token);
                var output = result.Payload ?? NullElement;
                return (new ExecutedCommandResult(item.Command, ranAt, output, version), result);
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
            catch { }
        }

        _logger.LogInformation("Job {JobId} finished for user {User} with result {Result}", jobId, requestedBy ?? "unknown", lastResult.Result);

        if (!string.Equals(lastResult.Result, "success", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(lastResult.Result);
        }

        return lastResult;
    }
        finally
        {
            JobLogContext.Current = prevCtx;
        }
    }

    // Stringified variant for background job serialization safety
public async Task<OperationResult> ExecuteChain(string itemsJson, string? requestedBy, PerformContext context, CancellationToken token)
    {
        var prevCtx = JobLogContext.Current;
        JobLogContext.Current = context;
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrEmpty(itemsJson) ? "[]" : itemsJson);
            var items = doc.RootElement.Deserialize<CommandItem[]>() ?? Array.Empty<CommandItem>();
            return await ExecuteChain(items, requestedBy, context, token);
        }
        catch
        {
            return new OperationResult(null, "invalid-items-json");
        }
        finally
        {
            JobLogContext.Current = prevCtx;
        }
    }

    private JsonElement BuildCommandPayload(string command, JsonElement originalPayload, JsonElement previousOutput, OperationResult lastResult)
    {
        try
        {
            // Resolve any command-specific transformations (e.g., scriptId for powershell)
            var basePayload = ResolvePayload(command, originalPayload);

            object? prevObj = null;
            try
            {
                if (previousOutput.ValueKind != JsonValueKind.Undefined)
                {
                    prevObj = JsonSerializer.Deserialize<object?>(previousOutput.GetRawText());
                }
            }
            catch
            {
                prevObj = null;
            }

            if (basePayload.ValueKind == JsonValueKind.Object)
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(basePayload.GetRawText()) ?? new Dictionary<string, object?>();
                dict["previousOutput"] = prevObj;
                dict["previousResult"] = lastResult.Result;
                var json = JsonSerializer.SerializeToUtf8Bytes(dict);
                using var newDoc = JsonDocument.Parse(json);
                return newDoc.RootElement.Clone();
            }
            else
            {
                var wrapper = new Dictionary<string, object?>
                {
                    ["value"] = JsonSerializer.Deserialize<object?>(basePayload.GetRawText()),
                    ["previousOutput"] = prevObj,
                    ["previousResult"] = lastResult.Result,
                };
                var json = JsonSerializer.SerializeToUtf8Bytes(wrapper);
                using var newDoc = JsonDocument.Parse(json);
                return newDoc.RootElement.Clone();
            }
        }
        catch
        {
            // On any failure, fall back to original payload to avoid breaking commands
            return originalPayload;
        }
    }
}
