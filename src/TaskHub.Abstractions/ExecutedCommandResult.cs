using System;
using System.Text.Json;

namespace TaskHub.Abstractions;

/// <summary>
/// Represents a command that has been executed.
/// </summary>
/// <param name="Command">Name of the executed command.</param>
/// <param name="RanAt">Timestamp when the command was executed.</param>
/// <param name="Output">Output returned by the command.</param>
/// <param name="PluginVersion">Version of the plugin that handled the command.</param>
public record ExecutedCommandResult(string Command, DateTimeOffset RanAt, JsonElement Output, string? PluginVersion = null);

