using BrowserApi.Common;
using BrowserApi.Dom;
using BrowserApi.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BrowserApi.Blazor;

public abstract class BrowserApiComponentBase : ComponentBase, IAsyncDisposable {
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    private JSInteropBackend? _backend;
    private Window? _window;
    private Document? _document;

    protected bool IsBrowserApiReady => _backend is not null;

    protected Window Window => _window ?? throw new InvalidOperationException(
        "Window is not available until OnBrowserApiReadyAsync. JS interop is not available during OnInitialized.");

    protected Document Document => _document ?? throw new InvalidOperationException(
        "Document is not available until OnBrowserApiReadyAsync. JS interop is not available during OnInitialized.");

    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender) {
            _backend = new JSInteropBackend(JsRuntime);
            JsObject.Backend = _backend;

            _window = new Window { Handle = _backend.GetGlobal("window") };
            _document = new Document { Handle = _backend.GetGlobal("document") };

            await OnBrowserApiReadyAsync();
        }
    }

    protected virtual Task OnBrowserApiReadyAsync() => Task.CompletedTask;

    public virtual async ValueTask DisposeAsync() {
        if (_backend is not null)
            await _backend.DisposeAsync();
    }
}
