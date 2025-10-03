using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TaskHub.Abstractions;

public interface ICommandHandler
{
    IReadOnlyCollection<string> Commands { get; }
    string ServiceName { get; }
    ICommand Create(JsonElement payload);
    void OnLoaded(IServiceProvider services);

    Task<OperationResult> ExecuteAsync(
        JsonElement payload,
        IServicePlugin service,
        ILogger logger,
        CancellationToken cancellationToken);
}

public interface ICommandHandler<out TCommand> : ICommandHandler where TCommand : ICommand
{
    new TCommand Create(JsonElement payload);
}

public interface IServiceProviderAware
{
    void SetServiceProvider(IServiceProvider services);
}

