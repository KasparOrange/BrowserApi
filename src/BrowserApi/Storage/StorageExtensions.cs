using BrowserApi.Common;
using BrowserApi.Dom;

namespace BrowserApi.WebStorage;

/// <summary>
/// Provides extension methods on <see cref="Window"/> for typed storage access and storage
/// change notifications.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TypedLocalStorage"/> and <see cref="TypedSessionStorage"/> wrap the browser's
/// <c>localStorage</c> and <c>sessionStorage</c> respectively with JSON serialization, so
/// you can store and retrieve complex objects without manual string conversion.
/// </para>
/// <para>
/// <see cref="OnStorageChanged"/> provides a notification callback that fires when storage
/// is modified in another tab or frame for the same origin.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Typed localStorage
/// var storage = window.TypedLocalStorage();
/// storage.Set("prefs", new UserPrefs { Theme = "dark", FontSize = 14 });
/// var prefs = storage.Get&lt;UserPrefs&gt;("prefs");
///
/// // Listen for cross-tab changes
/// window.OnStorageChanged(args => {
///     Console.WriteLine($"Key '{args.Key}' changed from '{args.OldValue}' to '{args.NewValue}'");
/// });
/// </code>
/// </example>
/// <seealso cref="TypedStorage"/>
/// <seealso cref="StorageChangedEventArgs"/>
public static class StorageExtensions {
    /// <summary>
    /// Creates a <see cref="TypedStorage"/> wrapper around the browser's <c>localStorage</c>,
    /// providing JSON-based get/set operations for complex objects.
    /// </summary>
    /// <param name="window">The window whose localStorage to wrap.</param>
    /// <returns>A <see cref="TypedStorage"/> instance backed by <c>localStorage</c>.</returns>
    public static TypedStorage TypedLocalStorage(this Window window) =>
        new(window.LocalStorage);

    /// <summary>
    /// Creates a <see cref="TypedStorage"/> wrapper around the browser's <c>sessionStorage</c>,
    /// providing JSON-based get/set operations for complex objects.
    /// </summary>
    /// <param name="window">The window whose sessionStorage to wrap.</param>
    /// <returns>A <see cref="TypedStorage"/> instance backed by <c>sessionStorage</c>.</returns>
    /// <remarks>
    /// Unlike <c>localStorage</c>, <c>sessionStorage</c> data is scoped to the tab and is
    /// cleared when the tab is closed.
    /// </remarks>
    public static TypedStorage TypedSessionStorage(this Window window) =>
        new(window.SessionStorage);

    /// <summary>
    /// Registers a callback that is invoked when the storage changes in another browsing context
    /// (tab, iframe, or window) for the same origin.
    /// </summary>
    /// <param name="window">The window to listen for storage events on.</param>
    /// <param name="callback">
    /// The callback invoked with a <see cref="StorageChangedEventArgs"/> describing the change.
    /// </param>
    /// <returns>
    /// A <see cref="JsHandle"/> representing the event listener, which can be used to remove
    /// the listener later.
    /// </returns>
    /// <remarks>
    /// The <c>storage</c> event fires only in other tabs/frames -- not in the tab that made the
    /// change. This is by design in the Web Storage specification.
    /// </remarks>
    public static JsHandle OnStorageChanged(this Window window, System.Action<StorageChangedEventArgs> callback) {
        return JsObject.Backend.AddEventListener(window.Handle, "storage", eventHandle => {
            var storageEvent = new StorageEvent { Handle = eventHandle };
            var args = new StorageChangedEventArgs(
                storageEvent.Key,
                storageEvent.OldValue,
                storageEvent.NewValue,
                storageEvent.Url
            );
            callback(args);
        });
    }
}
