namespace TaskHub.Abstractions;

using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public interface ICommand
{
    Task<OperationResult> ExecuteAsync(
        IServicePlugin service,
        CancellationToken cancellationToken,
        ClientWebSocket? socket = null);
}

