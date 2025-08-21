using System;

namespace TaskHub.Abstractions;

/// <summary>
/// Represents the status of an enqueued command.
/// </summary>
/// <param name="Id">Identifier of the background job.</param>
/// <param name="Status">Current status/state of the job.</param>
public record CommandStatusResult(string Id, string Status);
