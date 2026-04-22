# CSS-in-C# API Design

**Parent:** [css.md](css.md)

This document tracks the design decisions for expressing CSS in C#. Each CSS concept is listed with its status, the chosen approach (if decided), and alternatives considered.

---

## CSS Structural Reference

Before the decision tracker, a quick reference of CSS concepts and how they relate.

```
Stylesheet
├── Rule (selector + declaration block)
│   ├── Selector ─── "which elements?"
│   │   ├── Simple selectors (atomic)
│   │   │   ├── Type selector        div, p, h1
│   │   │   ├── Class selector       .card, .active
│   │   │   ├── ID selector          #header
│   │   │   ├── Universal selector   *
│   │   │   └── Attribute selector   [href], [type="text"]
│   │   ├── Pseudo-class             :hover, :focus, :nth-child(2)
│   │   │   (always attached to another selector — modifies "when/which")
│   │   ├── Pseudo-element           ::before, ::after
│   │   │   (always last — selects a sub-part of the element)
│   │   ├── Compound selector        .card.active:hover
│   │   │   (multiple simple + pseudo on SAME element, no spaces)
│   │   ├── Complex selector         .card > .title:hover
│   │   │   (compounds joined by combinators: >, space, +, ~)
│   │   └── Selector list            .card, .panel
│   │       (comma-separated, means "any of these")
│   └── Declaration block ─── "what styles?"
│       └── Declaration              property: value
│           ├── Property             color, display, padding
│           ├── Value                red, flex, 8px
│           └── !important           priority override flag
├── At-rules
│   ├── @media (query) { rules }
│   ├── @keyframes name { stops }
│   ├── @supports (condition) { rules }
│   └── @font-face { declarations }
└── Custom Properties
    ├── Definition       --name: value    (set on any selector, inherits)
    └── Usage            var(--name)      (reads the inherited value)
```

**Key facts:**
- Pseudo-classes are NOT standalone selectors. They always modify another selector. `:hover` alone implicitly means `*:hover`.
- Pseudo-elements MUST be last in a compound selector. `.card:hover::before` is valid; `.card::before:hover` is not.
- Custom properties cascade and inherit like normal CSS properties. A property set on `.card` is available to all descendants of `.card`.
- `!important` overrides the normal cascade. It's a flag on individual declarations, not on rules.

---

## Decision Tracker

### 1. CSS Values (Length, Color, etc.)

**Status: DECIDED**

Values are readonly structs implementing `ICssValue` with a `ToCss()` method. Extension properties on `int`/`double` provide unit syntax.

```csharp
// Chosen:
16.Px           // Length — C# 14 extension property, no parentheses
1.5.Rem         // Length
Color.Hex("#fff")
Color.Rgba(0, 0, 0, 0.5)
45.Deg          // Angle
200.Ms          // Duration
50.Percent      // Percentage
1.Fr            // Flex
```

**Ruled out:**
```csharp
// ❌ Extension methods (requires parentheses)
16.Px()

// ❌ Static factory only (verbose)
Length.Px(16)
```

**Pending change:** Rename `CssColor` → `Color`.

---

### 2. Class vs Rule Distinction

**Status: DECIDED**

In CSS, everything is a "rule" (selector + declarations). Our API splits this into two types based on how they're USED in C#:

- **`Class`** — a CSS class you reference in Razor markup. Has a name (from field), implicit string conversion, `.When()`, `.Variant()`, `+` operator.
- **`Rule`** — a CSS rule you don't reference in markup. Just exists in the stylesheet. Used for `:root`, element resets, compound selectors that don't define new classes.

```csharp
// Class — referenced in Razor via @Card
public static readonly Class Card = new() {
    Background = Color.White,
    BorderRadius = 8.Px,
};

// Rule — just exists in the CSS, never referenced in markup
public static readonly Rule ResetBody = new(El.Body) {
    Margin = 0.Px,
};
```

**Anonymous rules** — related rules that don't need names go in a `Rules` collection:
```csharp
public static readonly Rules Reset = [
    new(El.All) { BoxSizing = BoxSizing.BorderBox },
    new(El.Body) { Margin = 0.Px },
    new(El.Html) { FontSize = 16.Px },
];
```

