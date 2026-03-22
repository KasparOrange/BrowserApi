# Event Handling Patterns

BrowserApi provides a strongly-typed event system built on top of DOM `addEventListener`/`removeEventListener`. Instead of writing JavaScript callback glue code, you subscribe to events with C# lambdas and receive fully-typed event objects.

## The Core Pattern: On&lt;TEvent&gt;

All event handling flows through a single generic extension method on `EventTarget`:

```csharp
public static EventSubscription On<TEvent>(
    this EventTarget target,
    string eventName,
    Action<TEvent> handler)
    where TEvent : Event, new()
```

This method:
1. Calls `addEventListener` on the target's underlying JavaScript object
2. When the event fires, deserializes the DOM event into a typed `TEvent` instance
3. Invokes your handler with the typed event
4. Returns an `EventSubscription` that removes the listener when disposed

Example with explicit type and event name:

```csharp
using var sub = element.On<MouseEvent>("click", e => {
    Console.WriteLine($"Clicked at ({e.ClientX}, {e.ClientY})");
});
```

## Convenience Methods

For common DOM events, typed convenience methods remove the need to specify the event name and type:

### Mouse Events

```csharp
using var click      = button.OnClick(e => { /* MouseEvent */ });
using var dblClick   = button.OnDblClick(e => { /* MouseEvent */ });
using var mouseDown  = element.OnMouseDown(e => { /* MouseEvent */ });
using var mouseUp    = element.OnMouseUp(e => { /* MouseEvent */ });
using var mouseMove  = element.OnMouseMove(e => { /* MouseEvent */ });
using var mouseEnter = element.OnMouseEnter(e => { /* MouseEvent */ });
using var mouseLeave = element.OnMouseLeave(e => { /* MouseEvent */ });
```

### Keyboard Events

```csharp
using var keyDown = input.OnKeyDown(e => { /* KeyboardEvent */ });
using var keyUp   = input.OnKeyUp(e => { /* KeyboardEvent */ });
```

### Pointer Events

Pointer events unify mouse, touch, and pen input into a single model:

```csharp
using var pointerDown  = canvas.OnPointerDown(e => { /* PointerEvent */ });
using var pointerUp    = canvas.OnPointerUp(e => { /* PointerEvent */ });
using var pointerMove  = canvas.OnPointerMove(e => { /* PointerEvent */ });
using var pointerEnter = canvas.OnPointerEnter(e => { /* PointerEvent */ });
using var pointerLeave = canvas.OnPointerLeave(e => { /* PointerEvent */ });
```

### Focus Events

```csharp
using var focus = input.OnFocus(e => { /* Event */ });
using var blur  = input.OnBlur(e => { /* Event */ });
```

### Form Events

```csharp
using var inputEvent = textbox.OnInput(e => { /* Event */ });
using var change     = select.OnChange(e => { /* Event */ });
using var submit     = form.OnSubmit(e => {
    e.PreventDefault(); // prevent browser navigation
    // handle form submission
});
```

## EventSubscription Lifecycle

Every event subscription method returns an `EventSubscription`, which is a sealed class that implements `IDisposable`:

```csharp
public sealed class EventSubscription : IDisposable {
    public void Dispose(); // calls removeEventListener
}
```

### Creation

An `EventSubscription` is created when you call any `On*` method. At that moment, `addEventListener` is called on the JavaScript side.

### Using Statement (Scoped Lifetime)

The most common pattern ties the subscription to a scope:

```csharp
using var sub = button.OnClick(e => HandleClick(e));
// listener is active here
// ...
// listener is automatically removed when 'sub' goes out of scope
```

### Manual Disposal

For subscriptions that need dynamic lifetime management:

```csharp
EventSubscription? subscription = null;

void StartListening() {
    subscription = canvas.OnPointerMove(e => TrackPointer(e));
}

void StopListening() {
    subscription?.Dispose();  // removes the event listener
    subscription = null;
}
```

### Idempotent Disposal

Calling `Dispose()` multiple times is safe -- subsequent calls are no-ops:

```csharp
var sub = element.OnClick(e => { });
sub.Dispose(); // removes listener
sub.Dispose(); // no-op, no error
```

## Typed Event Properties

### MouseEvent

`MouseEvent` provides coordinates, button state, and modifier keys:

