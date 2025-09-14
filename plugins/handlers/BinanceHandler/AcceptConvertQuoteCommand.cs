using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;
using BinanceServicePlugin;

namespace BinanceHandler;

public class AcceptConvertQuoteCommand : ICommand
{
    public AcceptConvertQuoteCommand(AcceptConvertQuoteRequest request)
    {
        Request = request;
    }

    public AcceptConvertQuoteRequest Request { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        var client = (BinanceClient)service.GetService();
        var result = await client.AcceptConvertQuoteAsync(Request.QuoteId, cancellationToken);
        var element = result ?? JsonDocument.Parse("null").RootElement;
        var status = result.HasValue ? "success" : "not_found";
        return new OperationResult(element, status);
    }
}
