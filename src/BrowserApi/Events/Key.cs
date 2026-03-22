using BrowserApi.Common;

namespace BrowserApi.Events;

/// <summary>
/// Represents the logical key values from the
/// <see href="https://www.w3.org/TR/uievents-key/">UI Events KeyboardEvent key Values</see> specification.
/// </summary>
/// <remarks>
/// <para>
/// Each member maps to the <c>KeyboardEvent.key</c> property value via a
/// <see cref="StringValueAttribute"/>. The <c>key</c> property represents the logical
/// meaning of the key, which can change depending on keyboard layout and modifier state
/// (e.g., pressing "A" with Shift produces <c>"A"</c>, without Shift produces <c>"a"</c>).
/// </para>
/// <para>
/// Use <see cref="KeyboardEventExtensions.IsKey"/> to compare a <see cref="KeyboardEvent"/>
/// against a <see cref="Key"/> value. Use <see cref="StringValueExtensions.ToStringValue{TEnum}"/>
/// to get the underlying string for a given enum member.
/// </para>
/// <para>
/// For physical key identification (layout-independent), use <see cref="KeyCode"/> instead.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Check if the Enter key was pressed
/// if (keyboardEvent.IsKey(Key.Enter)) { /* handle enter */ }
///
/// // Get the string value
/// string value = Key.ArrowUp.ToStringValue(); // "ArrowUp"
/// </code>
/// </example>
/// <seealso cref="KeyCode"/>
/// <seealso cref="KeyboardEventExtensions"/>
/// <seealso cref="Modifiers"/>
public enum Key {
    // Modifier keys

    /// <summary>The Alt (Option on macOS) modifier key.</summary>
    [StringValue("Alt")] Alt,

    /// <summary>The AltGraph (Alt Gr) modifier key, used on some international keyboards.</summary>
    [StringValue("AltGraph")] AltGraph,

    /// <summary>The Caps Lock key.</summary>
    [StringValue("CapsLock")] CapsLock,

    /// <summary>The Control (Ctrl) modifier key.</summary>
    [StringValue("Control")] Control,

    /// <summary>The Fn (Function) modifier key, typically on laptop keyboards.</summary>
    [StringValue("Fn")] Fn,

    /// <summary>The Fn Lock key, which toggles the Fn key state.</summary>
    [StringValue("FnLock")] FnLock,

    /// <summary>The Meta key (Windows key on PC, Command key on macOS).</summary>
    [StringValue("Meta")] Meta,

    /// <summary>The Num Lock key, which toggles the numeric keypad between number and navigation modes.</summary>
    [StringValue("NumLock")] NumLock,

    /// <summary>The Scroll Lock key.</summary>
    [StringValue("ScrollLock")] ScrollLock,

    /// <summary>The Shift modifier key.</summary>
    [StringValue("Shift")] Shift,

    /// <summary>The Symbol modifier key (found on some mobile and specialized keyboards).</summary>
    [StringValue("Symbol")] Symbol,

    /// <summary>The Symbol Lock key.</summary>
    [StringValue("SymbolLock")] SymbolLock,

    // Whitespace

    /// <summary>The Enter (Return) key.</summary>
    [StringValue("Enter")] Enter,

    /// <summary>The Tab key.</summary>
    [StringValue("Tab")] Tab,

    /// <summary>The Space bar. The string value is a literal space character (<c>" "</c>).</summary>
    [StringValue(" ")] Space,

    // Navigation

    /// <summary>The Down Arrow navigation key.</summary>
    [StringValue("ArrowDown")] ArrowDown,

    /// <summary>The Left Arrow navigation key.</summary>
    [StringValue("ArrowLeft")] ArrowLeft,

    /// <summary>The Right Arrow navigation key.</summary>
    [StringValue("ArrowRight")] ArrowRight,

    /// <summary>The Up Arrow navigation key.</summary>
    [StringValue("ArrowUp")] ArrowUp,

    /// <summary>The End key, which moves to the end of content.</summary>
    [StringValue("End")] End,

    /// <summary>The Home key, which moves to the beginning of content.</summary>
    [StringValue("Home")] Home,

    /// <summary>The Page Down key.</summary>
    [StringValue("PageDown")] PageDown,

    /// <summary>The Page Up key.</summary>
    [StringValue("PageUp")] PageUp,

    // Editing

    /// <summary>The Backspace key, which deletes the character before the cursor.</summary>
    [StringValue("Backspace")] Backspace,

    /// <summary>The Clear key.</summary>
    [StringValue("Clear")] Clear,

    /// <summary>The Copy key (or Ctrl+C equivalent).</summary>
    [StringValue("Copy")] Copy,

    /// <summary>The Cut key (or Ctrl+X equivalent).</summary>
    [StringValue("Cut")] Cut,

    /// <summary>The Delete key, which deletes the character after the cursor.</summary>
    [StringValue("Delete")] Delete,

    /// <summary>The Insert key, which toggles between insert and overwrite modes.</summary>
    [StringValue("Insert")] Insert,

    /// <summary>The Paste key (or Ctrl+V equivalent).</summary>
    [StringValue("Paste")] Paste,

    /// <summary>The Redo key (or Ctrl+Y equivalent).</summary>
    [StringValue("Redo")] Redo,

