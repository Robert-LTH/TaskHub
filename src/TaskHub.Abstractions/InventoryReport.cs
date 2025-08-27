using System;
using System.Text.Json;

namespace TaskHub.Abstractions;

/// <summary>
/// Represents a unit of inventory data reported by a handler.
/// </summary>
/// <param name="Source">Name of the handler or source.</param>
/// <param name="Data">Inventory payload.</param>
/// <param name="CollectedAt">Timestamp when the inventory was collected.</param>
public record InventoryReport(string Source, JsonElement Data, DateTimeOffset CollectedAt);
