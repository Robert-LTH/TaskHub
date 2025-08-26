using System.Threading;
using System.Threading.Tasks;

namespace TaskHub.Abstractions;

/// <summary>
/// Publishes the result of a completed job to interested transports.
/// </summary>
public interface IResultPublisher
{
    /// <summary>
    /// Publish the result of a job to a remote caller.
    /// </summary>
    /// <param name="result">The job result.</param>
    /// <param name="callbackConnectionId">Optional connection identifier to route the result.</param>
    /// <param name="token">Cancellation token.</param>
    Task PublishResultAsync(CommandStatusResult result, string? callbackConnectionId, CancellationToken token);
}
