using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TaskHub.Abstractions;

public abstract class CommandHandlerBase : ICommandHandler
{
    public abstract IReadOnlyCollection<string> Commands { get; }
    public abstract string ServiceName { get; }
    public abstract ICommand Create(JsonElement payload);
    public abstract void OnLoaded(IServiceProvider services);

    public virtual async Task<OperationResult> ExecuteAsync(
        JsonElement payload,
        IServicePlugin service,
        CancellationToken cancellationToken)
    {
        var command = Create(payload);
        var result = await command.ExecuteAsync(service, cancellationToken);
        return result;
    }
}
