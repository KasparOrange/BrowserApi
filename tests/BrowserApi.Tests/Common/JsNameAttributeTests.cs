using System.Reflection;
using BrowserApi.Common;

namespace BrowserApi.Tests.Common;

public class JsNameAttributeTests {
    [JsName("querySelector")]
    private class TestClass {
        [JsName("innerHTML")]
        public string? InnerHtml { get; set; }
    }

    [Fact]
    public void Attribute_stores_name() {
        var attr = new JsNameAttribute("querySelector");
        Assert.Equal("querySelector", attr.Name);
    }

    [Fact]
    public void Retrievable_from_class_via_reflection() {
        var attr = typeof(TestClass).GetCustomAttribute<JsNameAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("querySelector", attr.Name);
    }

    [Fact]
    public void Retrievable_from_property_via_reflection() {
        var prop = typeof(TestClass).GetProperty("InnerHtml");
        var attr = prop?.GetCustomAttribute<JsNameAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("innerHTML", attr.Name);
    }
}
