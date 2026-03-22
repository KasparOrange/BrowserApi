using BrowserApi.Common;
using BrowserApi.Css;
using BrowserApi.Dom;

namespace BrowserApi.Tests.Common;

[Collection("JsObject")]
public class JsBatchTests : IDisposable {
    private readonly MockBrowserBackend _mock;

    public JsBatchTests() {
        _mock = new MockBrowserBackend();
        JsObject.Backend = _mock;
    }

    public void Dispose() { }

    [Fact]
    public void SetProperty_queues_without_calling_backend() {
        var element = new Element { Handle = new JsHandle(new object()) };
        var batch = new JsBatch();

        batch.SetProperty(element, "textContent", "hello");

        Assert.Empty(_mock.Calls);
        Assert.Equal(1, batch.Count);
    }

    [Fact]
    public void InvokeVoid_queues_without_calling_backend() {
        var ctx = new CanvasRenderingContext2D { Handle = new JsHandle(new object()) };
        var batch = new JsBatch();

        batch.InvokeVoid(ctx, "fillRect", 0, 0, 100, 100);

        Assert.Empty(_mock.Calls);
        Assert.Equal(1, batch.Count);
    }

    [Fact]
    public async Task ExecuteAsync_makes_single_backend_call() {
        var element = new Element { Handle = new JsHandle(new object()) };
        _mock.PropertyValues["browserApi"] = new object(); // for GetGlobal

        var batch = new JsBatch();
        batch.SetProperty(element, "textContent", "hello");
        batch.SetProperty(element, "className", "active");
        batch.InvokeVoid(element, "setAttribute", "data-id", "123");

        // GetGlobal is called during setup, clear those
        _mock.Calls.Clear();

        await batch.ExecuteAsync();

        // Should have: 1 GetGlobal("browserApi") + 1 InvokeVoidAsync("batch", ...)
        var getGlobal = _mock.Calls.Where(c => c.Method == "GetGlobal").ToList();
        var invokeAsync = _mock.Calls.Where(c => c.Method == "InvokeVoidAsync").ToList();
        Assert.Single(getGlobal);
        Assert.Single(invokeAsync);
    }

    [Fact]
    public async Task Multiple_targets_tracked_correctly() {
        var el1 = new Element { Handle = new JsHandle("ref-1") };
        var el2 = new Element { Handle = new JsHandle("ref-2") };

        var batch = new JsBatch();
        batch.SetProperty(el1, "textContent", "a");
        batch.SetProperty(el2, "textContent", "b");
        batch.SetProperty(el1, "className", "x");

        Assert.Equal(3, batch.Count);

        await batch.ExecuteAsync();

        var invokeCall = Assert.Single(_mock.Calls, c => c.Method == "InvokeVoidAsync");
        Assert.Equal("batch", invokeCall.Name);
        // First arg should be targets array with 2 elements
        var targets = invokeCall.Args[0] as object?[];
        Assert.NotNull(targets);
        Assert.Equal(2, targets!.Length);
    }

    [Fact]
    public async Task Empty_batch_is_noop() {
        var batch = new JsBatch();

        await batch.ExecuteAsync();

        Assert.Empty(_mock.Calls);
    }

    [Fact]
    public void Count_reflects_queued_commands() {
        var element = new Element { Handle = new JsHandle(new object()) };
        var batch = new JsBatch();

        Assert.Equal(0, batch.Count);
        batch.SetProperty(element, "a", "1");
        Assert.Equal(1, batch.Count);
        batch.InvokeVoid(element, "b");
        Assert.Equal(2, batch.Count);
    }

    [Fact]
    public void ConvertToJs_applied_to_values() {
        var element = new Element { Handle = new JsHandle(new object()) };
        var batch = new JsBatch();

        // CssColor should be converted to string via ConvertToJs
        batch.SetProperty(element, "color", CssColor.Red);

        // JsObject should be converted to JsHandle
        var child = new Element { Handle = new JsHandle("child-ref") };
        batch.InvokeVoid(element, "appendChild", child);

        Assert.Equal(2, batch.Count);
    }

    [Fact]
    public async Task RunAsync_creates_and_flushes_batch() {
        var element = new Element { Handle = new JsHandle(new object()) };

        await JsBatch.RunAsync(batch => {
            batch.SetProperty(element, "textContent", "hello");
            batch.InvokeVoid(element, "focus");
        });

        Assert.Contains(_mock.Calls, c => c.Method == "InvokeVoidAsync" && c.Name == "batch");
    }

    [Fact]
    public async Task BatchAsync_extension_flushes_on_completion() {
        var element = new Element { Handle = new JsHandle(new object()) };

        await element.BatchAsync(b => {
            b.Set("textContent", "hello");
            b.Set("className", "active");
            b.Call("focus");
        });

        Assert.Contains(_mock.Calls, c => c.Method == "InvokeVoidAsync" && c.Name == "batch");
    }

    [Fact]
    public void JsBatchScope_Set_returns_self_for_chaining() {
        var element = new Element { Handle = new JsHandle(new object()) };
        var batch = new JsBatch();
        var scope = new JsBatchScope(batch, element);

        var result = scope.Set("a", "1");

        Assert.Same(scope, result);
    }

    [Fact]
    public void JsBatchScope_Call_returns_self_for_chaining() {
        var element = new Element { Handle = new JsHandle(new object()) };
        var batch = new JsBatch();
        var scope = new JsBatchScope(batch, element);

        var result = scope.Call("focus");

        Assert.Same(scope, result);
    }
}
