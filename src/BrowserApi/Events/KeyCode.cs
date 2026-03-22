using BrowserApi.Common;

namespace BrowserApi.Events;

/// <summary>
/// Represents the physical key codes from the
/// <see href="https://www.w3.org/TR/uievents-code/">UI Events KeyboardEvent code Values</see> specification.
/// </summary>
/// <remarks>
/// <para>
/// Each member maps to the <c>KeyboardEvent.code</c> property value via a
/// <see cref="StringValueAttribute"/>. The <c>code</c> property identifies the physical key
/// on the keyboard, regardless of the current keyboard layout or modifier state. For example,
/// <see cref="KeyA"/> always refers to the same physical key position, even if the user's
/// keyboard layout maps that position to a different character.
/// </para>
/// <para>
/// Use <see cref="KeyboardEventExtensions.IsCode"/> to compare a <see cref="KeyboardEvent"/>
/// against a <see cref="KeyCode"/> value. For logical key identification (layout-dependent),
/// use <see cref="Key"/> instead.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Check if the physical "W" key was pressed (for WASD controls, works on any layout)
/// if (keyboardEvent.IsCode(KeyCode.KeyW)) { /* move forward */ }
///
/// // Get the string value
/// string code = KeyCode.ArrowUp.ToStringValue(); // "ArrowUp"
/// </code>
/// </example>
/// <seealso cref="Key"/>
/// <seealso cref="KeyboardEventExtensions"/>
/// <seealso cref="Modifiers"/>
public enum KeyCode {
    // Letters

    /// <summary>The physical key at the <c>A</c> position on a QWERTY layout.</summary>
    [StringValue("KeyA")] KeyA,

    /// <summary>The physical key at the <c>B</c> position on a QWERTY layout.</summary>
    [StringValue("KeyB")] KeyB,

    /// <summary>The physical key at the <c>C</c> position on a QWERTY layout.</summary>
    [StringValue("KeyC")] KeyC,

    /// <summary>The physical key at the <c>D</c> position on a QWERTY layout.</summary>
    [StringValue("KeyD")] KeyD,

    /// <summary>The physical key at the <c>E</c> position on a QWERTY layout.</summary>
    [StringValue("KeyE")] KeyE,

    /// <summary>The physical key at the <c>F</c> position on a QWERTY layout.</summary>
    [StringValue("KeyF")] KeyF,

    /// <summary>The physical key at the <c>G</c> position on a QWERTY layout.</summary>
    [StringValue("KeyG")] KeyG,

    /// <summary>The physical key at the <c>H</c> position on a QWERTY layout.</summary>
    [StringValue("KeyH")] KeyH,

    /// <summary>The physical key at the <c>I</c> position on a QWERTY layout.</summary>
    [StringValue("KeyI")] KeyI,

    /// <summary>The physical key at the <c>J</c> position on a QWERTY layout.</summary>
    [StringValue("KeyJ")] KeyJ,

    /// <summary>The physical key at the <c>K</c> position on a QWERTY layout.</summary>
    [StringValue("KeyK")] KeyK,

    /// <summary>The physical key at the <c>L</c> position on a QWERTY layout.</summary>
    [StringValue("KeyL")] KeyL,

    /// <summary>The physical key at the <c>M</c> position on a QWERTY layout.</summary>
    [StringValue("KeyM")] KeyM,

    /// <summary>The physical key at the <c>N</c> position on a QWERTY layout.</summary>
    [StringValue("KeyN")] KeyN,

    /// <summary>The physical key at the <c>O</c> position on a QWERTY layout.</summary>
    [StringValue("KeyO")] KeyO,

    /// <summary>The physical key at the <c>P</c> position on a QWERTY layout.</summary>
    [StringValue("KeyP")] KeyP,

    /// <summary>The physical key at the <c>Q</c> position on a QWERTY layout.</summary>
    [StringValue("KeyQ")] KeyQ,

    /// <summary>The physical key at the <c>R</c> position on a QWERTY layout.</summary>
    [StringValue("KeyR")] KeyR,

    /// <summary>The physical key at the <c>S</c> position on a QWERTY layout.</summary>
    [StringValue("KeyS")] KeyS,

    /// <summary>The physical key at the <c>T</c> position on a QWERTY layout.</summary>
    [StringValue("KeyT")] KeyT,

    /// <summary>The physical key at the <c>U</c> position on a QWERTY layout.</summary>
    [StringValue("KeyU")] KeyU,

    /// <summary>The physical key at the <c>V</c> position on a QWERTY layout.</summary>
    [StringValue("KeyV")] KeyV,

    /// <summary>The physical key at the <c>W</c> position on a QWERTY layout.</summary>
    [StringValue("KeyW")] KeyW,

    /// <summary>The physical key at the <c>X</c> position on a QWERTY layout.</summary>
    [StringValue("KeyX")] KeyX,

    /// <summary>The physical key at the <c>Y</c> position on a QWERTY layout.</summary>
    [StringValue("KeyY")] KeyY,

    /// <summary>The physical key at the <c>Z</c> position on a QWERTY layout.</summary>
    [StringValue("KeyZ")] KeyZ,

