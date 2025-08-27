using System.Collections.Generic;
using System.Text.Json;

namespace TaskHub.Abstractions;

/// <summary>
/// Collects inventory data from handlers for periodic reporting.
/// </summary>
public interface IReportingContainer
{
    /// <summary>
    /// Add inventory data for later publishing.
    /// </summary>
    /// <param name="source">Name of the handler or source.</param>
    /// <param name="data">The inventory payload.</param>
    void AddReport(string source, JsonElement data);

    /// <summary>
    /// Retrieve and remove all pending reports.
    /// </summary>
    /// <returns>Collection of inventory reports.</returns>
    IReadOnlyList<InventoryReport> DrainReports();
}
