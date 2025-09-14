using System.Text.Json.Serialization;

namespace BinanceServicePlugin;

public class AveragePrice
{
    [JsonPropertyName("mins")]
    public int Mins { get; set; }

    [JsonPropertyName("price")]
    public string Price { get; set; } = string.Empty;
}