**Discovery by type, not by name.** The source generator collects ALL fields by their type (`Class`, `Rule`, `Rules`, `CssVar<T>`, `Keyframes`, `FontFace`). Field names are for human readability only — never magic conventions. You can have multiple `Rules` fields named anything:
```csharp
public static readonly Rules Reset = [...];      // collected
public static readonly Rules Typography = [...]; // also collected
public static readonly Rules RootVars = [...];   // also collected
// All three are found because they're type Rules — not because of their names.
```

**`.Selector` property** — `Class` has implicit string conversion returning the name without dot (`"card"` for `class=""`). The `.Selector` property returns the CSS selector with dot (`".card"` for use in component parameters expecting CSS selectors). Prefer updating component parameters to accept `Class` directly instead.

In Razor: `<div class="@DndTestStyles.Card">` — prefer explicit stylesheet reference over `using static` for discoverability.

**Ruled out:**
```csharp
// ❌ Lambda builder (too imperative for simple declarations)
public static readonly Class Card = Css.Class(s => {
    s.Background = Color.White;
});

// ❌ Nested class per rule (too verbose)
public class Card : Rule {
    public Color Background => Color.White;
}

// ❌ Fluent chain (every CSS prop becomes a method — massive builder)
public static readonly Class Card = Css
    .Background(Color.White)
    .BorderRadius(8.Px);
```

---

### 3. Selector Operators

**Status: DECIDED**

CSS combinators map to C# operators. Precedence naturally matches CSS specificity.

| CSS | Meaning | C# Operator | Fluent Method | Precedence |
|-----|---------|-------------|---------------|------------|
| `.a:hover` | pseudo-class | `A.Hover` (property) | `A.On(Hover)` | — (property access) |
| `.a.b` | compound | `A * B` | `A.And(B)` | 3 (highest binary) |
| `.a + .b` | adjacent sibling | `A + B` | `A.Adjacent(B)` | 4 |
| `.a ~ .b` | general sibling | `A - B` | `A.Sibling(B)` | 4 |
| `.a .b` | descendant | `A >> B` | `A.Descendant(B)` | 5 |
| `.a > .b` | child | `A > B` | `A.Child(B)` | 6 |
| `.a, .b` | selector list | params / `\|` | `A.Or(B)` | 10 (lowest) |

**Why `*` for compound:** `&` has lower precedence than `>`, causing `A & B > C` to parse as `A & (B > C)`. `*` has precedence 3 — binds tighter than all combinators.

**Why `-` for general sibling:** `~` is unary in C# (bitwise complement). `-` pairs visually with `+` (adjacent sibling). Loose coupling vs tight coupling.

**Operator pairing:** `>` requires `<` (throws `NotSupportedException`). `>>` requires `<<` (same).

**Ruled out:**
```csharp
// ❌ & for compound (precedence too low — binds looser than >)
Card & Active > Title  // parses as Card & (Active > Title) — WRONG

// ❌ && for compound (even lower precedence, requires true/false operators)
// ❌ ~ for general sibling (unary only in C#)
// ❌ Indexer for compound (conflated with pseudo-class indexer in earlier design)
```

---

### 4. Pseudo-classes

**Status: DECIDED**

Pseudo-classes are computed properties on `Selector` (not a separate `PseudoClass` type passed to an indexer).

```csharp
// Chosen:
Card.Hover              // .card:hover
Card.Focus              // .card:focus
Card.FirstChild         // .card:first-child
El.Input.Disabled       // input:disabled
Card.Hover.After        // .card:hover::after (pseudo-element)

// Functional:
Card.NthChild(2)        // .card:nth-child(2)
Card.Not(Disabled)      // .card:not(.disabled)  — Disabled is a Class
Card.Has(Title)         // .card:has(.title)
```

**Ruled out:**
```csharp
// ❌ Separate PseudoClass type with indexer (v1 design)
Card[Pseudo.Hover]      // verbose, extra type
Card[Hover]             // ambiguous when CssClass also named Hover

// ❌ Static values with using static
using static Pseudo;
Card[Hover]             // naming collision with :active (pseudo) vs .active (class)
```

**Why properties won out:** Pseudo-classes always attach to a selector in CSS. Making them properties mirrors this — `.Hover` is a modifier of `Card`, not an independent thing.

**For nesting (object initializer),** `Self.Hover` is used since there's no instance. `Self` = `&` (SCSS parent reference).

---

### 5. Nesting

**Status: DECIDED**

