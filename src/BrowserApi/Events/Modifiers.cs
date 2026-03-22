namespace BrowserApi.Events;

/// <summary>
/// Represents a combination of keyboard modifier keys that may be active during an input event.
/// </summary>
/// <remarks>
/// <para>
/// This is a flags enum that supports bitwise combination, allowing you to test for
/// multiple simultaneous modifier keys. Use <see cref="KeyboardEventExtensions.GetModifiers"/>
/// or <see cref="MouseEventExtensions.GetModifiers"/> to extract the active modifiers
/// from an event.
/// </para>
/// <para>
/// The <see cref="Meta"/> flag corresponds to the Windows key on PC keyboards and the
/// Command key on macOS keyboards.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Check for Ctrl+Shift combination
/// Modifiers mods = keyboardEvent.GetModifiers();
/// if ((mods &amp; (Modifiers.Ctrl | Modifiers.Shift)) == (Modifiers.Ctrl | Modifiers.Shift))
/// {
///     // Both Ctrl and Shift are held
/// }
///
/// // Use HasModifier extension for simpler checks
/// if (keyboardEvent.HasModifier(Modifiers.Ctrl))
/// {
///     // Ctrl is held (possibly with other modifiers)
/// }
/// </code>
/// </example>
/// <seealso cref="KeyboardEventExtensions"/>
/// <seealso cref="MouseEventExtensions"/>
[Flags]
public enum Modifiers {
    /// <summary>No modifier keys are pressed.</summary>
    None = 0,

    /// <summary>The Control (Ctrl) modifier key is pressed.</summary>
    Ctrl = 1,

    /// <summary>The Shift modifier key is pressed.</summary>
    Shift = 2,

    /// <summary>The Alt (Option on macOS) modifier key is pressed.</summary>
    Alt = 4,

    /// <summary>The Meta modifier key is pressed (Windows key on PC, Command key on macOS).</summary>
    Meta = 8,
}
