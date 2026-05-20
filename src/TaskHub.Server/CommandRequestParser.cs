using System;
using System.Collections.Generic;
using System.Text.Json;

namespace TaskHub.Server;

internal static class CommandRequestParser
{
    private static readonly JsonDocument EmptyObjectDocument = JsonDocument.Parse("{}");

    public static bool TryReadCommands(JsonElement root, out CommandItem[] commands, out string itemsJson, out string? error)
    {
        commands = Array.Empty<CommandItem>();
        itemsJson = "[]";
        error = null;

        if (!root.TryGetProperty("commands", out var commandsElement) || commandsElement.ValueKind != JsonValueKind.Array)
        {
            error = "Commands array is required";
            return false;
        }

        var hasSharedPayload = root.TryGetProperty("payload", out var sharedPayload);
        var parsed = new List<CommandItem>();
        foreach (var commandElement in commandsElement.EnumerateArray())
        {
            if (commandElement.ValueKind == JsonValueKind.String)
            {
                var command = commandElement.GetString();
                if (string.IsNullOrWhiteSpace(command))
                {
                    error = "Command name cannot be empty";
                    return false;
                }

                parsed.Add(new CommandItem(command!, ClonePayload(hasSharedPayload, sharedPayload)));
                continue;
            }

            if (commandElement.ValueKind != JsonValueKind.Object ||
                !commandElement.TryGetProperty("command", out var commandNameElement) ||
                commandNameElement.ValueKind != JsonValueKind.String)
            {
                error = "Each command must be a string or an object with a command property";
                return false;
            }

            var commandName = commandNameElement.GetString();
            if (string.IsNullOrWhiteSpace(commandName))
            {
                error = "Command name cannot be empty";
                return false;
            }

            var payload = commandElement.TryGetProperty("payload", out var itemPayload)
                ? itemPayload.Clone()
                : ClonePayload(hasSharedPayload, sharedPayload);

            parsed.Add(new CommandItem(commandName!, payload));
        }

        commands = parsed.ToArray();
        itemsJson = JsonSerializer.Serialize(commands);
        return true;
    }

    public static string? ReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }

    public static TimeSpan? ReadDelay(JsonElement root)
    {
        if (!root.TryGetProperty("delay", out var delayElement))
        {
            return null;
        }

        if (delayElement.ValueKind == JsonValueKind.String && TimeSpan.TryParse(delayElement.GetString(), out var timeSpan))
        {
            return timeSpan;
        }

        if (delayElement.ValueKind == JsonValueKind.Number && delayElement.TryGetInt64(out var milliseconds))
        {
            return TimeSpan.FromMilliseconds(milliseconds);
        }

        return null;
    }

    private static JsonElement ClonePayload(bool hasSharedPayload, JsonElement sharedPayload)
    {
        return hasSharedPayload ? sharedPayload.Clone() : EmptyObjectDocument.RootElement.Clone();
    }
}