Recursive indexer on `Declarations` type. Each nested block has the same indexer, enabling unlimited depth.

```csharp
// Chosen:
public static readonly Class Card = new() {
    Background = Color.White,

    [Self.Hover] = new() {                    // &:hover
        BoxShadow = Shadows.Lg,

        [Self.After] = new() {                // &:hover::after
            Content = Css.String("→"),
        },
    },

    [Self > Title] = new() {                  // & > .title
        FontSize = 1.25.Rem,
    },

    [Media.MaxWidth(768.Px)] = new() {        // @media (max-width: 768px)
        Padding = 4.Px,
    },
};
```

**Ruled out:**
```csharp
// ❌ Flat separate Rule fields (no nesting, verbose, names everything)
public static readonly Rule CardHovered = new(Card.Hover) { ... };

// ❌ Lambda nesting (imperative, less declarative)
Css.Class(card => {
    card.On(card.Hover, s => { s.BoxShadow = Shadows.Lg; });
});
```

---

### 6. Selector Lists

**Status: DECIDED (params), operator `|` as alternative**

```csharp
// params constructor for Rule:
public static readonly Rule Interactive = new(Card.Hover, Panel.Hover, Btn.Focus) {
    Outline = Outline.Ring(Color.Blue),
};

// params indexer for nesting:
[Self.Hover, Self.FocusWithin] = new() {
    Outline = Outline.Ring(Color.Blue),
},

// | operator for inline composition:
var both = Card.Hover | Panel.Hover;
```

Confirmed: C# 13+ `params` works in indexer setters and object initializers.

---

### 7. Keyframes

**Status: DECIDED**

Indexer initializer with `Percentage` keys. `From`/`To` are `Percentage` constants (0%, 100%).

```csharp
public static readonly Keyframes FadeIn = new() {
    [From] = new() { Opacity = 0 },
    [50.Percent] = new() { Opacity = 0.5 },
    [To] = new() { Opacity = 1 },
};
```

---

### 8. Media Queries

**Status: DECIDED**

Typed media feature values. Used as nesting indexer keys.

```csharp
// Typed construction (no strings):
Media.MaxWidth(768.Px)
Media.MinWidth(1024.Px)
Media.PrefersDark
Media.PrefersReducedMotion
Media.Print

// Combined:
Media.MinWidth(768.Px) & Media.MaxWidth(1024.Px)   // and

// In nesting:
[Media.MaxWidth(768.Px)] = new() { ... }
```

---

### 9. Element Selectors

**Status: DECIDED**

Pre-defined `Selector` instances in a static `El` class. Generated from HTML spec.

```csharp
using static BrowserApi.Css.El;

El.Div, El.P, El.H1, El.Ul, El.Li, El.A, El.Input, El.Span, El.Table, ...
// or with using static:
Div, P, H1, Ul, Li, A, Input, Span, Table, ...

// Usage:
El.Li * Active > El.Span   // li.active > span
```

---

### 10. External Classes

**Status: DECIDED**

Classes and CSS variables from external frameworks (MudBlazor, etc.) are **auto-generated** by the source generator from the framework's CSS files. Not hand-written.

#### Setup

```xml
<!-- .csproj — declare external CSS sources -->
<ItemGroup>
    <ExternalCss Include="$(PkgMudBlazor)/staticwebassets/**/*.css"
                 RootClass="Mud" />
</ItemGroup>
```

The source generator:
1. Reads CSS files tagged `ExternalCss`
2. Extracts `.mud-*` class selectors → typed `Class.External()` constants
3. Extracts `--mud-*` custom properties → typed `CssVar.External()` constants
4. Groups by dash-separated segments: `mud-table-cell` → `Mud.Table.Cell`
5. Emits nested static classes under the `RootClass` name

#### Generated API

```csharp
// Auto-generated — never hand-written:
public static class Mud {
    public static class Palette {
        public static readonly CssVar<Color> Primary = CssVar.External("--mud-palette-primary");
        public static readonly CssVar<Color> Surface = CssVar.External("--mud-palette-surface");
        public static readonly CssVar<Color> TextDisabled = CssVar.External("--mud-palette-text-disabled");
        // ... every --mud-palette-* variable
    }

    public static readonly CssVar<Length> AppBarHeight = CssVar.External("--mud-appbar-height");

    public static class Table {
        public static readonly Class Root = Class.External("mud-table");
        public static readonly Class Cell = Class.External("mud-table-cell");
        public static readonly Class Container = Class.External("mud-table-container");
    }

    public static class Dialog {
        public static readonly Class Root = Class.External("mud-dialog");
        public static readonly Class Container = Class.External("mud-dialog-container");
        public static readonly Class Content = Class.External("mud-dialog-content");
    }
    // ... every mud-* component
}
```