```csharp
button.OnClick(e => {
    // Coordinates
    double clientX = e.ClientX;   // relative to viewport
    double clientY = e.ClientY;
    double pageX   = e.PageX;     // relative to document
    double pageY   = e.PageY;
    double screenX = e.ScreenX;   // relative to screen
    double screenY = e.ScreenY;
    double offsetX = e.OffsetX;   // relative to target element
    double offsetY = e.OffsetY;

    // Button (which button triggered the event)
    short button = e.Button;       // raw value: 0=left, 1=middle, 2=right

    // Buttons (which buttons are currently held)
    ushort buttons = e.Buttons;    // bitmask

    // Modifier keys
    bool ctrl  = e.CtrlKey;
    bool shift = e.ShiftKey;
    bool alt   = e.AltKey;
    bool meta  = e.MetaKey;
});
```

### KeyboardEvent

`KeyboardEvent` provides the key value, physical code, and modifier state:

```csharp
input.OnKeyDown(e => {
    string key  = e.Key;      // logical key: "a", "Enter", "ArrowUp"
    string code = e.Code;     // physical key: "KeyA", "Enter", "ArrowUp"
    bool repeat = e.Repeat;   // true if key is being held down
    bool ctrl   = e.CtrlKey;
    bool shift  = e.ShiftKey;
    bool alt    = e.AltKey;
    bool meta   = e.MetaKey;
});
```

### PointerEvent

`PointerEvent` extends `MouseEvent` with pointer-specific properties:

```csharp
canvas.OnPointerDown(e => {
    // All MouseEvent properties are available, plus:
    int pointerId       = e.PointerId;   // unique pointer identifier
    double width        = e.Width;       // contact geometry width
    double height       = e.Height;      // contact geometry height
    float pressure      = e.Pressure;    // 0.0 to 1.0 pressure
    float tangentialP   = e.TangentialPressure;
    int tiltX           = e.TiltX;       // pen tilt (-90 to 90)
    int tiltY           = e.TiltY;
    int twist           = e.Twist;       // pen rotation (0 to 359)
    string pointerType  = e.PointerType; // "mouse", "pen", "touch"
    bool isPrimary      = e.IsPrimary;
});
```

## Helper Extensions

### Key Matching

The `Key` enum maps to the W3C `KeyboardEvent.key` specification values. Use `IsKey` for type-safe comparison:

```csharp
input.OnKeyDown(e => {
    if (e.IsKey(Key.Enter))      Console.WriteLine("Enter pressed");
    if (e.IsKey(Key.Escape))     Console.WriteLine("Escape pressed");
    if (e.IsKey(Key.ArrowUp))    Console.WriteLine("Up arrow");
    if (e.IsKey(Key.ArrowDown))  Console.WriteLine("Down arrow");
    if (e.IsKey(Key.Tab))        Console.WriteLine("Tab");
    if (e.IsKey(Key.Space))      Console.WriteLine("Space");
    if (e.IsKey(Key.Backspace))  Console.WriteLine("Backspace");
    if (e.IsKey(Key.Delete))     Console.WriteLine("Delete");

    // Letters and digits
    if (e.IsKey(Key.A))          Console.WriteLine("'a' key");
    if (e.IsKey(Key.D0))         Console.WriteLine("'0' digit");

    // Function keys
    if (e.IsKey(Key.F1))         Console.WriteLine("F1");
    if (e.IsKey(Key.F12))        Console.WriteLine("F12");
});
```

### Physical Key Code Matching

The `KeyCode` enum maps to `KeyboardEvent.code` (physical key position, layout-independent). Use `IsCode` for game-style controls that should work on any keyboard layout:

```csharp
document.OnKeyDown(e => {
    // WASD controls work on QWERTY, AZERTY, Dvorak, etc.
    if (e.IsCode(KeyCode.KeyW)) MoveForward();
    if (e.IsCode(KeyCode.KeyA)) MoveLeft();
    if (e.IsCode(KeyCode.KeyS)) MoveBackward();
    if (e.IsCode(KeyCode.KeyD)) MoveRight();
});
```

### Modifier Keys

The `Modifiers` flags enum and `GetModifiers()` / `HasModifier()` extensions provide a clean API for checking modifier state.

**On KeyboardEvent:**

