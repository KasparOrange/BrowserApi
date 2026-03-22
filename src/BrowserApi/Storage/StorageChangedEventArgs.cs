namespace BrowserApi.WebStorage;

/// <summary>
/// Contains data for a browser storage change event, corresponding to the JavaScript <c>StorageEvent</c>.
/// </summary>
/// <remarks>
/// <para>
/// Instances of this class are passed to the callback registered via
/// <see cref="StorageExtensions.OnStorageChanged"/>. They describe which key changed,
/// what the old and new values were, and which URL triggered the change.
/// </para>
/// <para>
/// When <see cref="Key"/> is <see langword="null"/>, the storage was cleared entirely
/// (e.g., via <c>localStorage.clear()</c>), and <see cref="OldValue"/> and
/// <see cref="NewValue"/> will also be <see langword="null"/>.
/// </para>
/// </remarks>
/// <seealso cref="StorageExtensions.OnStorageChanged"/>
/// <seealso cref="TypedStorage"/>
public sealed class StorageChangedEventArgs {
    /// <summary>
    /// Gets the key of the storage item that changed, or <see langword="null"/> if the entire
    /// storage was cleared.
    /// </summary>
    public string? Key { get; }

    /// <summary>
    /// Gets the previous value of the changed key, or <see langword="null"/> if the key was
    /// newly added or the storage was cleared.
    /// </summary>
    public string? OldValue { get; }

    /// <summary>
    /// Gets the new value of the changed key, or <see langword="null"/> if the key was removed
    /// or the storage was cleared.
    /// </summary>
    public string? NewValue { get; }

    /// <summary>
    /// Gets the URL of the document whose storage changed.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageChangedEventArgs"/> class.
    /// </summary>
    /// <param name="key">The key that changed, or <see langword="null"/> if cleared.</param>
    /// <param name="oldValue">The previous value, or <see langword="null"/>.</param>
    /// <param name="newValue">The new value, or <see langword="null"/>.</param>
    /// <param name="url">The URL of the document whose storage changed.</param>
    public StorageChangedEventArgs(string? key, string? oldValue, string? newValue, string url) {
        Key = key;
        OldValue = oldValue;
        NewValue = newValue;
        Url = url;
    }
}