#### Usage

```csharp
// Type "Mud." → IntelliSense shows everything, organized by component
public static readonly Rule DialogBlur = new(Mud.Dialog.Container > Mud.Overlay.Dialog) {
    BackdropFilter = Filter.Blur(10.Px),
};

public static readonly Class Card = new() {
    Background = Mud.Palette.Surface,
    Color = Mud.Palette.TextPrimary,
};
```

Update MudBlazor NuGet → types regenerate automatically. No manual step.

#### Manual fallback

For edge cases the auto-generator can't parse:
```csharp
// Hand-written external reference
public static readonly Class SomeWeirdThing = Class.External("weird-thing");
```

---

### 11. ClassList (composing classes in Razor)

**Status: DECIDED**

Operator `+` on `Class` returns a `ClassList` struct. Zero-allocation for 1-4 classes.

```csharp
// In Razor:
<div class="@(Card + Active + Round)">
// ClassList has implicit string conversion → "card active round"
```

**String escape hatch** — for raw class names from frameworks without typed references:
```csharp
<div class="@(Card + "some-external-class")">
// Works via operator overload: Class + string → ClassList
```

Also available: `Css.Classes(params Class[])` for dynamic/enumerable cases.

---

### 12. Build Pipeline

**Status: DECIDED**

```
C# StyleSheet → .scss file → sass compiler → .css file → wwwroot/
```

MSBuild target runs before build. C# is the authoring language, SCSS is plumbing, CSS is the output. Zero runtime cost.

---

### 13. Asset Source Generator

**Status: DECIDED (design), not yet implemented**

Roslyn incremental source gen scans `wwwroot/` via `AdditionalFiles`, emits typed `Assets` class. File rename/delete triggers regen.

```csharp
Assets.Css.App          // "css/app.css"
Assets.Images.Logo      // "images/logo.svg"
```

Code fix provider (future): suggests rename when a reference breaks.

---

### 14. `!important`

**Status: DECIDED**

Property on each value type returning a copy with the `!important` flag set. No parentheses.

```csharp
// Chosen:
Padding = 0.Px.Important
BackgroundColor = Mud.Palette.Primary.Important
Display = Display.None.Important
```

Internals — boolean flag stored on the value struct:
```csharp
public readonly struct Length : ICssValue {
    private readonly string _value;
    private readonly bool _important;

    public Length Important => new(_value, important: true);
    public string ToCss() => _important ? _value + " !important" : _value;
}
```

Every value type gets this property. For generated types, the generator adds it.

**Open sub-issue:** Enum-based CSS keyword types (like `Display`) can't have properties. Options: make them structs instead of enums, or use C# 14 extension properties on enums. To be resolved during implementation.

**Ruled out:**
```csharp
// ❌ Wrapper type Important<T> (complicates property types)
// ❌ Extension method .Important() (parentheses)
// ❌ .IMPORTANT all-caps (violates .NET conventions)
// ❌ Declaration-level !important (not valid C# syntax)
```

---

### 15. Attribute Selectors

**Status: OPEN — candidates below**

CSS attribute selectors:
```css
[href]              /* has attribute */
[type="text"]       /* equals */
[class~="card"]     /* word in space-separated list */
[lang|="en"]        /* starts with value or value- */
[href^="https"]     /* starts with */
[href$=".pdf"]      /* ends with */
[style*="width:0%"] /* contains substring */
```

#### Option A: Strings only

All attribute names are strings. Simple, flexible, but no IntelliSense for attribute names.

```csharp
Attr("href")                              // [href]
Attr("type", "text")                      // [type="text"]
Attr("style").Contains("width:0%")        // [style*="width:0%"]
Attr("href").StartsWith("https")          // [href^="https"]
Attr("href").EndsWith(".pdf")             // [href$=".pdf"]
Attr("class").HasWord("card")             // [class~="card"]
Attr("lang").DashMatch("en")              // [lang|="en"]
```

