using System.Text.Json;

namespace TaskHub.Server;

public record CommandChainRequest(string[] Commands, JsonElement Payload, string? Signature = null);
