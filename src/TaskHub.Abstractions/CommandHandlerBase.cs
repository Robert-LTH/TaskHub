using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TaskHub.Abstractions;

public abstract class CommandHandlerBase : ICommandHandler, IServiceProviderAware
{
    private IServiceProvider? _services;

    protected IServiceProvider Services =>
        _services ?? throw new InvalidOperationException("Handler has not been initialized with services.");

    public abstract IReadOnlyCollection<string> Commands { get; }
    public abstract string ServiceName { get; }
    public abstract CommandExecutionContext ExecutionContext { get; }
    public abstract ICommand Create(JsonElement payload, ILogger logger);
    public virtual ICommand Create(string command, JsonElement payload, ILogger logger) => Create(payload, logger);

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
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var command = Create(payload, logger);
        var result = await command.ExecuteAsync(service, cancellationToken);
        return result;
    }
}
