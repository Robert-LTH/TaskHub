namespace TaskHub.Abstractions;

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public interface ICommand
{
    /// <summary>
    /// Indicates whether this command should wait for all previously queued
    /// commands to complete before it begins execution. Defaults to
    /// <c>false</c>, allowing the command to run in parallel with earlier
    /// commands in the chain.
    /// </summary>
    bool WaitForPrevious => false;

    Task<OperationResult> ExecuteAsync(
        IServicePlugin service,
        ILogger logger,
        CancellationToken cancellationToken);
}

