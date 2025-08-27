using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;

namespace TaskHub.Server;

/// <summary>
/// Thread-safe container for aggregating inventory reports from multiple handlers.
/// </summary>
public class ReportingContainer : IReportingContainer
{
    private readonly ConcurrentQueue<InventoryReport> _queue = new();

    /// <inheritdoc />
    public void AddReport(string source, JsonElement data)
    {
        _queue.Enqueue(new InventoryReport(source, data, DateTimeOffset.UtcNow));
    }

    /// <inheritdoc />
    public IReadOnlyList<InventoryReport> DrainReports()
    {
        var list = new List<InventoryReport>();
        while (_queue.TryDequeue(out var report))
        {
            list.Add(report);
        }
        return list;
    }
}
