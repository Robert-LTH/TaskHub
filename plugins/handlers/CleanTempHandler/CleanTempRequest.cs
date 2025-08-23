using System.Text.Json.Serialization;

namespace CleanTempHandler;

public class CleanTempRequest
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = @"C:\\temp22";
}
