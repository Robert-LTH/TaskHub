namespace BinanceHandler;

public class GetConvertQuoteRequest
{
    public string FromAsset { get; set; } = string.Empty;
    public string ToAsset { get; set; } = string.Empty;
    public string? FromAmount { get; set; }
    public string? ToAmount { get; set; }
}
