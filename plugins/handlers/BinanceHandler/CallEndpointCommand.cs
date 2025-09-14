using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;
using BinanceServicePlugin;

namespace BinanceHandler;

public class CallEndpointCommand : ICommand
{
    public CallEndpointCommand(CallEndpointRequest request)
    {
        Request = request;
    }

    public CallEndpointRequest Request { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        var client = (BinanceClient)service.GetService();
        var method = new HttpMethod(Request.Method.ToUpperInvariant());
        var result = await client.SendAsync(method, Request.Endpoint, Request.Query, Request.Body, cancellationToken);
        var element = result ?? JsonDocument.Parse("{}").RootElement;
        var status = result != null ? "success" : "error";
        return new OperationResult(element, status);
    }
}

