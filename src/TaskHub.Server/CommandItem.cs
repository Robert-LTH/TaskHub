using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskHub.Server;

public record CommandItem(
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("payload")] object? Payload);
