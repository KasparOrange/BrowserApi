namespace BrowserApi.Common;

/// <summary>
/// Defines the low-level transport layer that bridges .NET calls to JavaScript operations.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IBrowserBackend"/> is the central abstraction that decouples the generated
/// browser API types from any specific interop mechanism. The core <c>BrowserApi</c> package
/// has zero dependencies -- it defines only this interface. Concrete implementations live
/// in separate packages:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <b>BrowserApi.JSInterop</b> -- <c>JSInteropBackend</c> uses Blazor's
///       <c>IJSRuntime</c> / <c>IJSInProcessRuntime</c>.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Test doubles</b> -- mock or fake backends for unit testing without a browser.
///     </description>
///   </item>
/// </list>
/// <para>
/// Set the active backend via <see cref="JsObject.Backend"/> at application startup.
/// All <see cref="JsObject"/>-derived types delegate their property access, method
/// invocation, construction, and event handling through this interface.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In a Blazor WebAssembly app:
/// JsObject.Backend = new JSInteropBackend(jsRuntime);
///
/// // In a unit test:
/// JsObject.Backend = new FakeBrowserBackend();
/// </code>
/// </example>
/// <seealso cref="JsObject"/>
/// <seealso cref="JsHandle"/>
public interface IBrowserBackend : IAsyncDisposable {
    /// <summary>
    /// Synchronously reads a property value from a JavaScript object.
    /// </summary>
    /// <typeparam name="T">The expected .NET type of the property value.</typeparam>
    /// <param name="target">The handle to the JavaScript object that owns the property.</param>
    /// <param name="propertyName">The JavaScript property name (camelCase).</param>
    /// <returns>The property value, converted to <typeparamref name="T"/>.</returns>
    /// <remarks>
    /// This method requires a synchronous interop runtime (e.g., Blazor WebAssembly).
    /// It will throw on Blazor Server where only async interop is available.
    /// </remarks>
    T GetProperty<T>(JsHandle target, string propertyName);

    /// <summary>
    /// Synchronously sets a property value on a JavaScript object.
    /// </summary>
    /// <param name="target">The handle to the JavaScript object that owns the property.</param>
    /// <param name="propertyName">The JavaScript property name (camelCase).</param>
    /// <param name="value">
    /// The value to set. May be <see langword="null"/>, a primitive, a <see cref="JsHandle"/>,
    /// or any JSON-serializable object.
    /// </param>
    /// <remarks>
    /// This method requires a synchronous interop runtime (e.g., Blazor WebAssembly).
    /// It will throw on Blazor Server where only async interop is available.
    /// </remarks>
    void SetProperty(JsHandle target, string propertyName, object? value);

    /// <summary>
    /// Synchronously invokes a void method on a JavaScript object.
    /// </summary>
    /// <param name="target">The handle to the JavaScript object that owns the method.</param>
    /// <param name="methodName">The JavaScript method name (camelCase).</param>
    /// <param name="args">The arguments to pass to the method.</param>
    /// <remarks>
    /// This method requires a synchronous interop runtime (e.g., Blazor WebAssembly).
    /// It will throw on Blazor Server where only async interop is available.
    /// </remarks>
    void InvokeVoid(JsHandle target, string methodName, object?[] args);

    /// <summary>
    /// Synchronously invokes a method on a JavaScript object and returns the result.
    /// </summary>
    /// <typeparam name="T">The expected .NET type of the return value.</typeparam>
    /// <param name="target">The handle to the JavaScript object that owns the method.</param>
    /// <param name="methodName">The JavaScript method name (camelCase).</param>
    /// <param name="args">The arguments to pass to the method.</param>
    /// <returns>The method's return value, converted to <typeparamref name="T"/>.</returns>
    /// <remarks>
    /// This method requires a synchronous interop runtime (e.g., Blazor WebAssembly).
    /// It will throw on Blazor Server where only async interop is available.
    /// </remarks>
    T Invoke<T>(JsHandle target, string methodName, object?[] args);

