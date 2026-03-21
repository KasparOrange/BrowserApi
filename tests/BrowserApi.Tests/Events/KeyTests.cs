using BrowserApi.Common;
using BrowserApi.Events;

namespace BrowserApi.Tests.Events;

public class KeyTests {
    [Theory]
    [InlineData(Key.Enter, "Enter")]
    [InlineData(Key.ArrowUp, "ArrowUp")]
    [InlineData(Key.Space, " ")]
    [InlineData(Key.Escape, "Escape")]
    [InlineData(Key.A, "a")]
    [InlineData(Key.Z, "z")]
    [InlineData(Key.D0, "0")]
    [InlineData(Key.F1, "F1")]
    [InlineData(Key.F12, "F12")]
    public void Key_enum_has_correct_string_values(Key key, string expected) {
        Assert.Equal(expected, key.ToStringValue());
    }

    [Theory]
    [InlineData(KeyCode.KeyA, "KeyA")]
    [InlineData(KeyCode.Digit0, "Digit0")]
    [InlineData(KeyCode.Enter, "Enter")]
    [InlineData(KeyCode.Space, "Space")]
    [InlineData(KeyCode.ArrowUp, "ArrowUp")]
    [InlineData(KeyCode.ShiftLeft, "ShiftLeft")]
    public void KeyCode_enum_has_correct_string_values(KeyCode code, string expected) {
        Assert.Equal(expected, code.ToStringValue());
    }
}
