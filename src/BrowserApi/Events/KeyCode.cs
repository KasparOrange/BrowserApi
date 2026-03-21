using BrowserApi.Common;

namespace BrowserApi.Events;

public enum KeyCode {
    // Letters
    [StringValue("KeyA")] KeyA,
    [StringValue("KeyB")] KeyB,
    [StringValue("KeyC")] KeyC,
    [StringValue("KeyD")] KeyD,
    [StringValue("KeyE")] KeyE,
    [StringValue("KeyF")] KeyF,
    [StringValue("KeyG")] KeyG,
    [StringValue("KeyH")] KeyH,
    [StringValue("KeyI")] KeyI,
    [StringValue("KeyJ")] KeyJ,
    [StringValue("KeyK")] KeyK,
    [StringValue("KeyL")] KeyL,
    [StringValue("KeyM")] KeyM,
    [StringValue("KeyN")] KeyN,
    [StringValue("KeyO")] KeyO,
    [StringValue("KeyP")] KeyP,
    [StringValue("KeyQ")] KeyQ,
    [StringValue("KeyR")] KeyR,
    [StringValue("KeyS")] KeyS,
    [StringValue("KeyT")] KeyT,
    [StringValue("KeyU")] KeyU,
    [StringValue("KeyV")] KeyV,
    [StringValue("KeyW")] KeyW,
    [StringValue("KeyX")] KeyX,
    [StringValue("KeyY")] KeyY,
    [StringValue("KeyZ")] KeyZ,

    // Digits
    [StringValue("Digit0")] Digit0,
    [StringValue("Digit1")] Digit1,
    [StringValue("Digit2")] Digit2,
    [StringValue("Digit3")] Digit3,
    [StringValue("Digit4")] Digit4,
    [StringValue("Digit5")] Digit5,
    [StringValue("Digit6")] Digit6,
    [StringValue("Digit7")] Digit7,
    [StringValue("Digit8")] Digit8,
    [StringValue("Digit9")] Digit9,

    // Navigation
    [StringValue("ArrowUp")] ArrowUp,
    [StringValue("ArrowDown")] ArrowDown,
    [StringValue("ArrowLeft")] ArrowLeft,
    [StringValue("ArrowRight")] ArrowRight,

    // Editing
    [StringValue("Enter")] Enter,
    [StringValue("Space")] Space,
    [StringValue("Backspace")] Backspace,
    [StringValue("Tab")] Tab,
    [StringValue("Escape")] Escape,
    [StringValue("Delete")] Delete,
    [StringValue("Insert")] Insert,
    [StringValue("Home")] Home,
    [StringValue("End")] End,
    [StringValue("PageUp")] PageUp,
    [StringValue("PageDown")] PageDown,

    // Modifiers
    [StringValue("ShiftLeft")] ShiftLeft,
    [StringValue("ShiftRight")] ShiftRight,
    [StringValue("ControlLeft")] ControlLeft,
    [StringValue("ControlRight")] ControlRight,
    [StringValue("AltLeft")] AltLeft,
    [StringValue("AltRight")] AltRight,
    [StringValue("MetaLeft")] MetaLeft,
    [StringValue("MetaRight")] MetaRight,
    [StringValue("CapsLock")] CapsLock,

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
}
