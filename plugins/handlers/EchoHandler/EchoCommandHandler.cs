using System;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace EchoHandler;

public class EchoCommandHandler : ICommandHandler
{
    public string Name => "echo";

    public async Task ExecuteAsync(string arguments, IServicePlugin service, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(arguments, cancellationToken);
        Console.WriteLine($"Echo: {result}");
    }
}
