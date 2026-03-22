using BrowserApi.Common;
using BrowserApi.Dom;
using BrowserApi.Tests.Common;

namespace BrowserApi.Tests.Dom;

[Collection("JsObject")]
public class BulkQueryExtensionsTests : IDisposable {
    private readonly MockBrowserBackend _mock;
    private readonly Document _document;
    private readonly Element _element;

    public BulkQueryExtensionsTests() {
        _mock = new MockBrowserBackend();
        JsObject.Backend = _mock;
        _document = new Document { Handle = new JsHandle(new object()) };
        _element = new Element { Handle = new JsHandle(new object()) };
    }

    public void Dispose() { }

    [Fact]
    public async Task QueryValuesAsync_on_Document_calls_queryProperty() {
        _mock.InvokeAsyncReturnValue = new object?[] { "Item 1", "Item 2", "Item 3" };

        var result = await _document.QueryValuesAsync<string>("li", "textContent");

        Assert.Equal(3, result.Length);
        Assert.Equal("Item 1", result[0]);
        Assert.Equal("Item 2", result[1]);
        Assert.Equal("Item 3", result[2]);

        Assert.Contains(_mock.Calls, c => c.Method == "InvokeAsync" && c.Name == "queryProperty");
    }

    [Fact]
    public async Task QueryValuesAsync_on_Element_calls_queryProperty() {
        _mock.InvokeAsyncReturnValue = new object?[] { "a", "b" };

        var result = await _element.QueryValuesAsync<string>("span", "textContent");

        Assert.Equal(2, result.Length);
        Assert.Contains(_mock.Calls, c => c.Name == "queryProperty");
    }

    [Fact]
    public async Task QueryValuesAsync_returns_empty_array_for_null() {
        _mock.InvokeAsyncReturnValue = null;

        var result = await _document.QueryValuesAsync<string>("li", "textContent");

        Assert.Empty(result);
    }

    [Fact]
    public async Task QueryElementsAsync_returns_elements_with_handles() {
        var handle1 = new object();
        var handle2 = new object();
        _mock.InvokeAsyncReturnValue = new object?[] { handle1, handle2 };

        var result = await _document.QueryElementsAsync("li");

        Assert.Equal(2, result.Length);
        Assert.All(result, el => Assert.IsType<Element>(el));
        Assert.All(result, el => Assert.False(el.Handle.IsEmpty));
    }

    [Fact]
    public async Task QueryElementsAsync_returns_empty_for_no_matches() {
        _mock.InvokeAsyncReturnValue = null;

        var result = await _document.QueryElementsAsync(".nonexistent");

        Assert.Empty(result);
    }

    [Fact]
    public async Task QueryValuesAsync_enables_LINQ_chain() {
        _mock.InvokeAsyncReturnValue = new object?[] { "Alpha", "Beta", "Gamma", "Delta" };

        var result = await _document.QueryValuesAsync<string>("li", "textContent");

        // Pure C# LINQ — zero interop calls
        var filtered = result
            .Where(t => t.Length > 4)
            .OrderByDescending(t => t)
            .ToList();

        Assert.Equal(3, filtered.Count);
        Assert.Equal("Gamma", filtered[0]);
        Assert.Equal("Delta", filtered[1]);
        Assert.Equal("Alpha", filtered[2]);
    }

    [Fact]
    public async Task Full_fetch_linq_batch_pattern() {
        // Step 1: Bulk fetch (1 interop call)
        _mock.InvokeAsyncReturnValue = new object?[] { "Item A", "Item B", "Item C" };
        var texts = await _document.QueryValuesAsync<string>("li", "textContent");

        // Step 2: Pure C# LINQ (0 interop calls)
        var processed = texts.Select(t => t.ToUpperInvariant()).ToList();

        Assert.Equal("ITEM A", processed[0]);
        Assert.Equal("ITEM B", processed[1]);
        Assert.Equal("ITEM C", processed[2]);

        // Step 3: Batch write (1 interop call) — verified separately in JsBatchTests
        // Total: 2 interop calls instead of 3 reads + 3 writes = 6
    }
}
