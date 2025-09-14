using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;
using BinanceServicePlugin;

namespace BinanceHandler;

public class GetTickerPriceCommand : ICommand
{
    public GetTickerPriceCommand(GetTickerPriceRequest request)
    {
        Request = request;
    }

    public GetTickerPriceRequest Request { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        var client = (BinanceClient)service.GetService();
        var price = await client.GetTickerPriceAsync(Request.Symbol, cancellationToken);
        var element = JsonSerializer.SerializeToElement(price);
        var status = price != null ? "success" : "not_found";
        return new OperationResult(element, status);
    }
}

