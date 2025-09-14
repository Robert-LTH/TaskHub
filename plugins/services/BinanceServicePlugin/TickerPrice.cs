using System.Text.Json.Serialization;

namespace BinanceServicePlugin;

public class TickerPrice
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }
}

