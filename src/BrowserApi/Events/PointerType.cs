using BrowserApi.Common;

namespace BrowserApi.Events;

public enum PointerType {
    [StringValue("mouse")] Mouse,
    [StringValue("pen")] Pen,
    [StringValue("touch")] Touch,
}
