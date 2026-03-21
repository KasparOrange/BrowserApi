using BrowserApi.Common;

namespace BrowserApi.Events;

public static class PointerEventExtensions {
    public static PointerType? GetPointerType(this PointerEvent e) =>
        e.PointerType switch {
            "mouse" => Events.PointerType.Mouse,
            "pen" => Events.PointerType.Pen,
            "touch" => Events.PointerType.Touch,
            _ => null,
        };

    public static bool IsPointerType(this PointerEvent e, PointerType type) =>
        e.PointerType == type.ToStringValue();
}
