using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace EchoHandler;

public class EchoCommand : ICommand
{
    public EchoCommand(EchoRequest request)
    {
        Request = request;
    }

    public EchoRequest Request { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken, ClientWebSocket? socket = null)
    {
        var client = (HttpClient)service.GetService();
        var result = await client.GetStringAsync(Request.Resource, cancellationToken);
        Console.WriteLine($"Echo: {result}");
        return new OperationResult(JsonSerializer.SerializeToElement(result), "success");
    }
}

