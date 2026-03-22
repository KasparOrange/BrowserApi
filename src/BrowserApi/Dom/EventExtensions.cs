using BrowserApi.Common;
using BrowserApi.Events;

namespace BrowserApi.Dom;

public static class EventExtensions {
    // Generic typed event handler
    public static EventSubscription On<TEvent>(this EventTarget target, string eventName, System.Action<TEvent> handler)
        where TEvent : Event, new() {
        var listenerId = JsObject.Backend.AddEventListener(target.Handle, eventName, eventHandle => {
            var evt = new TEvent { Handle = eventHandle };
            handler(evt);
        });
        return new EventSubscription(target.Handle, eventName, listenerId);
    }

    // Mouse events
    public static EventSubscription OnClick(this EventTarget target, System.Action<MouseEvent> handler) =>
        target.On("click", handler);

    public static EventSubscription OnDblClick(this EventTarget target, System.Action<MouseEvent> handler) =>
        target.On("dblclick", handler);

    public static EventSubscription OnMouseDown(this EventTarget target, System.Action<MouseEvent> handler) =>
        target.On("mousedown", handler);

    public static EventSubscription OnMouseUp(this EventTarget target, System.Action<MouseEvent> handler) =>
        target.On("mouseup", handler);

    public static EventSubscription OnMouseMove(this EventTarget target, System.Action<MouseEvent> handler) =>
        target.On("mousemove", handler);

    public static EventSubscription OnMouseEnter(this EventTarget target, System.Action<MouseEvent> handler) =>
        target.On("mouseenter", handler);

    public static EventSubscription OnMouseLeave(this EventTarget target, System.Action<MouseEvent> handler) =>
        target.On("mouseleave", handler);

    // Keyboard events
    public static EventSubscription OnKeyDown(this EventTarget target, System.Action<KeyboardEvent> handler) =>
        target.On("keydown", handler);

    public static EventSubscription OnKeyUp(this EventTarget target, System.Action<KeyboardEvent> handler) =>
        target.On("keyup", handler);

    // Pointer events
    public static EventSubscription OnPointerDown(this EventTarget target, System.Action<PointerEvent> handler) =>
        target.On("pointerdown", handler);

    public static EventSubscription OnPointerUp(this EventTarget target, System.Action<PointerEvent> handler) =>
        target.On("pointerup", handler);

    public static EventSubscription OnPointerMove(this EventTarget target, System.Action<PointerEvent> handler) =>
        target.On("pointermove", handler);

    public static EventSubscription OnPointerEnter(this EventTarget target, System.Action<PointerEvent> handler) =>
        target.On("pointerenter", handler);

    public static EventSubscription OnPointerLeave(this EventTarget target, System.Action<PointerEvent> handler) =>
        target.On("pointerleave", handler);

    // Focus events
    public static EventSubscription OnFocus(this EventTarget target, System.Action<Event> handler) =>
        target.On("focus", handler);

    public static EventSubscription OnBlur(this EventTarget target, System.Action<Event> handler) =>
        target.On("blur", handler);

    // Form events
    public static EventSubscription OnInput(this EventTarget target, System.Action<Event> handler) =>
        target.On("input", handler);

    public static EventSubscription OnChange(this EventTarget target, System.Action<Event> handler) =>
        target.On("change", handler);

    public static EventSubscription OnSubmit(this EventTarget target, System.Action<Event> handler) =>
        target.On("submit", handler);
}