    /// <summary>The Undo key (or Ctrl+Z equivalent).</summary>
    [StringValue("Undo")] Undo,

    // UI

    /// <summary>The Escape key.</summary>
    [StringValue("Escape")] Escape,

    /// <summary>The Context Menu key (typically opens a right-click menu).</summary>
    [StringValue("ContextMenu")] ContextMenu,

    /// <summary>The Help key.</summary>
    [StringValue("Help")] Help,

    /// <summary>The Pause key.</summary>
    [StringValue("Pause")] Pause,

    /// <summary>The Print Screen key.</summary>
    [StringValue("PrintScreen")] PrintScreen,

    // Function keys

    /// <summary>The F1 function key.</summary>
    [StringValue("F1")] F1,

    /// <summary>The F2 function key.</summary>
    [StringValue("F2")] F2,

    /// <summary>The F3 function key.</summary>
    [StringValue("F3")] F3,

    /// <summary>The F4 function key.</summary>
    [StringValue("F4")] F4,

    /// <summary>The F5 function key.</summary>
    [StringValue("F5")] F5,

    /// <summary>The F6 function key.</summary>
    [StringValue("F6")] F6,

    /// <summary>The F7 function key.</summary>
    [StringValue("F7")] F7,

    /// <summary>The F8 function key.</summary>
    [StringValue("F8")] F8,

    /// <summary>The F9 function key.</summary>
    [StringValue("F9")] F9,

    /// <summary>The F10 function key.</summary>
    [StringValue("F10")] F10,

    /// <summary>The F11 function key.</summary>
    [StringValue("F11")] F11,

    /// <summary>The F12 function key.</summary>
    [StringValue("F12")] F12,

    // Letters

    /// <summary>The lowercase letter <c>a</c>.</summary>
    [StringValue("a")] A,

    /// <summary>The lowercase letter <c>b</c>.</summary>
    [StringValue("b")] B,

    /// <summary>The lowercase letter <c>c</c>.</summary>
    [StringValue("c")] C,

    /// <summary>The lowercase letter <c>d</c>.</summary>
    [StringValue("d")] D,

    /// <summary>The lowercase letter <c>e</c>.</summary>
    [StringValue("e")] E,

    /// <summary>The lowercase letter <c>f</c>.</summary>
    [StringValue("f")] F,

    /// <summary>The lowercase letter <c>g</c>.</summary>
    [StringValue("g")] G,

    /// <summary>The lowercase letter <c>h</c>.</summary>
    [StringValue("h")] H,

    /// <summary>The lowercase letter <c>i</c>.</summary>
    [StringValue("i")] I,

    /// <summary>The lowercase letter <c>j</c>.</summary>
    [StringValue("j")] J,

    /// <summary>The lowercase letter <c>k</c>.</summary>
    [StringValue("k")] K,

    /// <summary>The lowercase letter <c>l</c>.</summary>
    [StringValue("l")] L,

    /// <summary>The lowercase letter <c>m</c>.</summary>
    [StringValue("m")] M,

    /// <summary>The lowercase letter <c>n</c>.</summary>
    [StringValue("n")] N,

    /// <summary>The lowercase letter <c>o</c>.</summary>
    [StringValue("o")] O,

    /// <summary>The lowercase letter <c>p</c>.</summary>
    [StringValue("p")] P,

    /// <summary>The lowercase letter <c>q</c>.</summary>
    [StringValue("q")] Q,

    /// <summary>The lowercase letter <c>r</c>.</summary>
    [StringValue("r")] R,

    /// <summary>The lowercase letter <c>s</c>.</summary>
    [StringValue("s")] S,

    /// <summary>The lowercase letter <c>t</c>.</summary>
    [StringValue("t")] T,

    /// <summary>The lowercase letter <c>u</c>.</summary>
    [StringValue("u")] U,

    /// <summary>The lowercase letter <c>v</c>.</summary>
    [StringValue("v")] V,

    /// <summary>The lowercase letter <c>w</c>.</summary>
    [StringValue("w")] W,

    /// <summary>The lowercase letter <c>x</c>.</summary>
    [StringValue("x")] X,

    /// <summary>The lowercase letter <c>y</c>.</summary>
    [StringValue("y")] Y,

    /// <summary>The lowercase letter <c>z</c>.</summary>
    [StringValue("z")] Z,

    // Digits

    /// <summary>The digit <c>0</c>.</summary>
    [StringValue("0")] D0,

    /// <summary>The digit <c>1</c>.</summary>
    [StringValue("1")] D1,

    /// <summary>The digit <c>2</c>.</summary>
    [StringValue("2")] D2,

    /// <summary>The digit <c>3</c>.</summary>
    [StringValue("3")] D3,

    /// <summary>The digit <c>4</c>.</summary>
    [StringValue("4")] D4,

    /// <summary>The digit <c>5</c>.</summary>
    [StringValue("5")] D5,

    /// <summary>The digit <c>6</c>.</summary>
    [StringValue("6")] D6,

    /// <summary>The digit <c>7</c>.</summary>
    [StringValue("7")] D7,

    /// <summary>The digit <c>8</c>.</summary>
    [StringValue("8")] D8,

    /// <summary>The digit <c>9</c>.</summary>
    [StringValue("9")] D9,
}
