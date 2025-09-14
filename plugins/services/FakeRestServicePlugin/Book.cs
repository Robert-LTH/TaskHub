using System;
using System.Text.Json.Serialization;

namespace FakeRestServicePlugin;

public class Book
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("pageCount")]
    public int PageCount { get; set; }

    [JsonPropertyName("excerpt")]
    public string? Excerpt { get; set; }

    [JsonPropertyName("publishDate")]
    public DateTime PublishDate { get; set; }
}
