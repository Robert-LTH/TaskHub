using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;
using BinanceServicePlugin;

namespace BinanceHandler;

public class GetConvertQuoteCommand : ICommand
{
    public GetConvertQuoteCommand(GetConvertQuoteRequest request)
    {
        Request = request;
    }

    public GetConvertQuoteRequest Request { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        var client = (BinanceClient)service.GetService();
        var quote = await client.GetConvertQuoteAsync(
            Request.FromAsset,
            Request.ToAsset,
            Request.FromAmount,
            Request.ToAmount,
            cancellationToken);
        var element = quote ?? JsonDocument.Parse("null").RootElement;
        var status = quote.HasValue ? "success" : "not_found";
        return new OperationResult(element, status);
    }
}
