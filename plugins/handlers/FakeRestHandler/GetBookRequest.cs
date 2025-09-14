using System.Text.Json.Serialization;

namespace FakeRestHandler;

public class GetBookRequest
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}
