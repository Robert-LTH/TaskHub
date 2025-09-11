using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskHub.Server;

public class CommandChainRequest
{
    [JsonPropertyName("commands")] public CommandItem[] Commands { get; set; } = Array.Empty<CommandItem>();
    public TimeSpan? Delay { get; set; }
    public string? Signature { get; set; }
    public string? CallbackConnectionId { get; set; }
    public string? RequestedBy { get; set; }
}
