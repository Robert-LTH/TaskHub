using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;
using BinanceServicePlugin;

namespace BinanceHandler;

public class GetServerTimeCommand : ICommand
{
    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        var client = (BinanceClient)service.GetService();
        var serverTime = await client.GetServerTimeAsync(cancellationToken);
        var element = JsonSerializer.SerializeToElement(serverTime);
        var status = serverTime != null ? "success" : "not_found";
        return new OperationResult(element, status);
    }
}
