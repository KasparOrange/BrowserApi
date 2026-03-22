using BrowserApi.WebStorage;

namespace BrowserApi.Tests.Storage;

public class StorageChangedEventArgsTests {
    [Fact]
    public void Constructor_sets_all_properties() {
        var args = new StorageChangedEventArgs("theme", "light", "dark", "https://example.com");

        Assert.Equal("theme", args.Key);
        Assert.Equal("light", args.OldValue);
        Assert.Equal("dark", args.NewValue);
        Assert.Equal("https://example.com", args.Url);
    }

    [Fact]
    public void Null_key_indicates_storage_cleared() {
        var args = new StorageChangedEventArgs(null, null, null, "https://example.com");

        Assert.Null(args.Key);
        Assert.Null(args.OldValue);
        Assert.Null(args.NewValue);
        Assert.Equal("https://example.com", args.Url);
    }

    [Fact]
    public void Null_oldValue_indicates_new_key() {
        var args = new StorageChangedEventArgs("newKey", null, "value", "https://example.com");

        Assert.Equal("newKey", args.Key);
        Assert.Null(args.OldValue);
        Assert.Equal("value", args.NewValue);
    }

    [Fact]
    public void Null_newValue_indicates_removed_key() {
        var args = new StorageChangedEventArgs("removedKey", "oldVal", null, "https://example.com");

        Assert.Equal("removedKey", args.Key);
        Assert.Equal("oldVal", args.OldValue);
        Assert.Null(args.NewValue);
    }

    [Fact]
    public void Url_is_always_accessible() {
        var args = new StorageChangedEventArgs("k", "o", "n", "https://example.com/page");

        Assert.Equal("https://example.com/page", args.Url);
    }
}
