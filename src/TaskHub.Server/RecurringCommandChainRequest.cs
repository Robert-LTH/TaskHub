using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskHub.Server;

public class RecurringCommandChainRequest
{
    [JsonPropertyName("commands")] public CommandItem[] Commands { get; set; } = Array.Empty<CommandItem>();
    public string CronExpression { get; set; } = "* * * * *";
    public TimeSpan Delay { get; set; }
    public string? Signature { get; set; }
    public string? CallbackConnectionId { get; set; }
}