    // Digits

    /// <summary>The physical <c>0</c> key on the main keyboard (above the letter row).</summary>
    [StringValue("Digit0")] Digit0,

    /// <summary>The physical <c>1</c> key on the main keyboard.</summary>
    [StringValue("Digit1")] Digit1,

    /// <summary>The physical <c>2</c> key on the main keyboard.</summary>
    [StringValue("Digit2")] Digit2,

    /// <summary>The physical <c>3</c> key on the main keyboard.</summary>
    [StringValue("Digit3")] Digit3,

    /// <summary>The physical <c>4</c> key on the main keyboard.</summary>
    [StringValue("Digit4")] Digit4,

    /// <summary>The physical <c>5</c> key on the main keyboard.</summary>
    [StringValue("Digit5")] Digit5,

    /// <summary>The physical <c>6</c> key on the main keyboard.</summary>
    [StringValue("Digit6")] Digit6,

    /// <summary>The physical <c>7</c> key on the main keyboard.</summary>
    [StringValue("Digit7")] Digit7,

    /// <summary>The physical <c>8</c> key on the main keyboard.</summary>
    [StringValue("Digit8")] Digit8,

    /// <summary>The physical <c>9</c> key on the main keyboard.</summary>
    [StringValue("Digit9")] Digit9,

    // Navigation

    /// <summary>The physical Up Arrow key.</summary>
    [StringValue("ArrowUp")] ArrowUp,

    /// <summary>The physical Down Arrow key.</summary>
    [StringValue("ArrowDown")] ArrowDown,

    /// <summary>The physical Left Arrow key.</summary>
    [StringValue("ArrowLeft")] ArrowLeft,

    /// <summary>The physical Right Arrow key.</summary>
    [StringValue("ArrowRight")] ArrowRight,

    // Editing

    /// <summary>The physical Enter (Return) key.</summary>
    [StringValue("Enter")] Enter,

    /// <summary>The physical Space bar.</summary>
    [StringValue("Space")] Space,

    /// <summary>The physical Backspace key.</summary>
    [StringValue("Backspace")] Backspace,

    /// <summary>The physical Tab key.</summary>
    [StringValue("Tab")] Tab,

    /// <summary>The physical Escape key.</summary>
    [StringValue("Escape")] Escape,

    /// <summary>The physical Delete key.</summary>
    [StringValue("Delete")] Delete,

    /// <summary>The physical Insert key.</summary>
    [StringValue("Insert")] Insert,

    /// <summary>The physical Home key.</summary>
    [StringValue("Home")] Home,

    /// <summary>The physical End key.</summary>
    [StringValue("End")] End,

    /// <summary>The physical Page Up key.</summary>
    [StringValue("PageUp")] PageUp,

    /// <summary>The physical Page Down key.</summary>
    [StringValue("PageDown")] PageDown,

    // Modifiers

    /// <summary>The physical left Shift key.</summary>
    [StringValue("ShiftLeft")] ShiftLeft,

    /// <summary>The physical right Shift key.</summary>
    [StringValue("ShiftRight")] ShiftRight,

    /// <summary>The physical left Control key.</summary>
    [StringValue("ControlLeft")] ControlLeft,

    /// <summary>The physical right Control key.</summary>
    [StringValue("ControlRight")] ControlRight,

    /// <summary>The physical left Alt (Option on macOS) key.</summary>
    [StringValue("AltLeft")] AltLeft,

    /// <summary>The physical right Alt (Alt Gr on some layouts) key.</summary>
    [StringValue("AltRight")] AltRight,

    /// <summary>The physical left Meta (Windows/Command) key.</summary>
    [StringValue("MetaLeft")] MetaLeft,

    /// <summary>The physical right Meta (Windows/Command) key.</summary>
    [StringValue("MetaRight")] MetaRight,

    /// <summary>The physical Caps Lock key.</summary>
    [StringValue("CapsLock")] CapsLock,

    // Function keys

    /// <summary>The physical F1 function key.</summary>
    [StringValue("F1")] F1,

    /// <summary>The physical F2 function key.</summary>
    [StringValue("F2")] F2,

    /// <summary>The physical F3 function key.</summary>
    [StringValue("F3")] F3,

    /// <summary>The physical F4 function key.</summary>
    [StringValue("F4")] F4,

    /// <summary>The physical F5 function key.</summary>
    [StringValue("F5")] F5,

    /// <summary>The physical F6 function key.</summary>
    [StringValue("F6")] F6,

    /// <summary>The physical F7 function key.</summary>
    [StringValue("F7")] F7,

    /// <summary>The physical F8 function key.</summary>
    [StringValue("F8")] F8,

    /// <summary>The physical F9 function key.</summary>
    [StringValue("F9")] F9,

    /// <summary>The physical F10 function key.</summary>
    [StringValue("F10")] F10,

    /// <summary>The physical F11 function key.</summary>
    [StringValue("F11")] F11,

    /// <summary>The physical F12 function key.</summary>
    [StringValue("F12")] F12,
}
