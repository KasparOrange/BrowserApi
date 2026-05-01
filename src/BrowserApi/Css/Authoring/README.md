# BrowserApi.Css.Authoring

C#-authored stylesheets that render to CSS at runtime, ready for Blazor.

## TL;DR — drop in 3 places

```csharp
// 1. Program.cs — eager scan (optional but recommended):
builder.Services.AddBrowserApiCss();
```

```razor
@* 2. App.razor — emit the <style> tag *@
<HeadContent>
    <BrowserApiCss />
</HeadContent>
```

```csharp
// 3. AppStyles.cs (or any file) — declare your styles:
using BrowserApi.Css;
using BrowserApi.Css.Authoring;
using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;

public class Tokens : StyleSheet {
    public static readonly CssVar<Length> Radius = new(Length.Px(8));
    public static readonly CssVar<CssColor> Primary = new(CssColor.Hex("#0066cc"));
}

public class AppStyles : StyleSheet {
    public static readonly Class Btn = new() {
        Display       = Display.InlineFlex,
        AlignItems    = AlignItems.Center,
        Padding       = Length.Px(8),
        Background    = Tokens.Primary,
        BorderRadius  = Tokens.Radius,
        Color         = CssColor.White,
        Cursor        = Cursor.Pointer,
        FontWeight    = 600,
        BoxSizing     = BrowserApi.Css.BoxSizing.BorderBox,
        [Self.Hover]  = new() { Background = CssColor.Hex("#0052aa") },
    };
}
```

```razor
@* In any component *@
<button class="@AppStyles.Btn">Click me</button>
```

That's it. The `<BrowserApiCss />` component scans the AppDomain for every
`StyleSheet` subclass, renders the CSS, and emits a `<style>` tag. Class names
are populated lazily so `@AppStyles.Btn` returns `"btn"` regardless of render
order.

## What's working

| Feature | Status |
|---|---|
| Selector composition (operators + pseudo-classes) | ✓ |
| Pseudo-element terminal type | ✓ |
| `Class`, `Rule`, `ClassList`, `Class.None`, `.When`, `.Variant`, `.External` | ✓ |
| Nesting via indexer (`[Self.Hover] = new() { … }`) | ✓ |
| `Self`, `From`, `To`, `Is(...)`, `Where(...)` injected helpers | ✓ |
| `El.*` predefined element selectors | ✓ (common HTML; full HTML spec list TBD) |
| `CssVar<T>` with auto-`:root` emission | ✓ (Length, CssColor, Percentage) |
| Auto-discovery of all `StyleSheet` types via `CssRegistry` | ✓ |
| Blazor `<BrowserApiCss />` component | ✓ |
| ~80 typed CSS properties (layout, box, flex/grid, color, text, effects) | ✓ |
| Keyword enums w/ kebab-case serialization (Display, Position, Cursor, …) | ✓ |
| Reuses CSSOM-generated enums (BoxSizing, Visibility, FlexDirection, FlexWrap) | ✓ |
| `Border.Solid/Dashed/Dotted/Double` factories | ✓ |
| Runtime CSS render via reflection | ✓ |
| `&` (SCSS parent) resolution at render time | ✓ |
| 36 unit tests covering all of the above | ✓ |

## What's NOT here yet

In rough priority order:

1. **Roslyn source generator** — replaces the runtime reflection scan with
   compile-time emission. Needed for production but the runtime path works
   correctly today and is fast enough for most apps.
2. **Full property surface** — the spec calls for hundreds of CSS properties
   typed from `specs/css/*.json`. Currently ~80 representative ones exist.
3. **`LengthOrPercentage` / `NumberOrPercentage` / `Image`** union wrappers
   for properties that genuinely accept both kinds.
4. **`@media` / `@supports` / `@container` indexers** — only `Selector`
   indexer is wired today.
5. **`Keyframes` / `FontFace` types** — `@keyframes` and `@font-face`.
6. **Color SCSS-vs-CSS function dispatch** (`Lighten`, `Mix`, etc.) and
   the `IsVariable` taint propagation across all `ICssValue`.
7. **Analyzers BCA001/2/3** for sides shorthand, reverse selector operators,
   specificity threshold.
8. **External CSS parser** (auto-import MudBlazor classes/vars).
9. **Sass intermediate output + source maps** — currently the emitter
   resolves `&` itself and produces CSS directly; sass would let users
   read SCSS as the intermediate representation per spec §27.
10. **`.Important` via C# 14 extension properties** — the API design is
    decided in spec §14 but the implementation isn't shipped.

See `docs/plans/browser-api/css-in-csharp.md` for the full spec.

## Architectural notes

- **Two `StyleSheet` types coexist.** `BrowserApi.Css.StyleSheet` is the
  CSSOM type generated from WebIDL (DOM API for runtime CSSOM access).
  `BrowserApi.Css.Authoring.StyleSheet` is the static-stylesheet authoring
  base added here. They serve unrelated purposes; consumers either pick
  one namespace or alias as shown above.
- **Some keyword enums are reused from CSSOM.** `BoxSizing`, `Visibility`,
  `FlexDirection`, `FlexWrap` already exist in `BrowserApi.Css` with
  `[StringValue]` attributes; we use those directly. `Display`, `Position`,
  `Cursor`, etc. are defined here under `BrowserApi.Css.Authoring`. The
  keyword serializer (`KeywordExtensions.AsCss`) honors `[StringValue]`
  when present, falls back to PascalCase → kebab-case otherwise.
- **`Declarations` is non-abstract.** This is what makes target-typed
  `new() { … }` work inside the nesting indexer. `Class` and `Rule` derive
  from it for their specific behaviors (name + Razor conversions, selector
  + constructors).
- **Render order = source order.** No `OrderedDictionary` or merge logic —
  the reflection walk visits fields in declaration order and emits as-is.
  Same-key duplicates emit as duplicate blocks (CSS handles via cascade).
- **Class name resolution is lazy.** Implicit string conversion and
  `.Selector` access trigger `CssRegistry.EnsureScanned()` on demand so
  Razor markup works regardless of component render order.

## Sample rendered output

Input:

```csharp
public class Tokens : StyleSheet {
    public static readonly CssVar<Length> Radius = new(Length.Px(8));
    public static readonly CssVar<CssColor> Primary = new(CssColor.Hex("#0066cc"));
}
```

Output:

```css
:root { --radius: 8px; --primary: #0066cc; }
```

Input:

```csharp
public class Components : StyleSheet {
    public static readonly Class Btn = new() {
        Display      = Display.InlineFlex,
        Padding      = Length.Px(8),
        Background   = Tokens.Primary,
        BorderRadius = Tokens.Radius,
        Color        = CssColor.White,
        Cursor       = Cursor.Pointer,
        [Self.Hover] = new() { Background = CssColor.Hex("#0052aa") },
    };
}
```

Output (approximate; actual whitespace from emitter is more compact):

```css
.btn { display: inline-flex; padding: 8px; background: var(--primary); border-radius: var(--radius); color: white; cursor: pointer; }
.btn:hover { background: #0052aa; }
```
