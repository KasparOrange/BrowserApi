namespace BrowserApi.Events;

public static class MouseEventExtensions {
    public static Modifiers GetModifiers(this MouseEvent e) {
        var m = Modifiers.None;
        if (e.CtrlKey) m |= Modifiers.Ctrl;
        if (e.ShiftKey) m |= Modifiers.Shift;
        if (e.AltKey) m |= Modifiers.Alt;
        if (e.MetaKey) m |= Modifiers.Meta;
        return m;
    }

    public static bool HasModifier(this MouseEvent e, Modifiers modifier) =>
        (e.GetModifiers() & modifier) == modifier;

    public static MouseButton GetButton(this MouseEvent e) => (MouseButton)e.Button;

    public static MouseButtons GetButtons(this MouseEvent e) => (MouseButtons)e.Buttons;

    public static bool HasButton(this MouseEvent e, MouseButtons button) =>
        (e.GetButtons() & button) == button;
}
