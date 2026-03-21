using BrowserApi.Common;
using BrowserApi.Dom;

namespace BrowserApi.WebStorage;

public static class StorageExtensions {
    public static TypedStorage TypedLocalStorage(this Window window) =>
        new(window.LocalStorage);

    public static TypedStorage TypedSessionStorage(this Window window) =>
        new(window.SessionStorage);

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
