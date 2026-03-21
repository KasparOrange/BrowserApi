using BrowserApi.Events;

namespace BrowserApi.Tests.Events;

public class ModifiersTests {
    [Fact]
    public void Flags_combine_correctly() {
        var mods = Modifiers.Ctrl | Modifiers.Shift;
        Assert.True(mods.HasFlag(Modifiers.Ctrl));
        Assert.True(mods.HasFlag(Modifiers.Shift));
        Assert.False(mods.HasFlag(Modifiers.Alt));
        Assert.False(mods.HasFlag(Modifiers.Meta));
    }

    [Fact]
    public void None_has_no_flags() {
        Assert.False(Modifiers.None.HasFlag(Modifiers.Ctrl));
    }

    [Fact]
    public void All_modifiers_combine() {
        var all = Modifiers.Ctrl | Modifiers.Shift | Modifiers.Alt | Modifiers.Meta;
        Assert.True(all.HasFlag(Modifiers.Ctrl));
        Assert.True(all.HasFlag(Modifiers.Shift));
        Assert.True(all.HasFlag(Modifiers.Alt));
        Assert.True(all.HasFlag(Modifiers.Meta));
    }
}
