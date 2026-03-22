using BrowserApi.Common;
using Microsoft.JSInterop;

namespace BrowserApi.JSInterop;

/// <summary>
/// An <see cref="IBrowserBackend"/> implementation that uses Blazor's
/// <see cref="IJSRuntime"/> and <see cref="IJSInProcessRuntime"/> to communicate
/// with JavaScript.
/// </summary>
/// <remarks>
/// <para>
/// This backend delegates all interop calls to a companion JavaScript module
/// (<c>browserApi</c>) that must be loaded in the browser. The JavaScript module
/// provides helper functions (<c>browserApi.getProperty</c>, <c>browserApi.setProperty</c>,
/// <c>browserApi.invoke</c>, etc.) that perform the actual DOM operations.
/// </para>
/// <para>
/// <b>Synchronous vs. asynchronous:</b> Synchronous methods (e.g., <see cref="GetProperty{T}"/>,
/// <see cref="SetProperty"/>, <see cref="InvokeVoid"/>, <see cref="Invoke{T}"/>) require
/// <see cref="IJSInProcessRuntime"/>, which is only available in Blazor WebAssembly.
/// Calling synchronous methods from Blazor Server will throw an
/// <see cref="InvalidOperationException"/>. Asynchronous methods work in both hosting models.
/// </para>
/// <para>
/// <b>Event handling:</b> Event listeners are registered via <c>browserApi.addEventListener</c>,
/// which returns a JavaScript object reference representing the subscription. The .NET
/// callback is invoked through a <see cref="DotNetObjectReference{T}"/> adapter that
/// bridges the JavaScript event back to managed code.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Typical setup in a Blazor WebAssembly app:
/// var backend = new JSInteropBackend(jsRuntime);
/// JsObject.Backend = backend;
/// </code>
/// </example>
/// <seealso cref="IBrowserBackend"/>
/// <seealso cref="JsObject"/>
public sealed class JSInteropBackend : IBrowserBackend {
    private readonly IJSRuntime _jsRuntime;
    private readonly IJSInProcessRuntime? _syncRuntime;

    /// <summary>
    /// Initializes a new instance of the <see cref="JSInteropBackend"/> class.
    /// </summary>
    /// <param name="jsRuntime">
    /// The Blazor JS runtime to use for interop calls. If the runtime also implements
    /// <see cref="IJSInProcessRuntime"/> (as it does in Blazor WebAssembly), synchronous
    /// interop methods will be available.
    /// </param>
    public JSInteropBackend(IJSRuntime jsRuntime) {
        _jsRuntime = jsRuntime;
        _syncRuntime = jsRuntime as IJSInProcessRuntime;
    }

    /// <summary>
    /// Gets the synchronous JS runtime, or throws if synchronous interop is not available.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the underlying <see cref="IJSRuntime"/> does not implement
    /// <see cref="IJSInProcessRuntime"/> (i.e., on Blazor Server).
    /// </exception>
    private IJSInProcessRuntime SyncRuntime =>
        _syncRuntime ?? throw new InvalidOperationException(
            "Synchronous JS interop requires IJSInProcessRuntime (WebAssembly). Use async methods for Blazor Server.");

