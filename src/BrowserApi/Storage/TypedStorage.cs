using System.Text.Json;
using BrowserApi.Dom;

namespace BrowserApi.WebStorage;

public sealed class TypedStorage {
    private readonly BrowserApi.Dom.Storage _storage;

    public TypedStorage(BrowserApi.Dom.Storage storage) {
        _storage = storage;
    }

    public T? Get<T>(string key) {
        var json = _storage.GetItem(key);
        if (json is null) return default;
        return JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web);
    }

    public void Set<T>(string key, T value) {
        var json = JsonSerializer.Serialize(value, JsonSerializerOptions.Web);
        _storage.SetItem(key, json);
    }

    public string? GetString(string key) => _storage.GetItem(key);

    public void SetString(string key, string value) => _storage.SetItem(key, value);

    public void Remove(string key) => _storage.RemoveItem(key);

    public void Clear() => _storage.Clear();

    public bool ContainsKey(string key) => _storage.GetItem(key) is not null;

    public int Count => (int)_storage.Length;

    public IEnumerable<string> Keys {
        get {
            var length = _storage.Length;
            for (uint i = 0; i < length; i++) {
                var key = _storage.Key(i);
                if (key is not null)
                    yield return key;
            }
        }
    }
}
