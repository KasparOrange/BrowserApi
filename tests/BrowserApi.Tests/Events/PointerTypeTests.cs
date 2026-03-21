using BrowserApi.Common;
using BrowserApi.Events;

namespace BrowserApi.Tests.Events;

public class PointerTypeTests {
    [Theory]
    [InlineData(PointerType.Mouse, "mouse")]
    [InlineData(PointerType.Pen, "pen")]
    [InlineData(PointerType.Touch, "touch")]
    public void PointerType_string_values_match_dom(PointerType type, string expected) {
        Assert.Equal(expected, type.ToStringValue());
    }
}
