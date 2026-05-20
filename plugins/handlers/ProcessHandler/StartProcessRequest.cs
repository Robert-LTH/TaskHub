using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProcessHandler;

public class StartProcessRequest
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }

    [JsonPropertyName("argumentList")]
    public string[]? ArgumentList { get; set; }

    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; set; }

    [JsonPropertyName("environment")]
    public Dictionary<string, string?>? Environment { get; set; }

    [JsonPropertyName("timeoutMilliseconds")]
    public int? TimeoutMilliseconds { get; set; }
}
