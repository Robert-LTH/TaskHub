using System.Text.Json.Serialization;

namespace PetStoreHandler;

public class GetPetRequest
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}
