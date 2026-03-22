namespace BrowserApi.Events;

/// <summary>
/// Provides extension methods for <see cref="MouseEvent"/> that add strongly-typed
/// access to modifier state, button identification, and button-held state.
/// </summary>
/// <remarks>
/// These extensions wrap the raw numeric and boolean properties of the generated
/// <see cref="MouseEvent"/> class, providing type-safe alternatives using the
/// <see cref="Modifiers"/>, <see cref="MouseButton"/>, and <see cref="MouseButtons"/> enums.
/// </remarks>
/// <seealso cref="MouseEvent"/>
/// <seealso cref="Modifiers"/>
/// <seealso cref="MouseButton"/>
/// <seealso cref="MouseButtons"/>
public static class MouseEventExtensions {
    /// <summary>
    /// Gets the active modifier keys for this mouse event as a <see cref="Modifiers"/> flags value.
    /// </summary>
    /// <param name="e">The mouse event to inspect.</param>
    /// <returns>
    /// A <see cref="Modifiers"/> value representing all modifier keys that were held down
    /// when the event fired. Returns <see cref="Modifiers.None"/> if no modifiers were active.
    /// </returns>
    /// <example>
    /// <code>
    /// // Check for Ctrl+Click
    /// if (mouseEvent.GetModifiers() == Modifiers.Ctrl)
    /// {
    ///     // Ctrl was the only modifier held during the click
    /// }
    /// </code>
    /// </example>
    public static Modifiers GetModifiers(this MouseEvent e) {
        var m = Modifiers.None;
        if (e.CtrlKey) m |= Modifiers.Ctrl;
        if (e.ShiftKey) m |= Modifiers.Shift;
        if (e.AltKey) m |= Modifiers.Alt;
        if (e.MetaKey) m |= Modifiers.Meta;
        return m;
    }

    /// <summary>
    /// Checks whether one or more specific modifier keys are active in this mouse event.
    /// </summary>
    /// <param name="e">The mouse event to inspect.</param>
    /// <param name="modifier">
    /// The modifier(s) to check for. When multiple flags are combined, all must be active
    /// for this method to return <see langword="true"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if all specified modifier keys are active; otherwise, <see langword="false"/>.
    /// Note that additional modifiers beyond those specified may also be active.
    /// </returns>
    public static bool HasModifier(this MouseEvent e, Modifiers modifier) =>
        (e.GetModifiers() & modifier) == modifier;

    /// <summary>
    /// Gets the mouse button that triggered this event as a strongly-typed <see cref="MouseButton"/> value.
    /// </summary>
    /// <param name="e">The mouse event to inspect.</param>
    /// <returns>
    /// A <see cref="MouseButton"/> value identifying the button that was pressed or released
    /// to trigger this event.
    /// </returns>
    /// <remarks>
    /// This converts the <c>MouseEvent.button</c> property (a <see cref="short"/>) to the
    /// <see cref="MouseButton"/> enum. For checking which buttons are currently held down
    /// (not just the triggering button), use <see cref="GetButtons"/> instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// if (mouseEvent.GetButton() == MouseButton.Right)
    /// {
    ///     // Right-click detected
    /// }
    /// </code>
    /// </example>
    public static MouseButton GetButton(this MouseEvent e) => (MouseButton)e.Button;

    /// <summary>
    /// Gets all mouse buttons currently pressed as a strongly-typed <see cref="MouseButtons"/> flags value.
    /// </summary>
    /// <param name="e">The mouse event to inspect.</param>
    /// <returns>
    /// A <see cref="MouseButtons"/> flags value representing all buttons that are currently
    /// held down. Returns <see cref="MouseButtons.None"/> if no buttons are pressed.
    /// </returns>
    /// <remarks>
    /// This converts the <c>MouseEvent.buttons</c> property (a <see cref="ushort"/>) to the
    /// <see cref="MouseButtons"/> flags enum. Unlike <see cref="GetButton"/>, this reflects all
    /// currently pressed buttons, not just the one that triggered the event.
    /// </remarks>
    public static MouseButtons GetButtons(this MouseEvent e) => (MouseButtons)e.Buttons;

    /// <summary>
    /// Checks whether a specific mouse button is currently held down during this event.
    /// </summary>
    /// <param name="e">The mouse event to inspect.</param>
    /// <param name="button">The button to check for.</param>
    /// <returns>
    /// <see langword="true"/> if the specified button is currently pressed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <example>
    /// <code>
    /// // Detect dragging with the left button during mousemove
    /// if (mouseEvent.HasButton(MouseButtons.Left))
    /// {
    ///     // User is dragging with the left button held
    /// }
    /// </code>
    /// </example>
    public static bool HasButton(this MouseEvent e, MouseButtons button) =>
        (e.GetButtons() & button) == button;
}