    /// <inheritdoc/>
    /// <remarks>
    /// Calls <c>browserApi.getProperty(target, propertyName)</c> synchronously.
    /// Requires <see cref="IJSInProcessRuntime"/> (Blazor WebAssembly only).
    /// </remarks>
    public T GetProperty<T>(JsHandle target, string propertyName) {
        return SyncRuntime.Invoke<T>("browserApi.getProperty", target.Value, propertyName);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Calls <c>browserApi.setProperty(target, propertyName, value)</c> synchronously.
    /// Requires <see cref="IJSInProcessRuntime"/> (Blazor WebAssembly only).
    /// </remarks>
    public void SetProperty(JsHandle target, string propertyName, object? value) {
        SyncRuntime.InvokeVoid("browserApi.setProperty", target.Value, propertyName, value);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Calls <c>browserApi.invoke(target, methodName, args)</c> synchronously.
    /// Requires <see cref="IJSInProcessRuntime"/> (Blazor WebAssembly only).
    /// </remarks>
    public void InvokeVoid(JsHandle target, string methodName, object?[] args) {
        SyncRuntime.InvokeVoid("browserApi.invoke", target.Value, methodName, args);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Calls <c>browserApi.invoke(target, methodName, args)</c> synchronously and returns the result.
    /// Requires <see cref="IJSInProcessRuntime"/> (Blazor WebAssembly only).
    /// </remarks>
    public T Invoke<T>(JsHandle target, string methodName, object?[] args) {
        return SyncRuntime.Invoke<T>("browserApi.invoke", target.Value, methodName, args);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Calls <c>browserApi.invokeAsync(target, methodName, args)</c> asynchronously.
    /// Works in both Blazor WebAssembly and Blazor Server.
    /// </remarks>
    public async Task InvokeVoidAsync(JsHandle target, string methodName, object?[] args) {
        await _jsRuntime.InvokeVoidAsync("browserApi.invokeAsync", target.Value, methodName, args);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Calls <c>browserApi.invokeAsync(target, methodName, args)</c> asynchronously and returns the result.
    /// Works in both Blazor WebAssembly and Blazor Server.
    /// </remarks>
    public async Task<T> InvokeAsync<T>(JsHandle target, string methodName, object?[] args) {
        return await _jsRuntime.InvokeAsync<T>("browserApi.invokeAsync", target.Value, methodName, args);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Calls <c>browserApi.construct(jsClassName, args)</c> synchronously and wraps the
    /// returned <see cref="IJSObjectReference"/> in a <see cref="JsHandle"/>.
    /// Requires <see cref="IJSInProcessRuntime"/> (Blazor WebAssembly only).
    /// </remarks>
    public JsHandle Construct(string jsClassName, object?[] args) {
        var reference = SyncRuntime.Invoke<IJSObjectReference>("browserApi.construct", jsClassName, args);
        return new JsHandle(reference);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Calls <c>browserApi.getGlobal(name)</c> synchronously and wraps the returned
    /// <see cref="IJSObjectReference"/> in a <see cref="JsHandle"/>.
    /// Requires <see cref="IJSInProcessRuntime"/> (Blazor WebAssembly only).
    /// </remarks>
    public JsHandle GetGlobal(string name) {
        var reference = SyncRuntime.Invoke<IJSObjectReference>("browserApi.getGlobal", name);
        return new JsHandle(reference);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// If the handle wraps an <see cref="IJSObjectReference"/>, this method calls
    /// <see cref="IAsyncDisposable.DisposeAsync"/> on it, allowing the browser to
    /// garbage-collect the underlying JavaScript object. Handles that do not wrap
    /// an <see cref="IJSObjectReference"/> are silently ignored.
    /// </remarks>
    public async ValueTask DisposeHandle(JsHandle handle) {
        if (handle.Value is IJSObjectReference objRef)
            await objRef.DisposeAsync();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Registers an event listener by calling <c>browserApi.addEventListener(target, eventName, dotNetRef)</c>.
    /// A <see cref="DotNetObjectReference{T}"/> wrapping an <see cref="EventCallbackAdapter"/>
    /// is created to bridge the JavaScript event callback back into managed code.
    /// </para>
    /// <para>
    /// The returned <see cref="JsHandle"/> represents the listener and must be passed to
    /// <see cref="RemoveEventListener"/> to unsubscribe.
    /// </para>
    /// </remarks>
    public JsHandle AddEventListener(JsHandle target, string eventName, Action<JsHandle> callback) {
        var adapter = new EventCallbackAdapter(callback);
        var dotNetRef = DotNetObjectReference.Create(adapter);
        var listenerId = SyncRuntime.Invoke<IJSObjectReference>(
            "browserApi.addEventListener", target.Value, eventName, dotNetRef);
        return new JsHandle(listenerId);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Calls <c>browserApi.removeEventListener(target, eventName, listenerId)</c> synchronously.
    /// Requires <see cref="IJSInProcessRuntime"/> (Blazor WebAssembly only).
    /// </remarks>
    public void RemoveEventListener(JsHandle target, string eventName, JsHandle listenerId) {
        SyncRuntime.InvokeVoid("browserApi.removeEventListener", target.Value, eventName, listenerId.Value);
    }

    /// <summary>
    /// Disposes the backend. This is a no-op because the <see cref="JSInteropBackend"/>
    /// does not own any resources that require cleanup at the backend level.
    /// Individual <see cref="JsHandle"/> references are disposed separately via
    /// <see cref="DisposeHandle"/>.
    /// </summary>
    /// <returns>A completed <see cref="ValueTask"/>.</returns>
    public async ValueTask DisposeAsync() {
        // Nothing to dispose at the backend level
        await ValueTask.CompletedTask;
    }

    /// <summary>
    /// Internal adapter that bridges a JavaScript event callback to a managed
    /// <see cref="Action{JsHandle}"/> delegate.
    /// </summary>
    /// <remarks>
    /// An instance of this class is wrapped in a <see cref="DotNetObjectReference{T}"/>
    /// and passed to JavaScript. When the event fires, JavaScript calls the
    /// <see cref="OnEvent"/> method via the <c>[JSInvokable]</c> attribute, which in
    /// turn invokes the original .NET callback with a <see cref="JsHandle"/> wrapping
    /// the JavaScript event object.
    /// </remarks>
    private sealed class EventCallbackAdapter {
        private readonly Action<JsHandle> _callback;

        /// <summary>
        /// Initializes a new <see cref="EventCallbackAdapter"/> with the specified callback.
        /// </summary>
        /// <param name="callback">
        /// The managed callback to invoke when the JavaScript event fires.
        /// </param>
        public EventCallbackAdapter(Action<JsHandle> callback) {
            _callback = callback;
        }

        /// <summary>
        /// Called by JavaScript when the subscribed event fires.
        /// </summary>
        /// <param name="eventRef">
        /// A JavaScript object reference representing the event object (e.g., <c>MouseEvent</c>,
        /// <c>KeyboardEvent</c>). This is wrapped in a <see cref="JsHandle"/> before being
        /// passed to the managed callback.
        /// </param>
        [JSInvokable]
        public void OnEvent(IJSObjectReference eventRef) {
            _callback(new JsHandle(eventRef));
        }
    }
}