    /// <summary>
    /// Asynchronously invokes a void method on a JavaScript object.
    /// </summary>
    /// <param name="target">The handle to the JavaScript object that owns the method.</param>
    /// <param name="methodName">The JavaScript method name (camelCase).</param>
    /// <param name="args">The arguments to pass to the method.</param>
    /// <returns>A task that completes when the JavaScript method has finished executing.</returns>
    /// <remarks>
    /// This is the preferred invocation style for Blazor Server, where synchronous
    /// interop is not available. It also works on Blazor WebAssembly.
    /// </remarks>
    Task InvokeVoidAsync(JsHandle target, string methodName, object?[] args);

    /// <summary>
    /// Asynchronously invokes a method on a JavaScript object and returns the result.
    /// </summary>
    /// <typeparam name="T">The expected .NET type of the return value.</typeparam>
    /// <param name="target">The handle to the JavaScript object that owns the method.</param>
    /// <param name="methodName">The JavaScript method name (camelCase).</param>
    /// <param name="args">The arguments to pass to the method.</param>
    /// <returns>
    /// A task whose result is the method's return value, converted to <typeparamref name="T"/>.
    /// </returns>
    /// <remarks>
    /// This is the preferred invocation style for Blazor Server, where synchronous
    /// interop is not available. It also works on Blazor WebAssembly.
    /// </remarks>
    Task<T> InvokeAsync<T>(JsHandle target, string methodName, object?[] args);

    /// <summary>
    /// Constructs a new JavaScript object by invoking its constructor with the specified arguments.
    /// </summary>
    /// <param name="jsClassName">
    /// The fully qualified JavaScript class name (e.g., <c>"HTMLCanvasElement"</c>, <c>"URL"</c>).
    /// </param>
    /// <param name="args">The arguments to pass to the JavaScript constructor.</param>
    /// <returns>
    /// A <see cref="JsHandle"/> referencing the newly created JavaScript object.
    /// </returns>
    JsHandle Construct(string jsClassName, object?[] args);

    /// <summary>
    /// Retrieves a handle to a global JavaScript object by name.
    /// </summary>
    /// <param name="name">
    /// The name of the global variable (e.g., <c>"window"</c>, <c>"document"</c>,
    /// <c>"browserApi"</c>).
    /// </param>
    /// <returns>
    /// A <see cref="JsHandle"/> referencing the global JavaScript object.
    /// </returns>
    JsHandle GetGlobal(string name);

    /// <summary>
    /// Releases the backend-specific resources associated with a JavaScript object handle.
    /// </summary>
    /// <param name="handle">The handle to dispose.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the handle has been released.</returns>
    /// <remarks>
    /// For backends that use <c>IJSObjectReference</c> (such as the Blazor backend),
    /// this disposes the underlying object reference so the JavaScript garbage collector
    /// can reclaim the object.
    /// </remarks>
    ValueTask DisposeHandle(JsHandle handle);

    /// <summary>
    /// Registers an event listener on a JavaScript object and returns a handle
    /// representing the subscription.
    /// </summary>
    /// <param name="target">The handle to the JavaScript object to listen on.</param>
    /// <param name="eventName">
    /// The DOM event name (e.g., <c>"click"</c>, <c>"input"</c>, <c>"resize"</c>).
    /// </param>
    /// <param name="callback">
    /// A callback that receives a <see cref="JsHandle"/> referencing the JavaScript event object
    /// when the event fires.
    /// </param>
    /// <returns>
    /// A <see cref="JsHandle"/> representing the listener registration. Pass this handle
    /// to <see cref="RemoveEventListener"/> to unsubscribe.
    /// </returns>
    JsHandle AddEventListener(JsHandle target, string eventName, Action<JsHandle> callback);

    /// <summary>
    /// Removes a previously registered event listener from a JavaScript object.
    /// </summary>
    /// <param name="target">The handle to the JavaScript object the listener was registered on.</param>
    /// <param name="eventName">The DOM event name that was subscribed to.</param>
    /// <param name="listenerId">
    /// The handle returned by <see cref="AddEventListener"/> that identifies the listener
    /// to remove.
    /// </param>
    void RemoveEventListener(JsHandle target, string eventName, JsHandle listenerId);
}
