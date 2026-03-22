using BrowserApi.Common;
using BrowserApi.Events;

namespace BrowserApi.Dom;

/// <summary>
/// Provides strongly-typed event subscription extension methods for <see cref="EventTarget"/>.
/// </summary>
/// <remarks>
/// <para>
/// Each method attaches an event listener to the target and returns an <see cref="EventSubscription"/>
/// that can be disposed to remove the listener. The generic <see cref="On{TEvent}"/> method
/// supports any event type; the convenience methods (<see cref="OnClick"/>, <see cref="OnKeyDown"/>,
/// etc.) provide a shorthand for the most common DOM events.
/// </para>
/// <para>
/// All handlers receive a fully-typed event object (e.g., <see cref="MouseEvent"/>,
/// <see cref="KeyboardEvent"/>, <see cref="PointerEvent"/>), giving access to event-specific
/// properties such as <c>ClientX</c>, <c>Key</c>, or <c>PointerId</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Typed click handler with automatic event deserialization
/// using var sub = button.OnClick(e => {
///     Console.WriteLine($"Clicked at ({e.ClientX}, {e.ClientY})");
/// });
///
/// // Generic event handler for custom event types
/// using var custom = element.On&lt;CustomEvent&gt;("my-custom-event", e => {
///     Console.WriteLine(e.Detail);
/// });
/// </code>
/// </example>
/// <seealso cref="EventSubscription"/>
/// <seealso cref="EventTarget"/>
public static class EventExtensions {
    /// <summary>
    /// Subscribes a strongly-typed event handler to the specified event on the target.
    /// </summary>
    /// <typeparam name="TEvent">
    /// The event type to deserialize from the DOM event (e.g., <see cref="MouseEvent"/>,
    /// <see cref="KeyboardEvent"/>). Must derive from <see cref="Event"/> and have a
    /// parameterless constructor.
    /// </typeparam>
    /// <param name="target">The DOM event target to listen on.</param>
    /// <param name="eventName">
    /// The DOM event name (e.g., <c>"click"</c>, <c>"keydown"</c>, <c>"pointerenter"</c>).
    /// </param>
    /// <param name="handler">
    /// The callback invoked when the event fires. Receives a fully-typed <typeparamref name="TEvent"/> instance.
    /// </param>
    /// <returns>
    /// An <see cref="EventSubscription"/> that removes the listener when disposed.
    /// </returns>
    public static EventSubscription On<TEvent>(this EventTarget target, string eventName, System.Action<TEvent> handler)
        where TEvent : Event, new() {
        var listenerId = JsObject.Backend.AddEventListener(target.Handle, eventName, eventHandle => {
            var evt = new TEvent { Handle = eventHandle };
            handler(evt);
        });
        return new EventSubscription(target.Handle, eventName, listenerId);
    }

    // ── Mouse events ────────────────────────────────────────────────────

    /// <summary>
    /// Subscribes to the <c>click</c> event with a <see cref="MouseEvent"/> handler.
    /// </summary>
    /// <param name="target">The event target to listen on.</param>
    /// <param name="handler">The callback invoked on each click.</param>
    /// <returns>An <see cref="EventSubscription"/> that removes the listener when disposed.</returns>
    public static EventSubscription OnClick(this EventTarget target, System.Action<MouseEvent> handler) =>
        target.On("click", handler);

    /// <summary>
    /// Subscribes to the <c>dblclick</c> (double-click) event with a <see cref="MouseEvent"/> handler.
    /// </summary>
    /// <param name="target">The event target to listen on.</param>
    /// <param name="handler">The callback invoked on each double-click.</param>
    /// <returns>An <see cref="EventSubscription"/> that removes the listener when disposed.</returns>
    public static EventSubscription OnDblClick(this EventTarget target, System.Action<MouseEvent> handler) =>
        target.On("dblclick", handler);

    /// <summary>
    /// Subscribes to the <c>mousedown</c> event with a <see cref="MouseEvent"/> handler.
    /// </summary>
    /// <param name="target">The event target to listen on.</param>
    /// <param name="handler">The callback invoked when a mouse button is pressed.</param>
    /// <returns>An <see cref="EventSubscription"/> that removes the listener when disposed.</returns>
    public static EventSubscription OnMouseDown(this EventTarget target, System.Action<MouseEvent> handler) =>
        target.On("mousedown", handler);

