using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;
using BinanceServicePlugin;

namespace BinanceHandler;

public class GetConvertOrderStatusCommand : ICommand
{
    public GetConvertOrderStatusCommand(GetConvertOrderStatusRequest request)
    {
        Request = request;
    }

    public GetConvertOrderStatusRequest Request { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        var client = (BinanceClient)service.GetService();
        var statusResp = await client.GetConvertOrderStatusAsync(Request.OrderId, cancellationToken);
        var element = statusResp ?? JsonDocument.Parse("null").RootElement;
        var status = statusResp.HasValue ? "success" : "not_found";
        return new OperationResult(element, status);
    }
}
