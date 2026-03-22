using BrowserApi.Common;

namespace BrowserApi.Dom;

/// <summary>
/// Represents a subscription to a DOM event that can be disposed to remove the event listener.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EventSubscription"/> is returned by the event-binding methods in
/// <see cref="EventExtensions"/> (e.g., <see cref="EventExtensions.OnClick"/>,
/// <see cref="EventExtensions.OnKeyDown"/>). Calling <see cref="Dispose"/> removes the
/// underlying <c>addEventListener</c> callback from the DOM target, preventing further invocations
/// and avoiding memory leaks.
/// </para>
/// <para>
/// This type implements <see cref="IDisposable"/>, so it works naturally with <c>using</c>
/// statements and scoped lifetime management.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Subscribe to clicks, then unsubscribe when done
/// using var subscription = button.OnClick(e => Console.WriteLine("Clicked!"));
///
/// // Or manually dispose later
/// var sub = button.OnClick(e => HandleClick(e));
/// // ... some time later ...
/// sub.Dispose(); // removes the event listener
/// </code>
/// </example>
/// <seealso cref="EventExtensions"/>
/// <seealso cref="EventTarget"/>
public sealed class EventSubscription : IDisposable {
    private readonly JsHandle _targetHandle;
    private readonly string _eventName;
    private readonly JsHandle _listenerId;
    private bool _disposed;

    internal EventSubscription(JsHandle targetHandle, string eventName, JsHandle listenerId) {
        _targetHandle = targetHandle;
        _eventName = eventName;
        _listenerId = listenerId;
    }

    /// <summary>
    /// Removes the event listener from the DOM target. Subsequent calls are safe no-ops.
    /// </summary>
    public void Dispose() {
        if (_disposed) return;
        _disposed = true;
        JsObject.Backend.RemoveEventListener(_targetHandle, _eventName, _listenerId);
    }
}
