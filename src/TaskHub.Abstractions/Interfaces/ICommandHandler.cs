using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TaskHub.Abstractions;

public interface ICommandHandler
{
    IReadOnlyCollection<string> Commands { get; }
    string ServiceName { get; }
    Task ExecuteAsync(JsonElement payload, IServicePlugin service, CancellationToken cancellationToken);
}
