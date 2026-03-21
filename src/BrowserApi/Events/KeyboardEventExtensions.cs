using BrowserApi.Common;

namespace BrowserApi.Events;

public static class KeyboardEventExtensions {
    public static Modifiers GetModifiers(this KeyboardEvent e) {
        var m = Modifiers.None;
        if (e.CtrlKey) m |= Modifiers.Ctrl;
        if (e.ShiftKey) m |= Modifiers.Shift;
        if (e.AltKey) m |= Modifiers.Alt;
        if (e.MetaKey) m |= Modifiers.Meta;
        return m;
    }

    public static bool HasModifier(this KeyboardEvent e, Modifiers modifier) =>
        (e.GetModifiers() & modifier) == modifier;

    public static bool IsKey(this KeyboardEvent e, Key key) =>
        e.Key == key.ToStringValue();

    public static bool IsCode(this KeyboardEvent e, KeyCode code) =>
        e.Code == code.ToStringValue();
}
