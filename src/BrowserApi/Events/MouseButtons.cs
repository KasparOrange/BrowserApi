namespace BrowserApi.Events;

[Flags]
public enum MouseButtons : ushort {
    None = 0,
    Left = 1,
    Right = 2,
    Middle = 4,
    Back = 8,
    Forward = 16,
}
