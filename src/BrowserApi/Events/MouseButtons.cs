namespace BrowserApi.Events;

/// <summary>
/// Represents the set of mouse buttons currently pressed during a mouse event, corresponding
/// to the <c>MouseEvent.buttons</c> property value.
/// </summary>
/// <remarks>
/// <para>
/// This is a flags enum that supports bitwise combination, allowing you to test for
/// multiple simultaneously pressed buttons. Unlike <see cref="MouseButton"/> (which identifies
/// the single button that triggered the event), <see cref="MouseButtons"/> represents all
/// buttons that are currently held down.
/// </para>
/// <para>
/// Use <see cref="MouseEventExtensions.GetButtons"/> to convert a <see cref="MouseEvent"/>'s
/// raw <c>buttons</c> value to this strongly-typed flags enum, and
/// <see cref="MouseEventExtensions.HasButton"/> to check if a specific button is held.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Check if the left button is currently held during a mousemove event
/// if (mouseEvent.HasButton(MouseButtons.Left))
/// {
///     // User is dragging with the left button
/// }
///
/// // Check for multiple buttons
/// MouseButtons buttons = mouseEvent.GetButtons();
/// if ((buttons &amp; MouseButtons.Left) != 0 &amp;&amp; (buttons &amp; MouseButtons.Right) != 0)
/// {
///     // Both left and right buttons are pressed simultaneously
/// }
/// </code>
/// </example>
/// <seealso cref="MouseButton"/>
/// <seealso cref="MouseEventExtensions"/>
[Flags]
public enum MouseButtons : ushort {
    /// <summary>No buttons are pressed.</summary>
    None = 0,

    /// <summary>The primary (left) mouse button is pressed (bit 0).</summary>
    Left = 1,

    /// <summary>The secondary (right) mouse button is pressed (bit 1).</summary>
    Right = 2,

    /// <summary>The middle (scroll wheel) mouse button is pressed (bit 2).</summary>
    Middle = 4,

    /// <summary>The fourth button (browser "Back") is pressed (bit 3).</summary>
    Back = 8,

    /// <summary>The fifth button (browser "Forward") is pressed (bit 4).</summary>
    Forward = 16,
}
