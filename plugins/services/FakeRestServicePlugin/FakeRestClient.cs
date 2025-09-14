using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FakeRestServicePlugin;

/// <summary>
/// Simple client for the Fake REST API.
/// Swagger spec: https://fakerestapi.azurewebsites.net/swagger/v1/swagger.json
/// </summary>
public class FakeRestClient
{
    private readonly HttpClient _http;

    public FakeRestClient(HttpClient http)
    {
        _http = http;
        if (_http.BaseAddress == null)
        {
            _http.BaseAddress = new Uri("https://fakerestapi.azurewebsites.net/api/v1/");
        }
    }

    public async Task<IReadOnlyList<Book>> GetBooksAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync("Books", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return Array.Empty<Book>();
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var books = await JsonSerializer.DeserializeAsync<List<Book>>(stream, cancellationToken: cancellationToken);
        return books ?? new List<Book>();
    }

    public async Task<Book?> GetBookAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync($"Books/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<Book>(stream, cancellationToken: cancellationToken);
    }

    public async Task<Book?> CreateBookAsync(Book book, CancellationToken cancellationToken = default)
    {
        var content = new StringContent(JsonSerializer.Serialize(book), Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("Books", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<Book>(stream, cancellationToken: cancellationToken);
    }

    public async Task<bool> UpdateBookAsync(int id, Book book, CancellationToken cancellationToken = default)
    {
        var content = new StringContent(JsonSerializer.Serialize(book), Encoding.UTF8, "application/json");
        var response = await _http.PutAsync($"Books/{id}", content, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteBookAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"Books/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
