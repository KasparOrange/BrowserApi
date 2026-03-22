using BrowserApi.Common;
using BrowserApi.Dom;
using BrowserApi.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BrowserApi.Blazor;

/// <summary>
/// Base class for Blazor components that need to interact with browser APIs through
/// BrowserApi's typed wrappers.
/// </summary>
/// <remarks>
/// <para>
/// This component base class handles the boilerplate of initializing the BrowserApi
/// interop layer. It creates a <see cref="JSInteropBackend"/>, assigns it to
/// <see cref="JsObject.Backend"/>, and provides ready-to-use <see cref="Window"/> and
/// <see cref="Document"/> properties -- all automatically on first render.
/// </para>
/// <para>
/// <b>Lifecycle:</b> JavaScript interop is not available during <c>OnInitialized</c> or
/// <c>OnInitializedAsync</c> in Blazor. This class initializes the backend in
/// <see cref="ComponentBase.OnAfterRenderAsync"/>, which runs after the component has
/// been rendered to the DOM. Override <see cref="OnBrowserApiReadyAsync"/> to perform
/// initial browser API calls once the interop layer is ready.
/// </para>
/// <para>
/// <b>Disposal:</b> This class implements <see cref="IAsyncDisposable"/> to clean up
/// the <see cref="JSInteropBackend"/> when the component is removed from the render tree.
/// Override <see cref="DisposeAsync"/> if you need to perform additional cleanup, but
/// always call the base implementation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// @inherits BrowserApiComponentBase
///
/// &lt;h1&gt;@_title&lt;/h1&gt;
///
/// @code {
///     private string _title = "Loading...";
///
///     protected override async Task OnBrowserApiReadyAsync() {
///         _title = Document.Title;
///         StateHasChanged();
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="JSInteropBackend"/>
/// <seealso cref="JsObject"/>
/// <seealso cref="ServiceCollectionExtensions"/>
public abstract class BrowserApiComponentBase : ComponentBase, IAsyncDisposable {
    /// <summary>
    /// Gets or sets the Blazor JS runtime, injected by the dependency injection container.
    /// </summary>
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    private JSInteropBackend? _backend;
    private Window? _window;
    private Document? _document;

    /// <summary>
    /// Gets a value indicating whether the BrowserApi interop layer has been initialized
    /// and is ready for use.
    /// </summary>
    /// <value>
    /// <see langword="true"/> after the first render, when <see cref="Window"/> and
    /// <see cref="Document"/> are available; <see langword="false"/> before that.
    /// </value>
    /// <remarks>
    /// Use this property to guard browser API calls in render logic or event handlers
    /// that may execute before <see cref="OnBrowserApiReadyAsync"/> has been called.
    /// </remarks>
    protected bool IsBrowserApiReady => _backend is not null;

    /// <summary>
    /// Gets the <c>window</c> global object, providing access to browser window APIs
    /// such as <c>location</c>, <c>history</c>, <c>localStorage</c>, and more.
    /// </summary>
    /// <value>A typed <see cref="Dom.Window"/> wrapper around the JavaScript <c>window</c> object.</value>
    /// <exception cref="InvalidOperationException">
    /// Thrown if accessed before the interop layer is ready (i.e., before
    /// <see cref="OnBrowserApiReadyAsync"/> has been called). This typically happens
    /// when you try to access <see cref="Window"/> during <c>OnInitialized</c> or
    /// <c>OnInitializedAsync</c>, where JS interop is not yet available.
    /// </exception>
    protected Window Window => _window ?? throw new InvalidOperationException(
        "Window is not available until OnBrowserApiReadyAsync. JS interop is not available during OnInitialized.");

    /// <summary>
    /// Gets the <c>document</c> global object, providing access to DOM manipulation APIs
    /// such as <c>getElementById</c>, <c>createElement</c>, <c>querySelector</c>, and more.
    /// </summary>
    /// <value>A typed <see cref="Dom.Document"/> wrapper around the JavaScript <c>document</c> object.</value>
    /// <exception cref="InvalidOperationException">
    /// Thrown if accessed before the interop layer is ready (i.e., before
    /// <see cref="OnBrowserApiReadyAsync"/> has been called). This typically happens
    /// when you try to access <see cref="Document"/> during <c>OnInitialized</c> or
    /// <c>OnInitializedAsync</c>, where JS interop is not yet available.
    /// </exception>
    protected Document Document => _document ?? throw new InvalidOperationException(
        "Document is not available until OnBrowserApiReadyAsync. JS interop is not available during OnInitialized.");

    /// <summary>
    /// Initializes the BrowserApi interop layer on first render by creating the
    /// <see cref="JSInteropBackend"/>, setting it as the global <see cref="JsObject.Backend"/>,
    /// and obtaining handles to the <c>window</c> and <c>document</c> globals.
    /// </summary>
    /// <param name="firstRender">
    /// <see langword="true"/> on the first render; <see langword="false"/> on subsequent renders.
    /// </param>
    /// <returns>A task representing the asynchronous initialization.</returns>
    /// <remarks>
    /// This method calls <see cref="OnBrowserApiReadyAsync"/> on the first render after
    /// initialization is complete. Override <see cref="OnBrowserApiReadyAsync"/> instead
    /// of this method if you need to run code once the browser APIs are available.
    /// </remarks>
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender) {
            _backend = new JSInteropBackend(JsRuntime);
            JsObject.Backend = _backend;

            _window = new Window { Handle = _backend.GetGlobal("window") };
            _document = new Document { Handle = _backend.GetGlobal("document") };

            await OnBrowserApiReadyAsync();
        }
    }

    /// <summary>
    /// Called once after the first render, when the BrowserApi interop layer is fully
    /// initialized and the <see cref="Window"/> and <see cref="Document"/> properties
    /// are available.
    /// </summary>
    /// <returns>A task representing any asynchronous initialization work.</returns>
    /// <remarks>
    /// <para>
    /// Override this method to perform initial browser API operations, such as reading
    /// element dimensions, setting up event listeners, or fetching data from browser
    /// storage. This is the earliest point at which JavaScript interop is available in
    /// the Blazor component lifecycle.
    /// </para>
    /// <para>
    /// The default implementation returns <see cref="Task.CompletedTask"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override async Task OnBrowserApiReadyAsync() {
    ///     var title = Document.Title;
    ///     var width = Window.InnerWidth;
    ///     // ... use browser APIs here
    /// }
    /// </code>
    /// </example>
    protected virtual Task OnBrowserApiReadyAsync() => Task.CompletedTask;

    /// <summary>
    /// Disposes the <see cref="JSInteropBackend"/> and releases any associated resources.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that completes when disposal is finished.</returns>
    /// <remarks>
    /// Override this method to perform additional cleanup (e.g., removing event listeners),
    /// but always call <c>await base.DisposeAsync()</c> to ensure the backend is properly
    /// disposed.
    /// </remarks>
    public virtual async ValueTask DisposeAsync() {
        if (_backend is not null)
            await _backend.DisposeAsync();
    }
}
