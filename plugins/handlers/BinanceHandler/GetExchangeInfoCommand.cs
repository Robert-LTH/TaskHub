using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;
using BinanceServicePlugin;

namespace BinanceHandler;

public class GetExchangeInfoCommand : ICommand
{
    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        var client = (BinanceClient)service.GetService();
        var info = await client.GetExchangeInfoAsync(cancellationToken);
        var element = JsonSerializer.SerializeToElement(info);
        var status = info != null ? "success" : "not_found";
        return new OperationResult(element, status);
    }
}
