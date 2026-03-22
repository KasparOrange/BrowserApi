namespace BrowserApi.Events;

/// <summary>
/// Identifies which mouse button triggered a mouse event, corresponding to the
/// <c>MouseEvent.button</c> property value.
/// </summary>
/// <remarks>
/// <para>
/// This enum maps to the <c>MouseEvent.button</c> property, which indicates the single
/// button that was pressed or released to trigger the event. For tracking which buttons
/// are currently held down (possibly multiple), use <see cref="MouseButtons"/> instead.
/// </para>
/// <para>
/// Use <see cref="MouseEventExtensions.GetButton"/> to convert a <see cref="MouseEvent"/>'s
/// raw <c>button</c> value to this strongly-typed enum.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// MouseButton button = mouseEvent.GetButton();
/// if (button == MouseButton.Left)
/// {
///     // Primary (left) button was clicked
/// }
/// </code>
/// </example>
/// <seealso cref="MouseButtons"/>
/// <seealso cref="MouseEventExtensions"/>
public enum MouseButton : short {
    /// <summary>The primary (left) mouse button (value 0).</summary>
    Left = 0,

    /// <summary>The middle mouse button, typically the scroll wheel button (value 1).</summary>
    Middle = 1,

    /// <summary>The secondary (right) mouse button, typically used for context menus (value 2).</summary>
    Right = 2,

    /// <summary>The fourth button, typically the browser "Back" navigation button (value 3).</summary>
    Back = 3,

    /// <summary>The fifth button, typically the browser "Forward" navigation button (value 4).</summary>
    Forward = 4,
}
