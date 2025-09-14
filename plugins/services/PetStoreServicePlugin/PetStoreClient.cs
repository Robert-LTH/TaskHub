using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PetStoreServicePlugin;

/// <summary>
/// Simple client for the Swagger Petstore API.
/// Swagger spec: https://petstore.swagger.io/v2/swagger.json
/// </summary>
public class PetStoreClient
{
    private readonly HttpClient _http;

    public PetStoreClient(HttpClient http)
    {
        _http = http;
        if (_http.BaseAddress == null)
        {
            _http.BaseAddress = new Uri("https://petstore.swagger.io/v2/");
        }
    }

    public async Task<Pet?> GetPetAsync(long id, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync($"pet/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<Pet>(stream, cancellationToken: cancellationToken);
    }
}
