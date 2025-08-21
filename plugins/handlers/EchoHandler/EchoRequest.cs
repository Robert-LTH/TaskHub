using System.Text.Json.Serialization;

namespace EchoHandler;

public class EchoRequest
{
    [JsonPropertyName("resource")]
    public string Resource { get; set; } = string.Empty;
}

