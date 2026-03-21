using BrowserApi.Css;
using BrowserApi.Dom;
using BrowserApi.Events;

namespace BrowserApi.Tests.Dom;

public class DomHierarchyTests {
    [Fact]
    public void Node_extends_EventTarget() {
        Assert.True(typeof(Node).IsSubclassOf(typeof(EventTarget)));
    }

    [Fact]
    public void Element_extends_Node() {
        Assert.True(typeof(Element).IsSubclassOf(typeof(Node)));
    }

    [Fact]
    public void HtmlElement_extends_Element() {
        Assert.True(typeof(HtmlElement).IsSubclassOf(typeof(Element)));
    }

    [Fact]
    public void HtmlInputElement_extends_HtmlElement() {
        Assert.True(typeof(HtmlInputElement).IsSubclassOf(typeof(HtmlElement)));
    }

    [Fact]
    public void CssStyleProperties_extends_CssStyleDeclaration() {
        Assert.True(typeof(CssStyleProperties).IsSubclassOf(typeof(CssStyleDeclaration)));
    }

    [Fact]
    public void HtmlElement_has_Style_property() {
        var styleProp = typeof(HtmlElement).GetProperty("Style");
        Assert.NotNull(styleProp);
        Assert.Equal(typeof(CssStyleProperties), styleProp!.PropertyType);
    }

    [Fact]
    public void Event_hierarchy_is_correct() {
        Assert.True(typeof(Uievent).IsSubclassOf(typeof(Event)));
        Assert.True(typeof(MouseEvent).IsSubclassOf(typeof(Uievent)));
        Assert.True(typeof(KeyboardEvent).IsSubclassOf(typeof(Uievent)));
        Assert.True(typeof(PointerEvent).IsSubclassOf(typeof(MouseEvent)));
    }

    [Fact]
    public void HtmlInputElement_has_expected_properties() {
        var props = typeof(HtmlInputElement).GetProperties();
        Assert.Contains(props, p => p.Name == "Value" && p.PropertyType == typeof(string));
        Assert.Contains(props, p => p.Name == "Checked" && p.PropertyType == typeof(bool));
        Assert.Contains(props, p => p.Name == "Disabled" && p.PropertyType == typeof(bool));
    }

    [Fact]
    public void MouseEvent_has_expected_properties() {
        var props = typeof(MouseEvent).GetProperties();
        Assert.Contains(props, p => p.Name == "ClientX");
        Assert.Contains(props, p => p.Name == "ClientY");
        Assert.Contains(props, p => p.Name == "Button");
        Assert.Contains(props, p => p.Name == "Buttons");
        Assert.Contains(props, p => p.Name == "CtrlKey");
    }

    [Fact]
    public void KeyboardEvent_has_expected_properties() {
        var props = typeof(KeyboardEvent).GetProperties();
        Assert.Contains(props, p => p.Name == "Key" && p.PropertyType == typeof(string));
        Assert.Contains(props, p => p.Name == "Code" && p.PropertyType == typeof(string));
        Assert.Contains(props, p => p.Name == "Repeat" && p.PropertyType == typeof(bool));
    }
}
