namespace BrowserApi.Events;

[Flags]
public enum Modifiers {
    None = 0,
    Ctrl = 1,
    Shift = 2,
    Alt = 4,
    Meta = 8,
}
