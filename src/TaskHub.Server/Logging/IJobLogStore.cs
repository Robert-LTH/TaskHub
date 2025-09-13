using System.Collections.Generic;

namespace TaskHub.Server;

/// <summary>
/// Stores log messages produced by jobs for later retrieval.
/// </summary>
public interface IJobLogStore
{
    /// <summary>
    /// Append a log message for the specified job.
    /// </summary>
    void Append(string jobId, string message);

    /// <summary>
    /// Get the collected log messages for the specified job.
    /// </summary>
    IReadOnlyList<string>? GetLogs(string jobId);
}
