using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace EchoHandler;

public class EchoCommand : ICommand
{
    private readonly IReportingContainer? _container;

    public EchoCommand(EchoRequest request, IReportingContainer? container)
    {
        Request = request;
        _container = container;
    }

    public EchoRequest Request { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        var client = (HttpClient)service.GetService();
        var result = await client.GetStringAsync(Request.Resource, cancellationToken);
        logger.LogInformation($"Echo: {result}");
        var element = JsonSerializer.SerializeToElement(result);
        _container?.AddReport("echo", element);
        return new OperationResult(element, "success");
    }
}

