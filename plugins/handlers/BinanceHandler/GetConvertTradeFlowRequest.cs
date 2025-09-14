namespace BinanceHandler;

public class GetConvertTradeFlowRequest
{
    public long StartTime { get; set; }
    public long EndTime { get; set; }
    public int? Page { get; set; }
    public int? Limit { get; set; }
}
