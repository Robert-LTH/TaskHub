using System.Text.Json.Serialization;

namespace DiskSpaceHandler;

public class DiskSpaceRequest
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "/";
}

