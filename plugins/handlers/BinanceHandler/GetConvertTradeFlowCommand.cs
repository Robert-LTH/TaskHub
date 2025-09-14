using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;
using BinanceServicePlugin;

namespace BinanceHandler;

public class GetConvertTradeFlowCommand : ICommand
{
    public GetConvertTradeFlowCommand(GetConvertTradeFlowRequest request)
    {
        Request = request;
    }

    public GetConvertTradeFlowRequest Request { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        var client = (BinanceClient)service.GetService();
        var flow = await client.GetConvertTradeFlowAsync(
            Request.StartTime,
            Request.EndTime,
            Request.Page,
            Request.Limit,
            cancellationToken);
        var element = flow ?? JsonDocument.Parse("null").RootElement;
        var status = flow.HasValue ? "success" : "not_found";
        return new OperationResult(element, status);
    }
}
