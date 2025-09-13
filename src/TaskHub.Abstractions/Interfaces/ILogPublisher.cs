using System;

namespace TaskHub.Abstractions;

/// <summary>
/// Publishes log messages produced by running jobs.
/// </summary>
public interface ILogPublisher
{
    /// <summary>
    /// Publish a log message associated with a job.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="message">The log message.</param>
    /// <param name="callbackConnectionId">Optional connection identifier to route the log.</param>
    void PublishLog(string jobId, string message, string? callbackConnectionId);
}
