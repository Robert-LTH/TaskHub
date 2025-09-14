using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;
using BinanceServicePlugin;

namespace BinanceHandler;

public class GetAveragePriceCommand : ICommand
{
    public GetAveragePriceCommand(GetAveragePriceRequest request)
    {
        Request = request;
    }

    public GetAveragePriceRequest Request { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        var client = (BinanceClient)service.GetService();
        var price = await client.GetAveragePriceAsync(Request.Symbol, cancellationToken);
        var element = JsonSerializer.SerializeToElement(price);
        var status = price != null ? "success" : "not_found";
        return new OperationResult(element, status);
    }
}
