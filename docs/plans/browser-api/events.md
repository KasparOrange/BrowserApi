# Events

**Parent:** [browser-api.md](browser-api.md)

## Purpose

Typed event classes for all DOM events. Generated from WebIDL event interfaces (`uievents.idl`, `pointerevents.idl`, `touch-events.idl`, etc.).

## Use Cases

- **Strongly typed event handlers** — `OnClick` takes `MouseEvent`, `OnKeyDown` takes `KeyboardEvent`
- **Pattern matching on events** — switch on key + modifiers, pointer type, etc.
- **No casting** — event properties are typed (e.g., `PointerEvent.Pressure` is `float`, not `object`)

## Key Event Types

| WebIDL | C# Class | Notable Properties |
|--------|----------|-------------------|
| `Event` | `Event` | `Type`, `Target`, `PreventDefault()`, `StopPropagation()` |
| `UIEvent` | `UiEvent` | `View`, `Detail` |
| `MouseEvent` | `MouseEvent` | `ClientX`, `ClientY`, `Button`, `Buttons` |
| `PointerEvent` | `PointerEvent` | `PointerId`, `Pressure`, `PointerType`, `Width`, `Height` |
| `KeyboardEvent` | `KeyboardEvent` | `Key`, `Code`, `Modifiers` (flags enum) |
| `WheelEvent` | `WheelEvent` | `DeltaX`, `DeltaY`, `DeltaMode` |
| `TouchEvent` | `TouchEvent` | `Touches`, `ChangedTouches` |
| `FocusEvent` | `FocusEvent` | `RelatedTarget` |
| `InputEvent` | `InputEvent` | `Data`, `InputType`, `IsComposing` |
| `DragEvent` | `DragEvent` | `DataTransfer` |
| `AnimationEvent` | `AnimationEvent` | `AnimationName`, `ElapsedTime` |
| `TransitionEvent` | `TransitionEvent` | `PropertyName`, `ElapsedTime` |

## Ergonomic Additions (hand-written)

- `Key` enum — all key values as an enum instead of magic strings
- `Modifiers` flags enum — `Ctrl | Shift | Alt | Meta`
- `MouseButtons` flags enum — `Left | Right | Middle`
- `PointerType` enum — `Mouse | Pen | Touch`

## Event Registration

On Element (generated):
```csharp
public event Action<PointerEvent>? OnPointerDown {
    add => AddEventListener("pointerdown", value);
    remove => RemoveEventListener("pointerdown", value);
}
```

The mapping from event name to event type comes from the specs.
