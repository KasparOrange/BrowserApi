using BrowserApi.Events;

namespace BrowserApi.Tests.Events;

public class MouseButtonTests {
    [Fact]
    public void MouseButton_values_match_dom_spec() {
        Assert.Equal(0, (short)MouseButton.Left);
        Assert.Equal(1, (short)MouseButton.Middle);
        Assert.Equal(2, (short)MouseButton.Right);
        Assert.Equal(3, (short)MouseButton.Back);
        Assert.Equal(4, (short)MouseButton.Forward);
    }

    [Fact]
    public void MouseButtons_flags_match_dom_spec() {
        Assert.Equal((ushort)1, (ushort)MouseButtons.Left);
        Assert.Equal((ushort)2, (ushort)MouseButtons.Right);
        Assert.Equal((ushort)4, (ushort)MouseButtons.Middle);
        Assert.Equal((ushort)8, (ushort)MouseButtons.Back);
        Assert.Equal((ushort)16, (ushort)MouseButtons.Forward);
    }

    [Fact]
    public void MouseButtons_flags_combine() {
        var both = MouseButtons.Left | MouseButtons.Right;
        Assert.True(both.HasFlag(MouseButtons.Left));
        Assert.True(both.HasFlag(MouseButtons.Right));
        Assert.False(both.HasFlag(MouseButtons.Middle));
    }
}