**Pro:** Simple, one pattern. Fluent matchers are discoverable via `Attr("x").`.
**Con:** Attribute names are magic strings — typos are silent.

#### Option B: Typed constants + string fallback

Standard HTML attributes are pre-defined constants (generated from HTML spec). Custom/data attributes use string overload.

```csharp
// Tier 1: Standard HTML attributes — typed constants, generated from HTML spec
Attr.Href                                 // [href]
Attr.Type.Equals("text")                  // [type="text"]
Attr.Style.Contains("width:0%")           // [style*="width:0%"]
Attr.Disabled                             // [disabled]
Attr.Lang.DashMatch("en")                 // [lang|="en"]

// Tier 2: ARIA role — typed constant (no prefix, very common)
Attr.Role.Equals("button")               // [role="button"]
Attr.Role.Equals("navigation")           // [role="navigation"]

// Tier 3: ARIA attributes — typed constants, generated from WAI-ARIA spec
Attr.Aria.Label                           // [aria-label]
Attr.Aria.Hidden.Equals("true")           // [aria-hidden="true"]
Attr.Aria.Expanded                        // [aria-expanded]
Attr.Aria.Controls                        // [aria-controls]
Attr.Aria.Live.Equals("polite")           // [aria-live="polite"]

// Tier 4: data-* attributes — typed helper, prepends "data-"
Attr.Data("stick-value").Equals("0")      // [data-stick-value="0"]
Attr.Data("field-id")                     // [data-field-id]

// Tier 5: Escape hatch — any attribute, raw string
Attr("potato", "yes")                     // [potato="yes"]
Attr("custom-thing").Contains("x")        // [custom-thing*="x"]
```

All tiers return `AttrSelector` with the same fluent matchers: `.Equals()`, `.Contains()`, `.StartsWith()`, `.EndsWith()`, `.HasWord()`, `.DashMatch()`.

| Tier | Source Spec | Helper | Example |
|------|-----------|--------|---------|
| Standard HTML | HTML spec | `Attr.Type`, `Attr.Href`, ... | `Attr.Disabled` |
| ARIA role | WAI-ARIA | `Attr.Role` | `Attr.Role.Equals("button")` |
| ARIA `aria-*` | WAI-ARIA | `Attr.Aria.Label`, `Attr.Aria.Hidden`, ... | `Attr.Aria.Expanded` |
| Custom `data-*` | HTML spec | `Attr.Data("field-id")` | `Attr.Data("value").Equals("0")` |
| Escape hatch | — | `Attr("name")` | `Attr("potato")` |

**Pro:** IntelliSense for standard + ARIA attributes, typed helpers for `data-*` and `aria-*`, string fallback for anything else.
**Con:** Larger API surface (but standard/ARIA constants can be generated from specs we already have).

**Chosen: B** — typed constants for standard HTML attributes (generated from HTML spec), `Attr.Role` for ARIA role, typed constants for `aria-*` (generated from WAI-ARIA spec), `Attr.Data()` for `data-*`, and `Attr()` string overload as escape hatch for non-standard attributes.

---

### 16. CSS Custom Properties (Variables)

**Status: OPEN — candidates below**

#### How CSS custom properties work

Custom properties are declared with `--` prefix on any selector. They **cascade and inherit** to all descendants:

```css
/* Global — available everywhere */
:root {
    --primary: #3498db;
    --spacing: 1rem;
}

/* Scoped — available to .card and its descendants */
.card {
    --card-radius: 8px;
    border-radius: var(--card-radius);
}

/* .card > .title inherits --card-radius from .card */
.card > .title {
    border-radius: var(--card-radius);
}
```

Unlike SCSS variables (`$var`), CSS custom properties are **live at runtime**. Changing `--primary` in dark mode changes all usages instantly. This is why they exist alongside our C# "variables" (which are compile-time constants).

**Chosen: Self-contained CssVar with default + optional overrides in initializer**

The `CssVar<T>` declaration IS the definition. Default value in constructor auto-emits in `:root`. Object initializer adds conditional overrides (media queries etc.). Scoped overrides go inside the `Class` that needs them.

