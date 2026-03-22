using BrowserApi.Common;
using BrowserApi.Dom;
using BrowserApi.WebStorage;
using BrowserApi.Tests.Common;

namespace BrowserApi.Tests.Storage;

[Collection("JsObject")]
public class TypedStorageTests : IDisposable {
    private readonly MockBrowserBackend _mock;
    private readonly TypedStorage _storage;

    public TypedStorageTests() {
        _mock = new MockBrowserBackend();
        JsObject.Backend = _mock;
        var rawStorage = new BrowserApi.Dom.Storage { Handle = new JsHandle(new object()) };
        _storage = new TypedStorage(rawStorage);
    }

    public void Dispose() { }

    [Fact]
    public void Set_serializes_and_calls_SetItem() {
        _storage.Set("user", new TestUser { Name = "Alice", Age = 30 });

        var call = Assert.Single(_mock.Calls, c => c.Name == "setItem");
        Assert.Equal("user", call.Args[0]);
        var json = (string)call.Args[1]!;
        Assert.Contains("Alice", json);
    }

    [Fact]
    public void Get_calls_GetItem_and_deserializes() {
        _mock.InvokeReturnValue = "{\"name\":\"Alice\",\"age\":30}";

        var user = _storage.Get<TestUser>("user");

        Assert.NotNull(user);
        Assert.Equal("Alice", user!.Name);
        Assert.Equal(30, user.Age);
    }

    [Fact]
    public void Get_returns_default_for_missing_key() {
        _mock.InvokeReturnValue = null;

        var result = _storage.Get<TestUser>("missing");

        Assert.Null(result);
    }

    [Fact]
    public void GetString_delegates_to_GetItem() {
        _mock.InvokeReturnValue = "raw value";

        var result = _storage.GetString("key");

        Assert.Equal("raw value", result);
        Assert.Contains(_mock.Calls, c => c.Name == "getItem");
    }

    [Fact]
    public void SetString_delegates_to_SetItem() {
        _storage.SetString("key", "value");

        Assert.Contains(_mock.Calls, c => c.Name == "setItem");
    }

    [Fact]
    public void Remove_delegates_to_RemoveItem() {
        _storage.Remove("key");

        Assert.Contains(_mock.Calls, c => c.Name == "removeItem");
    }

    [Fact]
    public void Clear_delegates_to_Clear() {
        _storage.Clear();

        Assert.Contains(_mock.Calls, c => c.Name == "clear");
    }

    [Fact]
    public void ContainsKey_returns_true_when_item_exists() {
        _mock.InvokeReturnValue = "some value";

        Assert.True(_storage.ContainsKey("key"));
    }

    [Fact]
    public void ContainsKey_returns_false_when_item_missing() {
        _mock.InvokeReturnValue = null;

        Assert.False(_storage.ContainsKey("missing"));
    }

    [Fact]
    public void Count_delegates_to_Length() {
        _mock.PropertyValues["length"] = (uint)3;

        Assert.Equal(3, _storage.Count);
    }

    [Fact]
    public void Keys_enumerates_all_keys_via_Key_index() {
        _mock.PropertyValues["length"] = (uint)3;

        // The Storage.Key(index) method calls Invoke("key", index) on the backend.
        // We need to return different keys for each call. Since MockBrowserBackend
        // returns the same InvokeReturnValue each time, we test the iteration pattern.
        _mock.InvokeReturnValue = "key0";

        var keys = _storage.Keys.ToList();

        // Verify that Key(index) was called for each index
        var keyCalls = _mock.Calls.Where(c => c.Name == "key").ToList();
        Assert.Equal(3, keyCalls.Count);
        Assert.Equal(3, keys.Count);
    }

    [Fact]
    public void Keys_skips_null_keys() {
        _mock.PropertyValues["length"] = (uint)2;
        _mock.InvokeReturnValue = null;

        var keys = _storage.Keys.ToList();

        Assert.Empty(keys);
    }

    [Fact]
    public void Set_then_Get_roundtrip_with_complex_object() {
        var settings = new TestSettings {
            Theme = "dark",
            FontSize = 14,
            Tags = ["c#", "blazor"]
        };

        _storage.Set("settings", settings);

        // Get the JSON that was stored in the SetItem call
        var setCall = _mock.Calls.First(c => c.Name == "setItem");
        var storedJson = (string)setCall.Args[1]!;

        // Set up the mock to return that same JSON for the Get call
        _mock.InvokeReturnValue = storedJson;
        var restored = _storage.Get<TestSettings>("settings");

        Assert.NotNull(restored);
        Assert.Equal("dark", restored!.Theme);
        Assert.Equal(14, restored.FontSize);
        Assert.Equal(new[] { "c#", "blazor" }, restored.Tags);
    }

    private record TestUser {
        public string Name { get; init; } = "";
        public int Age { get; init; }
    }

    private record TestSettings {
        public string Theme { get; init; } = "";
        public int FontSize { get; init; }
        public string[] Tags { get; init; } = [];
    }
}
