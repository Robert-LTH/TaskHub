using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskHub.Server;

public class ScriptItem
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.NewGuid().ToString("n");
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string? Description { get; set; }
    // Store as plain text; clients may base64 if needed
    [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
    [JsonPropertyName("isVerified")] public bool IsVerified { get; set; }
    [JsonPropertyName("createdAt")] public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    [JsonPropertyName("updatedAt")] public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ScriptsRepository
{
    private readonly string _storePath;
    private readonly ConcurrentDictionary<string, ScriptItem> _items = new();
    private readonly object _fileLock = new();

    public ScriptsRepository()
    {
        var dataRoot = Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(dataRoot);
        _storePath = Path.Combine(dataRoot, "scripts.json");
        Load();
    }

    private void Load()
    {
        if (!File.Exists(_storePath)) return;
        try
        {
            var json = File.ReadAllText(_storePath);
            var items = JsonSerializer.Deserialize<List<ScriptItem>>(json) ?? new List<ScriptItem>();
            foreach (var it in items)
            {
                _items[it.Id] = it;
            }
        }
        catch
        {
            // ignore corrupt store
        }
    }

    private void Persist()
    {
        lock (_fileLock)
        {
            var items = _items.Values.OrderBy(i => i.Name).ToList();
            var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_storePath, json);
        }
    }

    public IEnumerable<ScriptItem> GetAll() => _items.Values.OrderBy(i => i.Name);
    public bool TryGet(string id, out ScriptItem? item) => _items.TryGetValue(id, out item);

    public ScriptItem CreateOrUpdate(ScriptItem incoming)
    {
        if (string.IsNullOrWhiteSpace(incoming.Id)) incoming.Id = Guid.NewGuid().ToString("n");
        var now = DateTimeOffset.UtcNow;
        incoming.UpdatedAt = now;
        if (!_items.ContainsKey(incoming.Id))
        {
            incoming.CreatedAt = now;
        }
        _items[incoming.Id] = incoming;
        Persist();
        return incoming;
    }

    public bool Delete(string id)
    {
        var ok = _items.TryRemove(id, out _);
        if (ok) Persist();
        return ok;
    }
}