```csharp
// Simple — just a default value (emits :root { --radius: 8px; })
public static readonly CssVar<Length> Radius = new(8.Px);

// Full — default + conditional overrides, all in one place
public static readonly CssVar<Color> Bg = new(Color.White) {
    [Media.PrefersDark] = Color.Hex("#0a0a0a"),
    [Media.HighContrast] = Color.Black,
};

// External — no default, no overrides (owned by MudBlazor)
public static readonly CssVar<Color> MudPrimary = CssVar.External("--mud-palette-primary");

// Using them — just reference by name:
public static readonly Class Card = new() {
    Background = Bg,         // → background: var(--bg)
    BorderRadius = Radius,   // → border-radius: var(--radius)
    Color = MudPrimary,      // → var(--mud-palette-primary)

    [Radius] = 12.Px,        // scoped override: .card { --radius: 12px; }
};
```

CSS output:
```css
:root { --bg: white; --radius: 8px; }
@media (prefers-color-scheme: dark) { :root { --bg: #0a0a0a; } }
@media (prefers-high-contrast) { :root { --bg: black; } }
.card { background: var(--bg); border-radius: var(--radius); --radius: 12px; }
```

**Ruled out:**
```csharp
// ❌ Option A: String-based Css.Var<T>("--name") (magic strings)
// ❌ Option B: Separate declaration + assignment in Rules (value far from declaration)
```

---

### 17. CSS Functions

**Status: PARTIALLY DECIDED**

| CSS Function | C# API | Status |
|---|---|---|
| `calc()` | `Css.Calc(100.Vh - 50.Px)` or Length operators | Decided |
| `var()` | `Css.Var<T>("--name")` | Open (see §16) |
| `url()` | `Css.Url("path")` / `Css.UrlFromFile("file")` | Open |
| `rgb()/rgba()` | `Color.Rgb()` / `Color.Rgba()` | Decided |
| `hsl()/hsla()` | `Color.Hsl()` / `Color.Hsla()` | Decided |
| `linear-gradient()` | `Gradient.Linear()` | Decided |
| `repeat()` | `Grid.Repeat()` | Open |
| `minmax()` | `Grid.MinMax()` | Open |
| `clamp()` | `Css.Clamp(min, preferred, max)` | Open |

---

### 18. Value Shorthands

**Status: OPEN**

CSS shorthands like `padding: 10px 20px` set multiple properties at once.

```csharp
// Option A: Css.Sides() helper
Padding = Css.Sides(10.Px, 20.Px)              // 10px 20px
Padding = Css.Sides(10.Px, 20.Px, 10.Px, 20.Px) // all four

// Option B: Tuple syntax
Padding = (10.Px, 20.Px)

// Option C: Setter methods (current design for some)
SetPadding(10.Px, 20.Px)
```

---

### 19. Self Keyword

**Status: MOSTLY DECIDED**

`Self` is a static `Selector` representing `&` (SCSS parent reference). Used inside nesting indexers.

```csharp
[Self.Hover] = ...          // &:hover
[Self > Title] = ...        // & > .title
[Self >> El.Span] = ...     // & span
```

`Self` has all pseudo-class properties (`.Hover`, `.Focus`, etc.) and all combinator operators.

---

### 20. Prefixing

**Status: DECIDED**

Automatic class name prefixing at two levels: global (project-wide) and per-stylesheet.

#### Global prefix — Program.cs configuration

The source generator reads the `AddBrowserApiCss` call from the Program.cs syntax tree (AST pattern matching, not runtime execution). The value must be a string literal or const.

```csharp
// In Program.cs — idiomatic .NET configuration
builder.Services.AddBrowserApiCss(options => {
    options.GlobalPrefix = "mw";
});
```

This is ALSO a real runtime method that registers Blazor services (AssetLink component, etc.). The source generator reads the same call for compile-time prefix info — one line, two purposes.

If the source generator can't extract the value (e.g., it's a variable, not a literal), it emits a diagnostic warning.

**Ruled out:**
```csharp
// ❌ Assembly attribute (works, but unfamiliar to most .NET devs)
[assembly: CssPrefix("mw")]

// ❌ MSBuild property (hidden in .csproj, not discoverable)
<CssPrefix>mw</CssPrefix>

// ❌ Convention from assembly name (too magic, fragile)
// MitWare.Blazor → "mw" automatically
```

#### Per-stylesheet prefix — attribute

```csharp
[Prefix("sp")]
public static partial class ShiftPlannerStyles : StyleSheet { }
```

