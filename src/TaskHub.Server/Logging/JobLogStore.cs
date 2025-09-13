using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TaskHub.Server;

/// <summary>
/// In-memory implementation of <see cref="IJobLogStore"/> with bounded job retention.
/// </summary>
public class JobLogStore : IJobLogStore
{
    private readonly ConcurrentDictionary<string, List<string>> _logs = new();
    private readonly ConcurrentQueue<string> _order = new();
    private const int MaxJobs = 100;

    public void Append(string jobId, string message)
    {
        var list = _logs.GetOrAdd(jobId, _ => new List<string>());
        lock (list)
        {
            list.Add(message);
        }
        _order.Enqueue(jobId);
        while (_order.Count > MaxJobs && _order.TryDequeue(out var oldId))
        {
            _logs.TryRemove(oldId, out _);
        }
    }

    public IReadOnlyList<string>? GetLogs(string jobId)
    {
        return _logs.TryGetValue(jobId, out var list) ? list.AsReadOnly() : null;
    }
}
