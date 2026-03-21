namespace BrowserApi.WebStorage;

public sealed class StorageChangedEventArgs {
    public string? Key { get; }
    public string? OldValue { get; }
    public string? NewValue { get; }
    public string Url { get; }

    public StorageChangedEventArgs(string? key, string? oldValue, string? newValue, string url) {
        Key = key;
        OldValue = oldValue;
        NewValue = newValue;
        Url = url;
    }
}
