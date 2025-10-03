using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TaskHub.Abstractions;

public abstract class CommandHandlerBase : ICommandHandler, IServiceProviderAware
{
    private IServiceProvider? _services;

    protected IServiceProvider Services =>
        _services ?? throw new InvalidOperationException("Handler has not been initialized with services.");

    public abstract IReadOnlyCollection<string> Commands { get; }
    public abstract string ServiceName { get; }
    public abstract ICommand Create(JsonElement payload);

    public virtual void OnLoaded(IServiceProvider services)
    {
        SetServiceProvider(services);
    }

    public void SetServiceProvider(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public virtual async Task<OperationResult> ExecuteAsync(
        JsonElement payload,
        IServicePlugin service,
        CancellationToken cancellationToken)
    {
        var command = Create(payload);
        var result = await command.ExecuteAsync(service, NullLogger.Instance, cancellationToken);
        return result;
    }

    public virtual async Task<OperationResult> ExecuteAsync(
        JsonElement payload,
        IServicePlugin service,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // Default implementation ignores logger; override in handlers to use it.
        var command = Create(payload);
        var result = await command.ExecuteAsync(service, logger, cancellationToken);
        return result;
    }
}

