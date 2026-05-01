# BrowserApi.Css.Authoring

C#-authored stylesheets that render to CSS at runtime, ready for Blazor.
Complete spec: [`docs/plans/browser-api/css-in-csharp.md`](../../../../docs/plans/browser-api/css-in-csharp.md).

## TL;DR — three places to wire it

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
// 3. AppStyles.cs — declare your styles. Field names auto-convert to CSS.
using BrowserApi.Css;
using BrowserApi.Css.Authoring;
using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;

public class Tokens : StyleSheet {
    public static readonly CssVar<Length> SpacingSm = new(Length.Px(8));
    public static readonly CssVar<Length> SpacingLg = new(Length.Px(16));
    public static readonly CssVar<CssColor> Primary = new(CssColor.Hex("#0066cc"));
    public static readonly CssVar<Length> Radius   = new(Length.Px(8));
}

public class AppStyles : StyleSheet {
    public static readonly Class Btn = new() {
        Display       = Display.InlineFlex,
        AlignItems    = AlignItems.Center,
        Padding       = Sides.Of(Tokens.SpacingSm, Tokens.SpacingLg),
        Background    = Tokens.Primary,
        BorderRadius  = Tokens.Radius,
        Color         = CssColor.White,
        Cursor        = Cursor.Pointer,
        FontWeight    = 600,
        BoxSizing     = BrowserApi.Css.BoxSizing.BorderBox,
        [Self.Hover]  = new() { Background = CssColor.Hex("#0052aa") },
        [Self.FocusVisible] = new() {
            Outline = Border.Solid(Length.Px(2), Tokens.Primary),
        },
        [Self.Disabled] = new() { Opacity = 0.5, Cursor = Cursor.NotAllowed },
    };
}
```

```razor
@* In any component *@
<button class="@AppStyles.Btn">Click me</button>
<button class="@AppStyles.Btn" disabled>Disabled</button>
```

That's it. `<BrowserApiCss />` scans the AppDomain for every `StyleSheet`
subclass, renders the CSS, and emits a `<style>` tag. Class names are
populated lazily so `@AppStyles.Btn` returns `"btn"` regardless of render
order. CSS variables auto-emit to a `:root` block at the top of each
stylesheet's output.

## What's working

### Selectors and rules

- Combinator operators: `*` compound, `+` adjacent sibling, `-` general sibling,
  `>` child, `>>` descendant, `|` selector list. Reverse `<` / `<<` are reserved
  by C# operator-pair rules but throw at runtime with a helpful message
  (analyzer BCA002 will turn that into a compile error).
- Pseudo-class properties on every selector: `Hover`, `Focus`, `FocusVisible`,
  `FocusWithin`, `Active`, `Disabled`, `Checked`, `FirstChild`, `LastChild`.
- Functional pseudo-classes: `NthChild(formula)`, `Not(other)`, `Has(other)`.
- Pseudo-elements `Before`/`After`/`Placeholder` return a constrained
  `PseudoElementSelector` — `Card.After.Before` is a compile error (CSS
  forbids two pseudo-elements), as is a combinator after a pseudo-element.
- Class operations: `+` for `ClassList`, `.When(bool)`, `.Variant(slug)` for BEM,
  `.External(name)` escape hatch, `Class.None` sentinel.
- `Rule(selector)` and `Rule(params Selector[])` for stylesheet-only rules
  (resets, multi-selector targets).

### At-rules and queries

- `MediaQuery.MaxWidth/MinWidth/MaxHeight/MinHeight`, `PrefersDark`,
  `PrefersLight`, `PrefersReducedMotion`, `PrefersReducedData`, `Print`,
  `Screen`, `Hover`, `Portrait`, `Landscape`. Combine with `&` (and) or `|` (any).
- `Supports.Property(name, value)`, pre-built `.Grid`/`.Flex`/`.Nesting`,
  `&` `|` `!` for AND/OR/NOT.
- `ContainerQuery.MinWidth/MaxWidth` — pairs with `ContainerType` /
  `ContainerName` / `Container` properties on `Declarations`.
- `Keyframes` with `Percentage` keys plus injected `From`/`To` constants.

### Values

- `Length` — `Px`/`Em`/`Rem`/`Vh`/`Vw`/`Percent`/`Calc`/`Auto`/`Zero`/`.Important`.
- `CssColor` — hex, rgb, rgba, hsl, hsla, named, `Inherit`/`Transparent`/`White`,
  `.Important`.
- `Percentage` — `Of(n)`/`Zero`/`.Important`.
- `Sides` — single `Length`, two-tuple `(vertical, horizontal)`,
  four-tuple via `Sides.Of(top:, right:, bottom:, left:)`. `CssVar<Length>`
  implicitly converts to `Sides`.
- `Border` — `Solid`/`Dashed`/`Dotted`/`Double` factories, `None`, `Custom`.
- `CssVar<T>` — auto-emits to `:root`, kebab-cased from field name; implicitly
  converts to `Length` / `CssColor` / `Percentage` / `Sides` for direct use as
  property values.

### Properties

About 80 typed `init`-only setters covering layout (`Display`, `Position`,
inset, overflow, visibility, z-index), box (width/height/min/max,
padding/margin all sides + logical, border shorthand and per-side, all
border-radius corners, outline), flex/grid (direction/wrap/grow/shrink,
justify/align, gap, order, grid-template-columns/rows/area), color
(color, background, opacity), typography (font-family/size/weight/style,
line-height, letter/word-spacing, text-align/decoration/transform/indent,
white-space, text-overflow), and effects (cursor, box-shadow, transform,
transition, animation, filter, backdrop-filter, pointer-events, user-select),
plus `ContainerType`/`ContainerName` for CSS containment.

### Keyword enums

Defined in this namespace: `Display`, `Position`, `Cursor`, `TextAlign`,
`FontStyle`, `TextDecoration`, `TextTransform`, `WhiteSpace`, `BorderStyle`,
`Overflow`, `JustifyContent`, `AlignItems`. Reused from `BrowserApi.Css` (CSSOM):
`BoxSizing`, `Visibility`, `FlexDirection`, `FlexWrap`. Serializer honors
`[StringValue]` attributes and falls back to PascalCase → kebab-case.

### Auto-discovery

- `CssRegistry` walks the AppDomain at first access (or when
  `AddBrowserApiCss()` is called during DI), discovers every `StyleSheet`
  subclass, and renders all CSS once.
- `<BrowserApiCss />` Blazor component emits the combined CSS as a `<style>`
  tag — drop-in for `App.razor`'s `<HeadContent>`.
- Class names and CssVar names are populated lazily on first access, so
  Razor markup works regardless of render order.

### Element selectors

`El.Root` (`:root`), `El.All` (`*`), and the common HTML elements:
`Html`/`Body`/`Div`/`Span`/`P`/`A`/`Button`/`Input`/`Textarea`/`Select`/
`Label`/`Form`/`Ul`/`Ol`/`Li`/`Table`/`Tr`/`Td`/`Th`/`Thead`/`Tbody`/
`Article`/`Section`/`Nav`/`Header`/`Footer`/`Main`/`Aside`/`Img`/`Svg`/
`H1`–`H6`. The full HTML-spec list will be auto-generated next.

### Tests

83 passing unit tests across `StyleSheetTests`, `SelectorTests`,
`InjectedHelperTests`, `CssRegistryTests`, `PropertySurfaceTests`,
`SidesTests`, `MediaQueryTests`, `KeyframesTests`, `EndToEndTests`, and
`CookbookTests` (the cookbook doubles as documentation of real-world
patterns: button states, card with dark mode, form reset, responsive
grid, keyframe animations with reduced-motion guard, container queries,
@supports progressive enhancement, !important).

## What's NOT here yet

In rough priority order:

1. **Roslyn source generator** — replaces runtime reflection scan with
   compile-time emission. Spec §12 design is locked in; implementation is
   the next big rock. Runtime path works for now and the perf cost is one
   reflection pass per app lifetime.
2. **Full property surface** — about 80 properties exist, the spec calls for
   hundreds generated from `specs/css/*.json`.
3. **Property-specific keyword types** — `FontSize.Large`, `Width.Auto`, etc.
   Currently keywords are exposed via static factory members on the value
   types (`Length.Auto`).
4. **Full HTML-spec El.*** — currently a hand-written subset; full set from
   the HTML spec data is a few-line addition once the spec parser runs.
5. **Color SCSS-vs-CSS dispatch** (`Lighten`, `Mix`, `Saturate`, etc.) and
   `IsVariable` taint propagation across all `ICssValue`s. Spec §29.
6. **`.Important` extension properties on enum keywords** — Length/Color/
   Percentage already have `.Important`; enum keywords need the C# 14
   extension-property pattern.
7. **External CSS parser** — auto-import MudBlazor classes/vars (`Mud.*`)
   from a NuGet package's CSS files. Spec §10.
8. **Analyzers BCA001/2/3** — sides shorthand named-args, reverse selector
   operator usage, specificity threshold.
9. **Sass intermediate output + chained source maps** — the emitter resolves
   `&` itself today; sass would let users debug at the SCSS layer per spec §27.
10. **`@layer` / scroll-driven animations** — post-MVP per spec §33.

## Architectural notes

- **Two `StyleSheet` types coexist.** `BrowserApi.Css.StyleSheet` is the
  CSSOM type generated from WebIDL (DOM API for runtime CSSOM access).
  `BrowserApi.Css.Authoring.StyleSheet` is the static-stylesheet authoring
  base added here. Use the alias pattern in consumers:
  `using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;`.
- **Some keyword enums are reused from CSSOM.** Where the CSSOM generator
  already produces an enum with `[StringValue]` attributes (BoxSizing,
  Visibility, FlexDirection, FlexWrap), we use those directly rather than
  duplicating. The keyword serializer honors `[StringValue]` when present
  and falls back to PascalCase → kebab-case otherwise.
- **`Declarations` is non-abstract.** Target-typed `new() { … }` works
  inside the nesting indexer because `Declarations` is concrete; `Class`
  and `Rule` derive for their specific behaviors.
- **Render order = source order.** Property and nested-rule lists preserve
  insertion order. No OrderedDictionary, no merge logic. Same-key duplicates
  emit as duplicate blocks; CSS handles via cascade.
- **Lazy resolution of names.** `Class.Name` / `CssVar<T>.Name` lookup
  triggers `CssRegistry.EnsureScanned()` if empty, so any consumption path
  (Razor markup, direct `ToCss()`) gets correct values regardless of
  initialization order.
- **Field-name → CSS naming.** PascalCase converts to kebab-case (`MyCard` →
  `my-card`). Digit handling: no hyphen between letter and digit
  (`Sp1` → `sp1`, `H1Heading` → `h1-heading`). For numbered tokens, prefer
  descriptive names (`SpacingSm/Md/Lg`) over numeric suffixes (`Sp1/2/3`).

## Sample rendered output

For the `Tokens` + `AppStyles` shown at the top:

```css
:root { --spacing-sm: 8px; --spacing-lg: 16px; --primary: #0066cc; --radius: 8px; }
.btn { display: inline-flex; align-items: center; padding: var(--spacing-sm) var(--spacing-lg); background: var(--primary); border-radius: var(--radius); color: white; cursor: pointer; font-weight: 600; box-sizing: border-box; }
.btn:hover { background: #0052aa; }
.btn:focus-visible { outline: 2px solid var(--primary); }
.btn:disabled { opacity: 0.5; cursor: not-allowed; }
```
