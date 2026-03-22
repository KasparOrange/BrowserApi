using BrowserApi.Common;
using BrowserApi.Css;
using BrowserApi.Dom;
using BrowserApi.Events;
using BrowserApi.Tests.Common;

namespace BrowserApi.Tests.Common;

[Collection("JsObject")]
public class JsObjectConversionTests : IDisposable {
    private readonly MockBrowserBackend _mock;

    public JsObjectConversionTests() {
        _mock = new MockBrowserBackend();
        JsObject.Backend = _mock;
    }

    public void Dispose() { }

    // ── ConvertFromJs ───────────────────────────────────────────────────

    [Fact]
    public void ConvertFromJs_with_null_returns_default_for_reference_type() {
        var result = JsObject.ConvertFromJs<string>(null);
        Assert.Null(result);
    }

    [Fact]
    public void ConvertFromJs_with_null_returns_default_for_value_type() {
        var result = JsObject.ConvertFromJs<int>(null);
        Assert.Equal(0, result);
    }

    [Fact]
    public void ConvertFromJs_with_JsObject_subclass_wraps_handle() {
        var handle = new JsHandle("element-ref");
        var result = JsObject.ConvertFromJs<Element>(handle);

        Assert.NotNull(result);
        Assert.Equal("element-ref", result.Handle.Value);
    }

    [Fact]
    public void ConvertFromJs_with_JsHandle_wraps_in_JsObject_subclass() {
        var handle = new JsHandle(new object());
        var result = JsObject.ConvertFromJs<Window>(handle);

        Assert.NotNull(result);
        Assert.False(result.Handle.IsEmpty);
    }

    [Fact]
    public void ConvertFromJs_with_non_JsHandle_object_wraps_as_handle() {
        // When raw is not a JsHandle but target is JsObject, it wraps in new JsHandle(raw)
        var rawRef = new object();
        var result = JsObject.ConvertFromJs<Element>(rawRef);

        Assert.NotNull(result);
        Assert.Same(rawRef, result.Handle.Value);
    }

    [Fact]
    public void ConvertFromJs_with_primitive_passes_through() {
        var result = JsObject.ConvertFromJs<int>(42);
        Assert.Equal(42, result);
    }

    [Fact]
    public void ConvertFromJs_with_string_passes_through() {
        var result = JsObject.ConvertFromJs<string>("hello");
        Assert.Equal("hello", result);
    }

    [Fact]
    public void ConvertFromJs_converts_numeric_types() {
        var result = JsObject.ConvertFromJs<double>(42);
        Assert.Equal(42.0, result);
    }

    [Fact]
    public void ConvertFromJs_with_enum_string_matches_StringValue() {
        var result = JsObject.ConvertFromJs<PointerType>("mouse");
        Assert.Equal(PointerType.Mouse, result);
    }

    [Fact]
    public void ConvertFromJs_with_enum_string_falls_back_to_name_parsing() {
        var result = JsObject.ConvertFromJs<PointerType>("Mouse");
        Assert.Equal(PointerType.Mouse, result);
    }

    // ── ConvertToJs ─────────────────────────────────────────────────────

    [Fact]
    public void ConvertToJs_with_null_returns_null() {
        var result = JsObject.ConvertToJs(null);
        Assert.Null(result);
    }

    [Fact]
    public void ConvertToJs_with_CssColor_returns_ToCss_string() {
        var color = CssColor.Rgb(255, 0, 0);
        var result = JsObject.ConvertToJs(color);

        Assert.IsType<string>(result);
        Assert.Equal(color.ToCss(), result);
    }

    [Fact]
    public void ConvertToJs_with_JsObject_returns_Handle() {
        var handleObj = new object();
        var element = new Element { Handle = new JsHandle(handleObj) };

        var result = JsObject.ConvertToJs(element);

        Assert.IsType<JsHandle>(result);
        var handle = (JsHandle)result;
        Assert.Same(handleObj, handle.Value);
    }

    [Fact]
    public void ConvertToJs_with_primitive_passes_through() {
        var result = JsObject.ConvertToJs(42);
        Assert.Equal(42, result);
    }

    [Fact]
    public void ConvertToJs_with_string_passes_through() {
        var result = JsObject.ConvertToJs("hello");
        Assert.Equal("hello", result);
    }

    [Fact]
    public void ConvertToJs_with_enum_having_StringValue_returns_string() {
        var result = JsObject.ConvertToJs(PointerType.Mouse);
        Assert.Equal("mouse", result);
    }

    [Fact]
    public void ConvertToJs_with_ICssValue_returns_ToCss() {
        var length = Length.Rem(2.5);
        var result = JsObject.ConvertToJs(length);

        Assert.Equal("2.5rem", result);
    }
}
