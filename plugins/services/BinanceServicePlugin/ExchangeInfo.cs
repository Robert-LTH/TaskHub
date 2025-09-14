using System.Text.Json.Serialization;

namespace BinanceServicePlugin;

public class ExchangeInfo
{
    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    [JsonPropertyName("serverTime")]
    public long ServerTime { get; set; }
}

