using BrowserApi.Common;
using BrowserApi.Runtime.VirtualDom;

namespace BrowserApi.Runtime.Tests;

public class JintBackendTests {
    private readonly VirtualDocument _doc;
    private readonly VirtualConsole _console;
    private readonly JintBackend _backend;

    public JintBackendTests() {
        _doc = new VirtualDocument();
        _console = new VirtualConsole();
        _backend = new JintBackend(_doc, _console);
    }

    // --- GetProperty ---

    [Fact]
    public void GetProperty_dispatches_to_GetJsProperty() {
        var el = new VirtualElement("div");
        el.Id = "test-id";
        var handle = new JsHandle(el);

        var result = _backend.GetProperty<string>(handle, "id");
        Assert.Equal("test-id", result);
    }

    [Fact]
    public void GetProperty_returns_tagName() {
        var el = new VirtualElement("span");
        var handle = new JsHandle(el);

        var result = _backend.GetProperty<string>(handle, "tagName");
        Assert.Equal("SPAN", result);
    }

    [Fact]
    public void GetProperty_returns_textContent() {
        var el = new VirtualElement("p");
        el.TextContent = "hello";
        var handle = new JsHandle(el);

        var result = _backend.GetProperty<string>(handle, "textContent");
        Assert.Equal("hello", result);
    }

    [Fact]
    public void GetProperty_returns_nodeType_as_int() {
        var el = new VirtualElement("div");
        var handle = new JsHandle(el);

        var result = _backend.GetProperty<int>(handle, "nodeType");
        Assert.Equal(1, result);
    }

    [Fact]
    public void GetProperty_non_node_target_returns_default() {
        var handle = new JsHandle("not a node");

        var result = _backend.GetProperty<string>(handle, "id");
        Assert.Null(result);
    }

    [Fact]
    public void GetProperty_null_target_value_returns_default() {
        var handle = new JsHandle(null);

        var result = _backend.GetProperty<string>(handle, "id");
        Assert.Null(result);
    }

    [Fact]
    public void GetProperty_style_returns_VirtualStyle() {
        var el = new VirtualElement("div");
        el.Style["color"] = "red";
        var handle = new JsHandle(el);

        var result = _backend.GetProperty<object>(handle, "style");
        Assert.IsType<VirtualStyle>(result);
    }

    // --- SetProperty ---

    [Fact]
    public void SetProperty_dispatches_to_SetJsProperty() {
        var el = new VirtualElement("div");
        var handle = new JsHandle(el);

        _backend.SetProperty(handle, "id", "my-id");
        Assert.Equal("my-id", el.Id);
    }

    [Fact]
    public void SetProperty_className() {
        var el = new VirtualElement("div");
        var handle = new JsHandle(el);

        _backend.SetProperty(handle, "className", "card active");
        Assert.Equal("card active", el.ClassName);
    }

    [Fact]
    public void SetProperty_textContent() {
        var el = new VirtualElement("div");
        var handle = new JsHandle(el);

        _backend.SetProperty(handle, "textContent", "new text");
        Assert.Equal("new text", el.TextContent);
    }

    [Fact]
    public void SetProperty_non_node_target_is_noop() {
        var handle = new JsHandle("not a node");
        // Should not throw
        _backend.SetProperty(handle, "id", "value");
    }

    // --- Invoke ---

    [Fact]
    public void Invoke_dispatches_to_InvokeJsMethod() {
        var handle = new JsHandle(_doc);

        var result = _backend.Invoke<object>(handle, "createElement", [new object[] { "div" }[0]]);
        Assert.IsType<VirtualElement>(result);
    }

    [Fact]
    public void Invoke_querySelector_returns_element() {
        var div = new VirtualElement("div");
        div.ClassName = "target";
        _doc.Body.AppendChild(div);

        var handle = new JsHandle(_doc);
        var result = _backend.Invoke<object>(handle, "querySelector", [".target"]);
        Assert.IsType<VirtualElement>(result);
        Assert.Same(div, result);
    }

    [Fact]
    public void Invoke_non_node_target_returns_default() {
        var handle = new JsHandle("not a node");
        var result = _backend.Invoke<object>(handle, "someMethod", []);
        Assert.Null(result);
    }

    // --- InvokeVoid ---

    [Fact]
    public void InvokeVoid_dispatches_to_InvokeJsMethod() {
        var parent = new VirtualElement("div");
        var child = new VirtualElement("span");
        var handle = new JsHandle(parent);

        _backend.InvokeVoid(handle, "appendChild", [child]);
        Assert.Single(parent.ChildNodes);
        Assert.Same(child, parent.ChildNodes[0]);
    }

    [Fact]
    public void InvokeVoid_non_node_target_is_noop() {
        var handle = new JsHandle("not a node");
        // Should not throw
        _backend.InvokeVoid(handle, "someMethod", []);
    }

    // --- InvokeAsync ---

    [Fact]
    public async Task InvokeAsync_is_sync_wrapper_returning_completed_task() {
        var handle = new JsHandle(_doc);
        var task = _backend.InvokeAsync<object>(handle, "createElement", ["p"]);

        Assert.True(task.IsCompleted);
        var result = await task;
        Assert.IsType<VirtualElement>(result);
    }

    // --- InvokeVoidAsync ---

