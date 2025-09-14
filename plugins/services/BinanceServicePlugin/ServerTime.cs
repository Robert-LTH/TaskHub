using System.Text.Json.Serialization;

namespace BinanceServicePlugin;

public class ServerTime
{
    [JsonPropertyName("serverTime")]
    public long Time { get; set; }
}

