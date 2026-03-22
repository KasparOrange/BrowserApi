using System.Text.Json;
using BrowserApi.Dom;

namespace BrowserApi.WebStorage;

/// <summary>
/// A typed wrapper around the browser's <see cref="Storage"/> API (<c>localStorage</c> or
/// <c>sessionStorage</c>) that automatically handles JSON serialization and deserialization.
/// </summary>
/// <remarks>
/// <para>
/// Instead of manually calling <c>JSON.stringify</c> / <c>JSON.parse</c> or their C# equivalents,
/// <see cref="TypedStorage"/> lets you store and retrieve complex objects with a single call.
/// For raw string access, use <see cref="GetString"/> and <see cref="SetString"/>.
/// </para>
/// <para>
/// Create instances via <see cref="StorageExtensions.TypedLocalStorage"/> or
/// <see cref="StorageExtensions.TypedSessionStorage"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var storage = window.TypedLocalStorage();
///
/// // Store and retrieve typed objects
/// storage.Set("settings", new AppSettings { Theme = "dark", Lang = "en" });
/// var settings = storage.Get&lt;AppSettings&gt;("settings");
///
/// // Simple key-value checks
/// if (storage.ContainsKey("settings"))
///     Console.WriteLine($"Storage has {storage.Count} items");
///
/// // Enumerate all keys
/// foreach (var key in storage.Keys)
///     Console.WriteLine(key);
///
/// // Remove or clear
/// storage.Remove("settings");
/// storage.Clear();
/// </code>
/// </example>
/// <seealso cref="StorageExtensions"/>
/// <seealso cref="StorageChangedEventArgs"/>
public sealed class TypedStorage {
    private readonly BrowserApi.Dom.Storage _storage;

    /// <summary>
    /// Initializes a new instance of <see cref="TypedStorage"/> wrapping the specified
    /// browser <see cref="Storage"/> object.
    /// </summary>
    /// <param name="storage">
    /// The underlying browser storage (typically <c>window.localStorage</c> or
    /// <c>window.sessionStorage</c>).
    /// </param>
    public TypedStorage(BrowserApi.Dom.Storage storage) {
        _storage = storage;
    }

    /// <summary>
    /// Retrieves the value associated with <paramref name="key"/> and deserializes it from JSON
    /// into <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the stored JSON into.</typeparam>
    /// <param name="key">The storage key.</param>
    /// <returns>
    /// The deserialized value, or the default value of <typeparamref name="T"/> if the key
    /// does not exist.
    /// </returns>
    public T? Get<T>(string key) {
        var json = _storage.GetItem(key);
        if (json is null) return default;
        return JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web);
    }

    /// <summary>
    /// Serializes <paramref name="value"/> to JSON and stores it under <paramref name="key"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value to store.</typeparam>
    /// <param name="key">The storage key.</param>
    /// <param name="value">The value to serialize and store.</param>
    public void Set<T>(string key, T value) {
        var json = JsonSerializer.Serialize(value, JsonSerializerOptions.Web);
        _storage.SetItem(key, json);
    }

    /// <summary>
    /// Retrieves the raw string value associated with <paramref name="key"/>, without JSON
    /// deserialization.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <returns>The stored string value, or <see langword="null"/> if the key does not exist.</returns>
    public string? GetString(string key) => _storage.GetItem(key);

    /// <summary>
    /// Stores a raw string value under <paramref name="key"/>, without JSON serialization.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="value">The string value to store.</param>
    public void SetString(string key, string value) => _storage.SetItem(key, value);

    /// <summary>
    /// Removes the storage item with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key of the item to remove. No error is thrown if the key does not exist.</param>
    public void Remove(string key) => _storage.RemoveItem(key);

    /// <summary>
    /// Removes all items from the storage.
    /// </summary>
    public void Clear() => _storage.Clear();

    /// <summary>
    /// Determines whether the storage contains an item with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns><see langword="true"/> if the key exists in storage; otherwise <see langword="false"/>.</returns>
    public bool ContainsKey(string key) => _storage.GetItem(key) is not null;

    /// <summary>
    /// Gets the number of items currently stored.
    /// </summary>
    public int Count => (int)_storage.Length;

    /// <summary>
    /// Gets an enumerable of all keys currently in the storage.
    /// </summary>
    /// <remarks>
    /// The keys are yielded lazily by index. The order is implementation-defined by the browser.
    /// </remarks>
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
