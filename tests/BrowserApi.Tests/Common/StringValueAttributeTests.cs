using BrowserApi.Common;

namespace BrowserApi.Tests.Common;

public class StringValueAttributeTests {
    public enum TestDirection {
        [StringValue("row")]
        Row,

        [StringValue("row-reverse")]
        RowReverse,

        [StringValue("column")]
        Column,

        [StringValue("")]
        Empty,

        NoAttribute
    }

    [Fact]
    public void Attribute_stores_value() {
        var attr = new StringValueAttribute("row-reverse");
        Assert.Equal("row-reverse", attr.Value);
    }

    [Theory]
    [InlineData(TestDirection.Row, "row")]
    [InlineData(TestDirection.RowReverse, "row-reverse")]
    [InlineData(TestDirection.Column, "column")]
    [InlineData(TestDirection.Empty, "")]
    public void ToStringValue_returns_attribute_value(TestDirection value, string expected) {
        Assert.Equal(expected, value.ToStringValue());
    }

    [Fact]
    public void ToStringValue_falls_back_to_name_when_no_attribute() {
        Assert.Equal("NoAttribute", TestDirection.NoAttribute.ToStringValue());
    }
}