    /// <summary>
    /// Subscribes to the <c>mouseup</c> event with a <see cref="MouseEvent"/> handler.
    /// </summary>
    /// <param name="target">The event target to listen on.</param>
    /// <param name="handler">The callback invoked when a mouse button is released.</param>
    /// <returns>An <see cref="EventSubscription"/> that removes the listener when disposed.</returns>
    public static EventSubscription OnMouseUp(this EventTarget target, System.Action<MouseEvent> handler) =>
        target.On("mouseup", handler);

    /// <summary>
    /// Subscribes to the <c>mousemove</c> event with a <see cref="MouseEvent"/> handler.
    /// </summary>
    /// <param name="target">The event target to listen on.</param>
    /// <param name="handler">The callback invoked when the mouse pointer moves over the target.</param>
    /// <returns>An <see cref="EventSubscription"/> that removes the listener when disposed.</returns>
    public static EventSubscription OnMouseMove(this EventTarget target, System.Action<MouseEvent> handler) =>
        target.On("mousemove", handler);

    /// <summary>
    /// Subscribes to the <c>mouseenter</c> event with a <see cref="MouseEvent"/> handler.
    /// </summary>
    /// <remarks>
    /// Unlike <c>mouseover</c>, the <c>mouseenter</c> event does not bubble and is only fired
    /// when the pointer enters the target element itself (not its descendants).
    /// </remarks>
    /// <param name="target">The event target to listen on.</param>
    /// <param name="handler">The callback invoked when the mouse enters the target.</param>
    /// <returns>An <see cref="EventSubscription"/> that removes the listener when disposed.</returns>
    public static EventSubscription OnMouseEnter(this EventTarget target, System.Action<MouseEvent> handler) =>
        target.On("mouseenter", handler);

    /// <summary>
    /// Subscribes to the <c>mouseleave</c> event with a <see cref="MouseEvent"/> handler.
    /// </summary>
    /// <remarks>
    /// Unlike <c>mouseout</c>, the <c>mouseleave</c> event does not bubble and is only fired
    /// when the pointer leaves the target element itself (not its descendants).
    /// </remarks>
    /// <param name="target">The event target to listen on.</param>
    /// <param name="handler">The callback invoked when the mouse leaves the target.</param>
    /// <returns>An <see cref="EventSubscription"/> that removes the listener when disposed.</returns>
    public static EventSubscription OnMouseLeave(this EventTarget target, System.Action<MouseEvent> handler) =>
        target.On("mouseleave", handler);

    // ── Keyboard events ─────────────────────────────────────────────────

    /// <summary>
    /// Subscribes to the <c>keydown</c> event with a <see cref="KeyboardEvent"/> handler.
    /// </summary>
    /// <param name="target">The event target to listen on.</param>
    /// <param name="handler">The callback invoked when a key is pressed down.</param>
    /// <returns>An <see cref="EventSubscription"/> that removes the listener when disposed.</returns>
    public static EventSubscription OnKeyDown(this EventTarget target, System.Action<KeyboardEvent> handler) =>
        target.On("keydown", handler);

    /// <summary>
    /// Subscribes to the <c>keyup</c> event with a <see cref="KeyboardEvent"/> handler.
    /// </summary>
    /// <param name="target">The event target to listen on.</param>
    /// <param name="handler">The callback invoked when a key is released.</param>
    /// <returns>An <see cref="EventSubscription"/> that removes the listener when disposed.</returns>
    public static EventSubscription OnKeyUp(this EventTarget target, System.Action<KeyboardEvent> handler) =>
        target.On("keyup", handler);

    // ── Pointer events ──────────────────────────────────────────────────

    /// <summary>
    /// Subscribes to the <c>pointerdown</c> event with a <see cref="PointerEvent"/> handler.
    /// </summary>
    /// <remarks>
    /// Pointer events unify mouse, touch, and pen input into a single event model.
    /// </remarks>
    /// <param name="target">The event target to listen on.</param>
    /// <param name="handler">The callback invoked when a pointer is pressed.</param>
    /// <returns>An <see cref="EventSubscription"/> that removes the listener when disposed.</returns>
    public static EventSubscription OnPointerDown(this EventTarget target, System.Action<PointerEvent> handler) =>
        target.On("pointerdown", handler);

    /// <summary>
    /// Subscribes to the <c>pointerup</c> event with a <see cref="PointerEvent"/> handler.
    /// </summary>
    /// <param name="target">The event target to listen on.</param>
    /// <param name="handler">The callback invoked when a pointer is released.</param>
    /// <returns>An <see cref="EventSubscription"/> that removes the listener when disposed.</returns>
    public static EventSubscription OnPointerUp(this EventTarget target, System.Action<PointerEvent> handler) =>
        target.On("pointerup", handler);

