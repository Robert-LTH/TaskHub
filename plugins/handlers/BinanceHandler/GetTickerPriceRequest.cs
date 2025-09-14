namespace BinanceHandler;

public class GetTickerPriceRequest
{
    public GetTickerPriceRequest(string symbol)
    {
        Symbol = symbol;
    }

    public string Symbol { get; }
}