```csharp
input.OnKeyDown(e => {
    // Get all active modifiers as a flags value
    Modifiers mods = e.GetModifiers();

    // Check for a specific modifier
    if (e.HasModifier(Modifiers.Ctrl)) {
        // Ctrl is held (possibly with other modifiers too)
    }

    // Check for exact modifier combination
    if (mods == (Modifiers.Ctrl | Modifiers.Shift)) {
        // Exactly Ctrl+Shift, nothing else
    }

    // Check that specific modifiers are included (others may be too)
    if (e.HasModifier(Modifiers.Ctrl | Modifiers.Shift)) {
        // Both Ctrl and Shift are held
    }
});
```

**On MouseEvent (same API):**

```csharp
element.OnClick(e => {
    Modifiers mods = e.GetModifiers();
    if (e.HasModifier(Modifiers.Ctrl)) {
        // Ctrl+Click
    }
    if (e.HasModifier(Modifiers.Meta)) {
        // Cmd+Click (macOS) or Win+Click (Windows)
    }
});
```

The `Modifiers` flags are:

| Flag | Value | Key |
|------|-------|-----|
| `None` | 0 | No modifiers |
| `Ctrl` | 1 | Control |
| `Shift` | 2 | Shift |
| `Alt` | 4 | Alt / Option |
| `Meta` | 8 | Windows / Command |

### Mouse Button Identification

**Which button triggered the event (`GetButton`):**

```csharp
element.OnMouseDown(e => {
    MouseButton button = e.GetButton();
    switch (button) {
        case MouseButton.Left:    // primary click
            break;
        case MouseButton.Middle:  // scroll wheel click
            break;
        case MouseButton.Right:   // context menu
            break;
        case MouseButton.Back:    // browser back button
            break;
        case MouseButton.Forward: // browser forward button
            break;
    }
});
```

**Which buttons are currently held (`GetButtons`, `HasButton`):**

```csharp
element.OnMouseMove(e => {
    // Check if left button is held (drag detection)
    if (e.HasButton(MouseButtons.Left)) {
        Console.WriteLine("Dragging with left button");
    }

    // Get all held buttons
    MouseButtons held = e.GetButtons();
    if ((held & MouseButtons.Left) != 0 && (held & MouseButtons.Right) != 0) {
        Console.WriteLine("Both left and right buttons held");
    }
});
```

### Pointer Type Detection

```csharp
canvas.OnPointerDown(e => {
    PointerType? type = e.GetPointerType();
    switch (type) {
        case PointerType.Mouse:
            Console.WriteLine("Mouse input");
            break;
        case PointerType.Touch:
            Console.WriteLine("Touch input");
            break;
        case PointerType.Pen:
            Console.WriteLine($"Pen input, pressure: {e.Pressure}");
            break;
        case null:
            Console.WriteLine("Unknown pointer type");
            break;
    }

    // Or use the direct comparison method
    if (e.IsPointerType(PointerType.Touch)) {
        // Increase hit target size for touch
    }
});
```

## Common Patterns

### Form Validation on Input

```csharp
var emailInput = Document.QuerySelector<HtmlInputElement>("#email");

using var validation = emailInput.OnInput(e => {
    string value = emailInput.Value;
    bool isValid = value.Contains('@') && value.Contains('.');

    emailInput.Style.BorderColor = isValid
        ? CssColor.Green
        : CssColor.Red;

    var errorLabel = Document.QuerySelector<HtmlElement>("#email-error");
    errorLabel.TextContent = isValid ? "" : "Please enter a valid email";
});
```

### Keyboard Shortcuts

```csharp
using var shortcuts = document.OnKeyDown(e => {
    // Ctrl+S: Save
    if (e.IsKey(Key.S) && e.HasModifier(Modifiers.Ctrl)) {
        e.PreventDefault(); // prevent browser's save dialog
        SaveDocument();
    }

    // Ctrl+Z: Undo
    if (e.IsKey(Key.Z) && e.HasModifier(Modifiers.Ctrl) &&
        !e.HasModifier(Modifiers.Shift)) {
        e.PreventDefault();
        Undo();
    }

    // Ctrl+Shift+Z: Redo
    if (e.IsKey(Key.Z) && e.HasModifier(Modifiers.Ctrl | Modifiers.Shift)) {
        e.PreventDefault();
        Redo();
    }

    // Escape: Close modal
    if (e.IsKey(Key.Escape)) {
        CloseModal();
    }
});
```

### Drag and Drop Setup