    [Fact]
    public async Task InvokeVoidAsync_is_sync_wrapper_returning_completed_task() {
        var parent = new VirtualElement("div");
        var child = new VirtualElement("span");
        var handle = new JsHandle(parent);

        var task = _backend.InvokeVoidAsync(handle, "appendChild", [child]);
        Assert.True(task.IsCompleted);
        await task;

        Assert.Single(parent.ChildNodes);
    }

    // --- GetGlobal ---

    [Fact]
    public void GetGlobal_document_returns_document_handle() {
        var handle = _backend.GetGlobal("document");
        Assert.False(handle.IsEmpty);
    }

    [Fact]
    public void GetGlobal_window_returns_document_handle() {
        var handle = _backend.GetGlobal("window");
        Assert.False(handle.IsEmpty);
    }

    [Fact]
    public void GetGlobal_console_returns_console_handle() {
        var handle = _backend.GetGlobal("console");
        Assert.False(handle.IsEmpty);
    }

    [Fact]
    public void GetGlobal_browserApi_returns_non_empty_handle() {
        var handle = _backend.GetGlobal("browserApi");
        Assert.False(handle.IsEmpty);
    }

    [Fact]
    public void GetGlobal_unknown_returns_handle_wrapping_null() {
        var handle = _backend.GetGlobal("unknownGlobal");
        Assert.True(handle.IsEmpty);
    }

    [Fact]
    public void GetGlobal_document_is_usable_as_node() {
        var handle = _backend.GetGlobal("document");
        // Should be able to call createElement
        var result = _backend.Invoke<object>(handle, "createElement", ["div"]);
        Assert.IsType<VirtualElement>(result);
    }

    [Fact]
    public void GetGlobal_console_log_captures_message() {
        var handle = _backend.GetGlobal("console");
        _backend.InvokeVoid(handle, "log", ["test message"]);

        Assert.Single(_console.Messages);
        Assert.Equal("test message", _console.Messages[0].Text);
    }

    // --- UnwrapArgs ---

    [Fact]
    public void UnwrapArgs_extracts_JsHandle_values() {
        var el = new VirtualElement("div");
        var parent = new VirtualElement("section");
        var parentHandle = new JsHandle(parent);

        // Pass a JsHandle-wrapped child to appendChild
        var childHandle = new JsHandle(el);
        _backend.InvokeVoid(parentHandle, "appendChild", [childHandle]);

        Assert.Single(parent.ChildNodes);
        Assert.Same(el, parent.ChildNodes[0]);
    }

    [Fact]
    public void UnwrapArgs_passes_non_handle_values_through() {
        var docHandle = _backend.GetGlobal("document");

        // createElement takes a string arg, not a JsHandle
        var result = _backend.Invoke<object>(docHandle, "createElement", ["span"]);
        Assert.IsType<VirtualElement>(result);
        var el = (VirtualElement)result!;
        Assert.Equal("span", el.TagName);
    }

    // --- Construct ---

    [Fact]
    public void Construct_returns_non_empty_handle() {
        var handle = _backend.Construct("SomeClass", []);
        Assert.False(handle.IsEmpty);
    }

    // --- DisposeHandle ---

    [Fact]
    public async Task DisposeHandle_completes_immediately() {
        var handle = new JsHandle(new VirtualElement("div"));
        var task = _backend.DisposeHandle(handle);
        Assert.True(task.IsCompleted);
        await task;
    }

    // --- DisposeAsync ---

    [Fact]
    public async Task DisposeAsync_completes_immediately() {
        var task = _backend.DisposeAsync();
        Assert.True(task.IsCompleted);
        await task;
    }

    // --- AddEventListener / RemoveEventListener ---

    [Fact]
    public void AddEventListener_returns_handle() {
        var el = new VirtualElement("button");
        var handle = new JsHandle(el);

        var listenerId = _backend.AddEventListener(handle, "click", _ => { });
        Assert.False(listenerId.IsEmpty);
    }

    [Fact]
    public void RemoveEventListener_does_not_throw() {
        var el = new VirtualElement("button");
        var handle = new JsHandle(el);
        var listenerId = _backend.AddEventListener(handle, "click", _ => { });

        // Should not throw
        _backend.RemoveEventListener(handle, "click", listenerId);
    }

    // --- ConvertResult wraps VirtualNode in JsHandle ---

    [Fact]
    public void GetProperty_document_body_returns_VirtualElement() {
        var handle = _backend.GetGlobal("document");
        var result = _backend.GetProperty<object>(handle, "body");
        Assert.IsType<VirtualElement>(result);
    }

    [Fact]
    public void GetProperty_children_returns_list() {
        var el = new VirtualElement("div");
        el.AppendChild(new VirtualElement("span"));
        var handle = new JsHandle(el);

        var result = _backend.GetProperty<object>(handle, "children");
        Assert.NotNull(result);
    }

    // --- VirtualBrowserApi (internal class) ---

    [Fact]
    public void BrowserApi_global_GetJsProperty_returns_null() {
        var handle = _backend.GetGlobal("browserApi");
        var result = _backend.GetProperty<object>(handle, "anything");
        Assert.Null(result);
    }

    [Fact]
    public void BrowserApi_global_InvokeJsMethod_returns_null() {
        var handle = _backend.GetGlobal("browserApi");
        var result = _backend.Invoke<object>(handle, "anyMethod", []);
        Assert.Null(result);
    }

    [Fact]
    public void BrowserApi_global_SetJsProperty_is_noop() {
        var handle = _backend.GetGlobal("browserApi");
        // Should not throw
        _backend.SetProperty(handle, "anything", "value");
    }
}
