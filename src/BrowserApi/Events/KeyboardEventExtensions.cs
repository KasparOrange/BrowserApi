using BrowserApi.Common;

namespace BrowserApi.Events;

/// <summary>
/// Provides extension methods for <see cref="KeyboardEvent"/> that add strongly-typed
/// access to modifier state, logical key values, and physical key codes.
/// </summary>
/// <remarks>
/// These extensions wrap the raw boolean and string properties of the generated
/// <see cref="KeyboardEvent"/> class, providing type-safe alternatives using the
/// <see cref="Modifiers"/>, <see cref="Key"/>, and <see cref="KeyCode"/> enums.
/// </remarks>
/// <seealso cref="KeyboardEvent"/>
/// <seealso cref="Modifiers"/>
/// <seealso cref="Key"/>
/// <seealso cref="KeyCode"/>
public static class KeyboardEventExtensions {
    /// <summary>
    /// Gets the active modifier keys for this keyboard event as a <see cref="Modifiers"/> flags value.
    /// </summary>
    /// <param name="e">The keyboard event to inspect.</param>
    /// <returns>
    /// A <see cref="Modifiers"/> value representing all modifier keys that were held down
    /// when the event fired. Returns <see cref="Modifiers.None"/> if no modifiers were active.
    /// </returns>
    /// <example>
    /// <code>
    /// Modifiers mods = keyboardEvent.GetModifiers();
    /// if (mods == (Modifiers.Ctrl | Modifiers.Shift))
    /// {
    ///     // Exactly Ctrl+Shift (no other modifiers)
    /// }
    /// </code>
    /// </example>
    public static Modifiers GetModifiers(this KeyboardEvent e) {
        var m = Modifiers.None;
        if (e.CtrlKey) m |= Modifiers.Ctrl;
        if (e.ShiftKey) m |= Modifiers.Shift;
        if (e.AltKey) m |= Modifiers.Alt;
        if (e.MetaKey) m |= Modifiers.Meta;
        return m;
    }

    /// <summary>
    /// Checks whether one or more specific modifier keys are active in this keyboard event.
    /// </summary>
    /// <param name="e">The keyboard event to inspect.</param>
    /// <param name="modifier">
    /// The modifier(s) to check for. When multiple flags are combined, all must be active
    /// for this method to return <see langword="true"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if all specified modifier keys are active; otherwise, <see langword="false"/>.
    /// Note that additional modifiers beyond those specified may also be active.
    /// </returns>
    /// <example>
    /// <code>
    /// if (keyboardEvent.HasModifier(Modifiers.Ctrl))
    /// {
    ///     // Ctrl is held (possibly with other modifiers too)
    /// }
    /// </code>
    /// </example>
    public static bool HasModifier(this KeyboardEvent e, Modifiers modifier) =>
        (e.GetModifiers() & modifier) == modifier;

    /// <summary>
    /// Checks whether this keyboard event's logical key matches the specified <see cref="Key"/> value.
    /// </summary>
    /// <param name="e">The keyboard event to inspect.</param>
    /// <param name="key">The logical key to compare against.</param>
    /// <returns>
    /// <see langword="true"/> if the event's <c>key</c> property matches the
    /// <see cref="StringValueAttribute"/> of the specified <see cref="Key"/> member;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This method compares against the logical key value (<c>KeyboardEvent.key</c>), which
    /// is layout-dependent. For layout-independent physical key comparison, use
    /// <see cref="IsCode"/> instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// if (keyboardEvent.IsKey(Key.Enter))
    /// {
    ///     // The Enter key was pressed
    /// }
    /// </code>
    /// </example>
    public static bool IsKey(this KeyboardEvent e, Key key) =>
        e.Key == key.ToStringValue();

    /// <summary>
    /// Checks whether this keyboard event's physical key code matches the specified <see cref="KeyCode"/> value.
    /// </summary>
    /// <param name="e">The keyboard event to inspect.</param>
    /// <param name="code">The physical key code to compare against.</param>
    /// <returns>
    /// <see langword="true"/> if the event's <c>code</c> property matches the
    /// <see cref="StringValueAttribute"/> of the specified <see cref="KeyCode"/> member;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This method compares against the physical key code (<c>KeyboardEvent.code</c>), which
    /// is layout-independent. This is useful for game-style WASD controls or keyboard shortcuts
    /// that should work regardless of keyboard layout. For logical key comparison, use
    /// <see cref="IsKey"/> instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// // WASD movement (works on QWERTY, AZERTY, Dvorak, etc.)
    /// if (keyboardEvent.IsCode(KeyCode.KeyW)) { /* move forward */ }
    /// if (keyboardEvent.IsCode(KeyCode.KeyA)) { /* move left */ }
    /// if (keyboardEvent.IsCode(KeyCode.KeyS)) { /* move backward */ }
    /// if (keyboardEvent.IsCode(KeyCode.KeyD)) { /* move right */ }
    /// </code>
    /// </example>
    public static bool IsCode(this KeyboardEvent e, KeyCode code) =>
        e.Code == code.ToStringValue();
}
