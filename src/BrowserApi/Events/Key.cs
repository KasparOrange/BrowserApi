using BrowserApi.Common;

namespace BrowserApi.Events;

public enum Key {
    // Modifier keys
    [StringValue("Alt")] Alt,
    [StringValue("AltGraph")] AltGraph,
    [StringValue("CapsLock")] CapsLock,
    [StringValue("Control")] Control,
    [StringValue("Fn")] Fn,
    [StringValue("FnLock")] FnLock,
    [StringValue("Meta")] Meta,
    [StringValue("NumLock")] NumLock,
    [StringValue("ScrollLock")] ScrollLock,
    [StringValue("Shift")] Shift,
    [StringValue("Symbol")] Symbol,
    [StringValue("SymbolLock")] SymbolLock,

    // Whitespace
    [StringValue("Enter")] Enter,
    [StringValue("Tab")] Tab,
    [StringValue(" ")] Space,

    // Navigation
    [StringValue("ArrowDown")] ArrowDown,
    [StringValue("ArrowLeft")] ArrowLeft,
    [StringValue("ArrowRight")] ArrowRight,
    [StringValue("ArrowUp")] ArrowUp,
    [StringValue("End")] End,
    [StringValue("Home")] Home,
    [StringValue("PageDown")] PageDown,
    [StringValue("PageUp")] PageUp,

    // Editing
    [StringValue("Backspace")] Backspace,
    [StringValue("Clear")] Clear,
    [StringValue("Copy")] Copy,
    [StringValue("Cut")] Cut,
    [StringValue("Delete")] Delete,
    [StringValue("Insert")] Insert,
    [StringValue("Paste")] Paste,
    [StringValue("Redo")] Redo,
    [StringValue("Undo")] Undo,

    // UI
    [StringValue("Escape")] Escape,
    [StringValue("ContextMenu")] ContextMenu,
    [StringValue("Help")] Help,
    [StringValue("Pause")] Pause,
    [StringValue("PrintScreen")] PrintScreen,

    // Function keys
    [StringValue("F1")] F1,
    [StringValue("F2")] F2,
    [StringValue("F3")] F3,
    [StringValue("F4")] F4,
    [StringValue("F5")] F5,
    [StringValue("F6")] F6,
    [StringValue("F7")] F7,
    [StringValue("F8")] F8,
    [StringValue("F9")] F9,
    [StringValue("F10")] F10,
    [StringValue("F11")] F11,
    [StringValue("F12")] F12,

    // Letters
    [StringValue("a")] A,
    [StringValue("b")] B,
    [StringValue("c")] C,
    [StringValue("d")] D,
    [StringValue("e")] E,
    [StringValue("f")] F,
    [StringValue("g")] G,
    [StringValue("h")] H,
    [StringValue("i")] I,
    [StringValue("j")] J,
    [StringValue("k")] K,
    [StringValue("l")] L,
    [StringValue("m")] M,
    [StringValue("n")] N,
    [StringValue("o")] O,
    [StringValue("p")] P,
    [StringValue("q")] Q,
    [StringValue("r")] R,
    [StringValue("s")] S,
    [StringValue("t")] T,
    [StringValue("u")] U,
    [StringValue("v")] V,
    [StringValue("w")] W,
    [StringValue("x")] X,
    [StringValue("y")] Y,
    [StringValue("z")] Z,

    // Digits
    [StringValue("0")] D0,
    [StringValue("1")] D1,
    [StringValue("2")] D2,
    [StringValue("3")] D3,
    [StringValue("4")] D4,
    [StringValue("5")] D5,
    [StringValue("6")] D6,
    [StringValue("7")] D7,
    [StringValue("8")] D8,
    [StringValue("9")] D9,
}