```csharp
var draggable = Document.QuerySelector<HtmlElement>("#draggable");
bool isDragging = false;
double startX = 0, startY = 0;
double elementX = 0, elementY = 0;

using var down = draggable.OnPointerDown(e => {
    isDragging = true;
    startX = e.ClientX;
    startY = e.ClientY;

    // Capture pointer to receive events even when cursor leaves the element
    draggable.SetPointerCapture(e.PointerId);
});

using var move = draggable.OnPointerMove(e => {
    if (!isDragging) return;

    double deltaX = e.ClientX - startX;
    double deltaY = e.ClientY - startY;

    draggable.Style.Transform = Transform.Translate(
        Length.Px(elementX + deltaX),
        Length.Px(elementY + deltaY));
});

using var up = draggable.OnPointerUp(e => {
    if (!isDragging) return;
    isDragging = false;

    elementX += e.ClientX - startX;
    elementY += e.ClientY - startY;

    draggable.ReleasePointerCapture(e.PointerId);
});
```

### Hover Effects

```csharp
var card = Document.QuerySelector<HtmlElement>(".card");

using var enter = card.OnMouseEnter(e => {
    card.Style.Transform = Transform.Scale(1.05);
    card.Style.BoxShadow = Shadow.Box(
        Length.Px(0), Length.Px(8),
        blur: Length.Px(24),
        color: CssColor.Rgba(0, 0, 0, 0.15));
    card.Style.Transition = Transition.All(Duration.Ms(200), Easing.EaseOut);
});

using var leave = card.OnMouseLeave(e => {
    card.Style.Transform = Transform.None;
    card.Style.BoxShadow = Shadow.Box(
        Length.Px(0), Length.Px(2),
        blur: Length.Px(8),
        color: CssColor.Rgba(0, 0, 0, 0.1));
});
```

### Input Debouncing

```csharp
var searchInput = Document.QuerySelector<HtmlInputElement>("#search");
System.Timers.Timer? debounceTimer = null;

using var inputSub = searchInput.OnInput(e => {
    debounceTimer?.Stop();
    debounceTimer?.Dispose();

    debounceTimer = new System.Timers.Timer(300); // 300ms debounce
    debounceTimer.AutoReset = false;
    debounceTimer.Elapsed += (_, _) => {
        string query = searchInput.Value;
        PerformSearch(query);
    };
    debounceTimer.Start();
});
```

## Memory Management

### Why Disposing Subscriptions Matters

Every `EventSubscription` holds a reference to a JavaScript event listener. If you create subscriptions without disposing them:

1. **The listener stays active.** Even if your C# object goes out of scope, the JavaScript `addEventListener` callback remains attached to the DOM element. Events continue to fire and invoke C# callbacks through the interop bridge.

2. **Memory leaks accumulate.** The bridge keeps references alive on both the .NET and JavaScript sides. Over time, especially in single-page applications where components mount and unmount frequently, leaked listeners consume increasing memory.

3. **Stale handlers cause bugs.** A listener from a disposed component may reference captured variables that are no longer valid, leading to null reference exceptions or operating on stale state.

### Best Practices

**Use `using` declarations for component-scoped subscriptions:**

```csharp
// These are automatically disposed when the enclosing scope/method exits
using var click = button.OnClick(HandleClick);
using var keyDown = document.OnKeyDown(HandleShortcuts);
```

**Collect subscriptions for cleanup in component lifecycle:**

```csharp
private readonly List<EventSubscription> _subscriptions = new();

protected override async Task OnBrowserApiReadyAsync() {
    _subscriptions.Add(button.OnClick(HandleClick));
    _subscriptions.Add(input.OnInput(HandleInput));
    _subscriptions.Add(document.OnKeyDown(HandleShortcuts));
}

public void Dispose() {
    foreach (var sub in _subscriptions)
        sub.Dispose();
    _subscriptions.Clear();
}
```

**Conditionally remove and re-add listeners:**

```csharp
EventSubscription? hoverSub;

void EnableHoverEffect() {
    hoverSub?.Dispose(); // remove any existing listener
    hoverSub = element.OnMouseEnter(e => ShowTooltip());
}

void DisableHoverEffect() {
    hoverSub?.Dispose();
    hoverSub = null;
}
```

The key principle: treat `EventSubscription` like any other `IDisposable` resource. If you create it, you own its lifetime and are responsible for disposing it.
