using System.Reflection;
using BrowserApi.Blazor;
using BrowserApi.Common;
using BrowserApi.Dom;
using Microsoft.JSInterop;

namespace BrowserApi.Tests.Blazor;

[Collection("JsObject")]
public class BrowserApiComponentBaseTests : IDisposable {
    public void Dispose() { }

    private static TestComponent CreateComponent(IJSRuntime? runtime = null) {
        var component = new TestComponent();
        var prop = typeof(BrowserApiComponentBase)
            .GetProperty("JsRuntime", BindingFlags.NonPublic | BindingFlags.Instance)!;
        prop.SetValue(component, runtime ?? new MockJSInProcessRuntime());
        return component;
    }

    [Fact]
    public void Window_throws_before_initialization() {
        var component = CreateComponent();

        var ex = Assert.Throws<InvalidOperationException>(() => _ = component.TestWindow);
        Assert.Contains("OnBrowserApiReadyAsync", ex.Message);
    }

    [Fact]
    public void Document_throws_before_initialization() {
        var component = CreateComponent();

        var ex = Assert.Throws<InvalidOperationException>(() => _ = component.TestDocument);
        Assert.Contains("OnBrowserApiReadyAsync", ex.Message);
    }

    [Fact]
    public void IsBrowserApiReady_is_false_initially() {
        var component = CreateComponent();

        Assert.False(component.TestIsBrowserApiReady);
    }

    [Fact]
    public async Task OnAfterRenderAsync_initializes_backend_and_globals() {
        var mockRuntime = new MockJSInProcessRuntime();
        mockRuntime.ReturnValues["browserApi.getGlobal"] = new object();
        var component = CreateComponent(mockRuntime);

        await component.CallOnAfterRenderAsync(true);

        Assert.True(component.TestIsBrowserApiReady);
        Assert.NotNull(component.TestWindow);
        Assert.NotNull(component.TestDocument);
        Assert.IsType<Window>(component.TestWindow);
        Assert.IsType<Document>(component.TestDocument);
    }

    [Fact]
    public async Task OnBrowserApiReadyAsync_called_on_first_render() {
        var mockRuntime = new MockJSInProcessRuntime();
        mockRuntime.ReturnValues["browserApi.getGlobal"] = new object();
        var component = CreateComponent(mockRuntime);

        await component.CallOnAfterRenderAsync(true);

        Assert.True(component.ReadyCalled);
    }

    [Fact]
    public async Task Subsequent_renders_skip_initialization() {
        var mockRuntime = new MockJSInProcessRuntime();
        mockRuntime.ReturnValues["browserApi.getGlobal"] = new object();
        var component = CreateComponent(mockRuntime);

        await component.CallOnAfterRenderAsync(true);
        component.ReadyCalled = false;

        await component.CallOnAfterRenderAsync(false);

        Assert.False(component.ReadyCalled);
    }

    [Fact]
    public async Task DisposeAsync_disposes_backend() {
        var mockRuntime = new MockJSInProcessRuntime();
        mockRuntime.ReturnValues["browserApi.getGlobal"] = new object();
        var component = CreateComponent(mockRuntime);

        await component.CallOnAfterRenderAsync(true);
        await component.DisposeAsync();

        // No exception means dispose succeeded
    }

    private sealed class TestComponent : BrowserApiComponentBase {
        public Window TestWindow => Window;
        public Document TestDocument => Document;
        public bool TestIsBrowserApiReady => IsBrowserApiReady;
        public bool ReadyCalled { get; set; }

        public Task CallOnAfterRenderAsync(bool firstRender) => OnAfterRenderAsync(firstRender);

        protected override Task OnBrowserApiReadyAsync() {
            ReadyCalled = true;
            return Task.CompletedTask;
        }
    }
}
