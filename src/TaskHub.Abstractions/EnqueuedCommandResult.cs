using System;

namespace TaskHub.Abstractions;

/// <summary>
/// Result returned when a command or chain of commands is enqueued.
/// </summary>
/// <param name="Id">Identifier of the background job.</param>
/// <param name="Commands">Commands that were executed.</param>
/// <param name="EnqueuedAt">Timestamp when the job was enqueued.</param>
public record EnqueuedCommandResult(string Id, ExecutedCommandResult[] Commands, DateTimeOffset EnqueuedAt);
