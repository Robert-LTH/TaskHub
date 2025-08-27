using System;
using System.Text.Json;

namespace TaskHub.Server;

public record CommandChainRequest(string[] Commands, JsonElement Payload, TimeSpan? Delay = null, string? Signature = null, string? CallbackConnectionId = null, string? RequestedBy = null);