    /// <summary>
    /// Subscribes to the <c>pointermove</c> event with a <see cref="PointerEvent"/> handler.
    /// </summary>
    /// <param name="target">The event target to listen on.</param>
    /// <param name="handler">The callback invoked when a pointer moves.</param>
    /// <returns>An <see cref="EventSubscription"/> that removes the listener when disposed.</returns>
    public static EventSubscription OnPointerMove(this EventTarget target, System.Action<PointerEvent> handler) =>
        target.On("pointermove", handler);

    /// <summary>
    /// Subscribes to the <c>pointerenter</c> event with a <see cref="PointerEvent"/> handler.
    /// </summary>
    /// <param name="target">The event target to listen on.</param>
    /// <param name="handler">The callback invoked when a pointer enters the target.</param>
    /// <returns>An <see cref="EventSubscription"/> that removes the listener when disposed.</returns>
    public static EventSubscription OnPointerEnter(this EventTarget target, System.Action<PointerEvent> handler) =>
        target.On("pointerenter", handler);

    /// <summary>
    /// Subscribes to the <c>pointerleave</c> event with a <see cref="PointerEvent"/> handler.
    /// </summary>
    /// <param name="target">The event target to listen on.</param>
    /// <param name="handler">The callback invoked when a pointer leaves the target.</param>
    /// <returns>An <see cref="EventSubscription"/> that removes the listener when disposed.</returns>
    public static EventSubscription OnPointerLeave(this EventTarget target, System.Action<PointerEvent> handler) =>
        target.On("pointerleave", handler);

    // ── Focus events ────────────────────────────────────────────────────

    /// <summary>
    /// Subscribes to the <c>focus</c> event with a base <see cref="Event"/> handler.
    /// </summary>
    /// <param name="target">The event target to listen on.</param>
    /// <param name="handler">The callback invoked when the target receives focus.</param>
    /// <returns>An <see cref="EventSubscription"/> that removes the listener when disposed.</returns>
    public static EventSubscription OnFocus(this EventTarget target, System.Action<Event> handler) =>
        target.On("focus", handler);

    /// <summary>
    /// Subscribes to the <c>blur</c> event with a base <see cref="Event"/> handler.
    /// </summary>
    /// <param name="target">The event target to listen on.</param>
    /// <param name="handler">The callback invoked when the target loses focus.</param>
    /// <returns>An <see cref="EventSubscription"/> that removes the listener when disposed.</returns>
    public static EventSubscription OnBlur(this EventTarget target, System.Action<Event> handler) =>
        target.On("blur", handler);

    // ── Form events ─────────────────────────────────────────────────────

    /// <summary>
    /// Subscribes to the <c>input</c> event with a base <see cref="Event"/> handler.
    /// </summary>
    /// <remarks>
    /// The <c>input</c> event fires synchronously when the value of an <c>&lt;input&gt;</c>,
    /// <c>&lt;select&gt;</c>, or <c>&lt;textarea&gt;</c> element changes.
    /// </remarks>
    /// <param name="target">The event target to listen on.</param>
    /// <param name="handler">The callback invoked on each input change.</param>
    /// <returns>An <see cref="EventSubscription"/> that removes the listener when disposed.</returns>
    public static EventSubscription OnInput(this EventTarget target, System.Action<Event> handler) =>
        target.On("input", handler);

    /// <summary>
    /// Subscribes to the <c>change</c> event with a base <see cref="Event"/> handler.
    /// </summary>
    /// <remarks>
    /// The <c>change</c> event fires when the element loses focus after its value was modified.
    /// Unlike <see cref="OnInput"/>, it fires only once per committed change.
    /// </remarks>
    /// <param name="target">The event target to listen on.</param>
    /// <param name="handler">The callback invoked when the value is committed.</param>
    /// <returns>An <see cref="EventSubscription"/> that removes the listener when disposed.</returns>
    public static EventSubscription OnChange(this EventTarget target, System.Action<Event> handler) =>
        target.On("change", handler);

    /// <summary>
    /// Subscribes to the <c>submit</c> event with a base <see cref="Event"/> handler.
    /// </summary>
    /// <remarks>
    /// Fires when a <c>&lt;form&gt;</c> is submitted. Call <c>e.PreventDefault()</c> inside
    /// the handler to prevent the default browser navigation.
    /// </remarks>
    /// <param name="target">The event target to listen on (typically a form element).</param>
    /// <param name="handler">The callback invoked on form submission.</param>
    /// <returns>An <see cref="EventSubscription"/> that removes the listener when disposed.</returns>
    public static EventSubscription OnSubmit(this EventTarget target, System.Action<Event> handler) =>
        target.On("submit", handler);
}
