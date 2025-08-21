using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace EchoHandler;

public class EchoCommandHandler : ICommandHandler
{
    public IReadOnlyCollection<string> Commands => new[] { "echo" };
    public string ServiceName => "http";

    public async Task ExecuteAsync(JsonElement payload, IServicePlugin service, CancellationToken cancellationToken)
    {
        var resource = payload.GetProperty("resource").GetString() ?? string.Empty;
        var client = (HttpClient)service.GetService();
        var result = await client.GetStringAsync(resource, cancellationToken);
        Console.WriteLine($"Echo: {result}");
    }
}

