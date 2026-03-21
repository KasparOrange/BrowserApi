using BrowserApi.Common;

namespace BrowserApi.Tests.Common;

[Collection("JsObject")]
public class JsObjectTests : IDisposable {
    private readonly MockBrowserBackend _mock;

    public JsObjectTests() {
        _mock = new MockBrowserBackend();
        JsObject.Backend = _mock;
    }

    public void Dispose() { }

    private sealed class TestObject : JsObject {
        public TestObject() { }
        public TestObject(JsHandle handle) : base(handle) { }

        public string GetName() => GetProperty<string>("name");
        public void SetName(string value) => SetProperty("name", value);
        public void DoSomething(string arg) => InvokeVoid("doSomething", arg);
        public int Calculate(int a, int b) => Invoke<int>("calculate", a, b);
        public Task DoAsync(string arg) => InvokeVoidAsync("doAsync", arg);
        public Task<string> FetchAsync(string url) => InvokeAsync<string>("fetchAsync", url);
    }

    [Fact]
    public void GetProperty_delegates_to_backend() {
        _mock.PropertyValues["name"] = "test";
        var obj = new TestObject(new JsHandle(new object()));

        var result = obj.GetName();

        Assert.Equal("test", result);
        Assert.Single(_mock.Calls);
        Assert.Equal("GetProperty", _mock.Calls[0].Method);
        Assert.Equal("name", _mock.Calls[0].Name);
    }

    [Fact]
    public void SetProperty_delegates_to_backend() {
        var obj = new TestObject(new JsHandle(new object()));

        obj.SetName("hello");

        Assert.Single(_mock.Calls);
        Assert.Equal("SetProperty", _mock.Calls[0].Method);
        Assert.Equal("name", _mock.Calls[0].Name);
        Assert.Equal("hello", _mock.Calls[0].Args[0]);
    }

    [Fact]
    public void InvokeVoid_delegates_to_backend() {
        var obj = new TestObject(new JsHandle(new object()));

        obj.DoSomething("arg1");

        Assert.Single(_mock.Calls);
        Assert.Equal("InvokeVoid", _mock.Calls[0].Method);
        Assert.Equal("doSomething", _mock.Calls[0].Name);
        Assert.Equal("arg1", _mock.Calls[0].Args[0]);
    }

    [Fact]
    public void Invoke_delegates_to_backend() {
        _mock.InvokeReturnValue = 42;
        var obj = new TestObject(new JsHandle(new object()));

        var result = obj.Calculate(1, 2);

        Assert.Equal(42, result);
        Assert.Single(_mock.Calls);
        Assert.Equal("Invoke", _mock.Calls[0].Method);
        Assert.Equal("calculate", _mock.Calls[0].Name);
    }

    [Fact]
    public async Task InvokeVoidAsync_delegates_to_backend() {
        var obj = new TestObject(new JsHandle(new object()));

        await obj.DoAsync("arg1");

        Assert.Single(_mock.Calls);
        Assert.Equal("InvokeVoidAsync", _mock.Calls[0].Method);
        Assert.Equal("doAsync", _mock.Calls[0].Name);
    }

    [Fact]
    public async Task InvokeAsync_delegates_to_backend() {
        _mock.InvokeAsyncReturnValue = "result";
        var obj = new TestObject(new JsHandle(new object()));

        var result = await obj.FetchAsync("https://example.com");

        Assert.Equal("result", result);
        Assert.Single(_mock.Calls);
        Assert.Equal("InvokeAsync", _mock.Calls[0].Method);
        Assert.Equal("fetchAsync", _mock.Calls[0].Name);
    }

    [Fact]
    public void JsObject_args_converted_to_JsHandle() {
        var child = new TestObject(new JsHandle("child-ref"));
        var parent = new TestObject(new JsHandle(new object()));

        parent.DoSomething("test"); // non-JsObject arg passes through
        var setCall = _mock.Calls[0];
        Assert.Equal("test", setCall.Args[0]);

        _mock.Calls.Clear();

        // Use ConvertToJs directly to verify JsObject conversion
        var converted = JsObject.ConvertToJs(child);
        Assert.IsType<JsHandle>(converted);
        Assert.Equal("child-ref", ((JsHandle)converted).Value);
    }

    [Fact]
    public void ConvertToJs_handles_css_values() {
        var length = BrowserApi.Css.Length.Rem(1.5);
        var converted = JsObject.ConvertToJs(length);
        Assert.Equal("1.5rem", converted);
    }

    [Fact]
    public void ConvertFromJs_wraps_JsHandle_in_JsObject() {
        var handle = new JsHandle("ref-123");
        var result = JsObject.ConvertFromJs<TestObject>(handle);
        Assert.NotNull(result);
        Assert.Equal("ref-123", result.Handle.Value);
    }

    [Fact]
    public void ConvertFromJs_returns_default_for_null() {
        var result = JsObject.ConvertFromJs<string>(null);
        Assert.Null(result);
    }

    [Fact]
    public void JsHandle_equality() {
        var obj = new object();
        var h1 = new JsHandle(obj);
        var h2 = new JsHandle(obj);
        var h3 = new JsHandle(new object());

        Assert.Equal(h1, h2);
        Assert.NotEqual(h1, h3);
        Assert.True(h1 == h2);
        Assert.True(h1 != h3);
    }

    [Fact]
    public void JsHandle_empty() {
        var empty = new JsHandle(null);
        Assert.True(empty.IsEmpty);

        var notEmpty = new JsHandle(new object());
        Assert.False(notEmpty.IsEmpty);
    }
}
