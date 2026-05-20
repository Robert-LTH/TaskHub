using System.Text.Json.Serialization;

namespace PopupHandler;

public class ShowPopupRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("durationMilliseconds")]
    public int? DurationMilliseconds { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("margin")]
    public int? Margin { get; set; }
}