#### Prefix chain: global → stylesheet → class name

```csharp
[assembly: ...] // or Program.cs: GlobalPrefix = "mw"

[Prefix("sp")]
public static partial class ShiftPlannerStyles : StyleSheet
{
    public static readonly Class PeopleList = new() { ... };
    // → .mw-sp-people-list
    //    ^^  ^^  ^^^^^^^^^^^
    //    │   │   └─ field name (PascalCase → kebab-case)
    //    │   └─ stylesheet prefix [Prefix("sp")]
    //    └─ global prefix (from Program.cs)

    public static readonly Class DayView = new() { ... };
    // → .mw-sp-day-view
}

// Stylesheet WITHOUT its own prefix:
public static partial class AppStyles : StyleSheet
{
    public static readonly Class DialogFrameless = new() { ... };
    // → .mw-dialog-frameless (no stylesheet prefix, just global)
}
```

In Razor, the prefix is transparent — the developer never writes it:
```razor
<div class="@ShiftPlannerStyles.PeopleList">
@* renders: class="mw-sp-people-list" *@
```

---

### 21. File Convention

**Status: DECIDED**

Stylesheet files use the `.css.cs` extension. Follows .NET compound extension conventions (`.razor.cs`, `.razor.css`, `.g.cs`, `.Designer.cs`).

```
MitWare.Blazor/
  Styles/
    AppStyles.css.cs
    ShiftPlannerStyles.css.cs
    DataTableStyles.css.cs
```

The source generator does NOT use the extension for discovery — it finds stylesheets by `: StyleSheet` inheritance in the syntax tree. The extension is a human convention for project organization.

---

### 22. Conditional Classes

**Status: DECIDED**

Two patterns for conditionally applying classes in Razor:

```csharp
// Ternary with Class.None:
<div class="@(isActive ? Active : Class.None)">

// Fluent .When():
<div class="@Active.When(isActive)">
```

Implementation:
```csharp
public readonly struct Class {
    public static readonly Class None = default;  // implicit string → ""
    public string When(bool condition) => condition ? Name : "";
}
```

---

### 23. Class Variants (BEM-style modifiers)

**Status: DECIDED**

`.Variant(slug)` appends a BEM modifier to the class name. Base is compile-time, suffix can be runtime.

```csharp
// In stylesheet:
public static readonly Class KanbanHeader = new() {
    FontWeight = FontWeight.Bold,

    // Known variants styled at compile time:
    [Self.Variant("backlog")] = new() { Background = Color.Gray(90) },
    [Self.Variant("active")]  = new() { Background = Color.Hex("#dbeafe") },
    [Self.Variant("done")]    = new() { Background = Color.Hex("#dcfce7") },
};

// In Razor — dynamic variant from data:
<div class="@(KanbanHeader + KanbanHeader.Variant(col.Slug))">
@* → class="mw-dt-kanban-header mw-dt-kanban-header--todo" *@
```

Implementation:
```csharp
public string Variant(string slug) => Name + "--" + slug;
```

SCSS output:
```scss
.mw-dt-kanban-header {
    font-weight: bold;
    &--backlog { background: #e5e5e5; }
    &--active  { background: #dbeafe; }
    &--done    { background: #dcfce7; }
}
```

---

### 24. `@font-face`

**Status: DECIDED**

```csharp
public static readonly FontFace Inter = new() {
    Family = "Inter",
    Src = Css.Url("fonts/Inter.woff2"),
    Weight = FontWeight.Range(400, 700),
    Display = FontDisplay.Swap,
};
```

---

### 25. `@supports` (Feature Queries)

**Status: DECIDED**

Used as nesting indexer keys, same pattern as `@media`:

```csharp
public static readonly Class Layout = new() {
    Display = Display.Flex,  // fallback

    [Supports.Grid] = new() {
        Display = Display.Grid,
    },
};
```

---

### 26. Explicitly NOT Supported

Opinionated omissions to prevent bad CSS practices:

| CSS Feature | Why Not |
|---|---|
| **ID selectors for styling** (`#header { }`) | Specificity bombs. Use classes. IDs remain for HTML linking/JS, not styling. |
| **Component scoped styles** (Blazor CSS isolation) | `::deep` is fake CSS. Prefix scoping is strictly better — same isolation, zero magic, no cascading gotchas. |
| **`::deep` / `>>>`** | Non-standard, unpredictable cascading across component boundaries. |
| **`@import`** | Use C# `using` and type references instead. |
| **Tailwind integration** | Competing paradigm. Tailwind = "compose utility classes in HTML." Ours = "write CSS rules in C# with type safety." Using both helps neither. |

