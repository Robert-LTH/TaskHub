using System;

namespace TaskHub.Abstractions;

public record EnqueuedCommandResult(string Id, string Command, DateTimeOffset EnqueuedAt);
