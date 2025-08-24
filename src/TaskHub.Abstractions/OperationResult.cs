using System.Text.Json;

namespace TaskHub.Abstractions;

/// <summary>
/// Generic result object returned by services and handlers when performing operations.
/// Contains either the resulting payload or an error message.
/// </summary>
/// <param name="Payload">Successful result payload if any.</param>
/// <param name="Result">"success" or an error message describing the failure.</param>
public record OperationResult(JsonElement? Payload, string Result);