---

### 27. Source Maps

**Status: OPEN**

Chain: C# → SCSS map (our emitter tracks line numbers) → SCSS → CSS map (sass generates). Browser devtools could show C# source.

---

### 28. Source Generator DX Features

**Status: PLANNED**

The source generator is not just a code emitter — it's the primary DX engine for the CSS-in-C# system.

#### Scaffold template

Creating a `.css.cs` file and writing `StyleSheet` triggers a code fix:
```
💡 Generate stylesheet scaffold
```
Expands to a complete stylesheet template with sections for variables, rules, classes, keyframes.

#### CSS preview

The source generator emits the compiled CSS as comments, visible on hover/go-to-definition:
```csharp
// CSS: .mw-dt-card { background: white; border-radius: 8px; }
// CSS: .mw-dt-card:hover { box-shadow: 0 4px 16px rgba(0,0,0,0.15); }
public static readonly Class Card = new() { ... };
```

#### Diagnostics

| Diagnostic | Description |
|---|---|
| Dead class detection | `Class 'X' is defined but never referenced in any .razor file` |
| CssVar unset | `CssVar 'Accent' is referenced but never assigned a value` |
| CssVar type mismatch | `CssVar<Length> used where CssVar<Color> expected` |
| Selector validation | `Pseudo-element must be last: 'Card.After.Hover' → 'Card.Hover.After'` |
| Prefix collision | `AppStyles.Card and DialogStyles.Card both resolve to '.mw-card'` |
| Invalid color | `Color.Hex("#ggg") is not a valid hex color` |

#### CSS-to-C# paste converter

Paste CSS into a `.css.cs` file, get a code fix:
```
💡 Convert CSS to C# stylesheet syntax
```
Converts pasted CSS rules into typed `Class` and `Rule` declarations.

#### Extract-to-CssVar refactoring

Select a value used in multiple classes:
```
💡 Extract to CssVar<Color> — used in 5 declarations
```
Creates a `CssVar<T>` field and replaces all usages.

---

### Summary

| # | Concept | Status |
|---|---------|--------|
| 1 | CSS Values | **Decided** |
| 2 | Class vs Rule Distinction | **Decided** — Class for Razor, Rule for anonymous CSS, discovery by type |
| 3 | Selector Operators | **Decided** |
| 4 | Pseudo-classes | **Decided** — properties on Selector |
| 5 | Nesting | **Decided** — recursive indexer |
| 6 | Selector Lists | **Decided** — params |
| 7 | Keyframes | **Decided** |
| 8 | Media Queries | **Decided** |
| 9 | Element Selectors | **Decided** |
| 10 | External Classes | **Decided** — auto-generated from framework CSS via source gen |
| 11 | ClassList | **Decided** — `+` operator, zero-alloc struct, string escape hatch |
| 12 | Build Pipeline | **Decided** — C# → SCSS → CSS |
| 13 | Asset Source Generator | **Decided** (not implemented) |
| 14 | !important | **Decided** — `.Important` property |
| 15 | Attribute Selectors | **Decided** — typed + `Aria` + `Data()` + string fallback |
| 16 | CSS Custom Properties | **Decided** — self-contained `CssVar<T>` with default + overrides |
| 17 | CSS Functions | **Partially decided** |
| 18 | Value Shorthands | **Open** |
| 19 | Self Keyword | **Mostly decided** |
| 20 | Prefixing | **Decided** — Program.cs global + attribute per-sheet |
| 21 | File Convention | **Decided** — `.css.cs` extension |
| 22 | Conditional Classes | **Decided** — `.When()` + ternary with `Class.None` |
| 23 | Class Variants | **Decided** — `.Variant(slug)` BEM modifiers |
| 24 | @font-face | **Decided** |
| 25 | @supports | **Decided** |
| 26 | Explicitly Not Supported | **Decided** — IDs, scoped styles, ::deep, @import, Tailwind |
| 27 | Source Maps | **Open** |
| 28 | Source Generator DX | **Planned** — scaffold, preview, diagnostics, converter, refactoring |
