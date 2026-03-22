using BrowserApi.Common;
using BrowserApi.Dom;
using BrowserApi.Events;
using BrowserApi.Tests.Common;

namespace BrowserApi.Tests.Dom;

[Collection("JsObject")]
public class EventExtensionsTests : IDisposable {
    private readonly MockBrowserBackend _mock;
    private readonly Element _element;

    public EventExtensionsTests() {
        _mock = new MockBrowserBackend();
        JsObject.Backend = _mock;
        _element = new Element { Handle = new JsHandle(new object()) };
    }

    public void Dispose() { }

    [Fact]
    public void OnClick_registers_click_listener() {
        _element.OnClick(_ => { });

        var call = Assert.Single(_mock.Calls, c => c.Method == "AddEventListener");
        Assert.Equal("click", call.Name);
    }

    [Fact]
    public void OnDblClick_registers_dblclick_listener() {
        _element.OnDblClick(_ => { });

        Assert.Contains(_mock.Calls, c => c.Method == "AddEventListener" && c.Name == "dblclick");
    }

    [Fact]
    public void OnKeyDown_registers_keydown_listener() {
        _element.OnKeyDown(_ => { });

        Assert.Contains(_mock.Calls, c => c.Method == "AddEventListener" && c.Name == "keydown");
    }

    [Fact]
    public void OnPointerDown_registers_pointerdown_listener() {
        _element.OnPointerDown(_ => { });

        Assert.Contains(_mock.Calls, c => c.Method == "AddEventListener" && c.Name == "pointerdown");
    }

    [Fact]
    public void OnFocus_registers_focus_listener() {
        _element.OnFocus(_ => { });

        Assert.Contains(_mock.Calls, c => c.Method == "AddEventListener" && c.Name == "focus");
    }

    [Fact]
    public void OnInput_registers_input_listener() {
        _element.OnInput(_ => { });

        Assert.Contains(_mock.Calls, c => c.Method == "AddEventListener" && c.Name == "input");
    }

    [Fact]
    public void On_generic_registers_custom_event() {
        _element.On<MouseEvent>("contextmenu", _ => { });

        Assert.Contains(_mock.Calls, c => c.Method == "AddEventListener" && c.Name == "contextmenu");
    }

    [Fact]
    public void OnClick_returns_EventSubscription() {
        var sub = _element.OnClick(_ => { });

        Assert.NotNull(sub);
        Assert.IsType<EventSubscription>(sub);
    }

    [Fact]
    public void EventSubscription_Dispose_calls_RemoveEventListener() {
        var sub = _element.OnClick(_ => { });

        sub.Dispose();

        Assert.Contains(_mock.Calls, c => c.Method == "RemoveEventListener" && c.Name == "click");
    }

    [Fact]
    public void EventSubscription_Dispose_is_idempotent() {
        var sub = _element.OnClick(_ => { });

        sub.Dispose();
        sub.Dispose();

        Assert.Single(_mock.Calls, c => c.Method == "RemoveEventListener");
    }
}
