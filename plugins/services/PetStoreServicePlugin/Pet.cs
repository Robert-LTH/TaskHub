using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PetStoreServicePlugin;

public class Pet
{
    [JsonPropertyName("id")] public long? Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("photoUrls")] public List<string> PhotoUrls { get; set; } = new();
    [JsonPropertyName("status")] public string? Status { get; set; }
}
