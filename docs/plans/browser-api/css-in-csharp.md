# CSS-in-C# API Design

**Parent:** [css.md](css.md)

This is the complete design spec for expressing CSS in C#. It was designed across multiple sessions with deep discussion of trade-offs, edge cases, and real-world validation against MitWare's `app.css` (800+ lines of production CSS).

---

## Philosophy

These principles drove every decision. When something doesn't work during implementation, resolve it in the spirit of these principles — don't just patch around it.

### 1. Zero string literals in user-facing APIs

Every CSS class name, selector, attribute, property, and value should be a typed C# identifier with IntelliSense. String literals are invisible to the compiler — typos become silent 404s or broken styles. The type system catches errors at compile time. String escape hatches exist but are the exception, not the norm.

### 2. Zero runtime cost for static styles

CSS stylesheets are static artifacts. The C# code that defines them runs at build time (via CssCompiler MSBuild target). The output is a plain `.css` file served as a static asset. No per-request computation. The only "runtime" cost is the browser evaluating standard CSS (which it does for any stylesheet). Class name references in Razor (`@Card`) are pre-computed string reads — equivalent to a `const`.

### 3. C# IS the preprocessor

C# replaces SCSS as the language developers write. SCSS remains in the build pipeline as an intermediate format (C# → SCSS → CSS via sass). Every SCSS feature (variables, mixins, nesting, loops, conditionals, color functions) is replaced by a native C# equivalent that is strictly more powerful. Developers never see or write SCSS.

### 4. Types ARE the API documentation

The property type tells you what values are valid. `FontSize = Length.` shows every valid length expression in IntelliSense. `FontSize = Color.Red` doesn't compile. Wrong types are compile errors, not runtime surprises. Property-specific types (like `FontSize` accepting both `Length` and keywords like `FontSize.Large`) are generated from the CSS spec data.

### 5. The method does the right thing

When a method has two valid implementation strategies (e.g., SCSS build-time vs CSS runtime for color functions), it automatically picks the correct one based on the input. This isn't hidden magic — it's the only correct behavior per case. SCSS can't process CSS variables; CSS relative color syntax can't be pre-computed. The user writes `.Lighten(20)` and gets the right output regardless of whether the color is a literal or a variable.

### 6. No magic names, no magic conventions

Discovery is by TYPE, not by field name. The source generator finds `Class`, `Rule`, `Rules`, `CssVar<T>`, `Keyframes`, `FontFace` fields — never by naming convention. Field names are for human readability. No Blazor-style `ChildContent`/`PropertyNameChanged` magic. The one exception: `StyleSheet` as a base/marker for the source generator to find stylesheet classes.

### 7. Explicit over implicit (with pragmatic exceptions)

Prefer explicit APIs. But when explicitness leads to bugs (like forgetting to use `color-mix()` for CSS variables), the method should do the right thing automatically. The test: would a reasonable developer expect the explicit version to "just work"? If yes, make it work. If the behavior difference matters, make it explicit.

### 8. Break from CSS convention when it prevents errors

We deliberately don't support ID selectors for styling (specificity bombs), Blazor CSS isolation (fake CSS with cascading gotchas), the 3-value sides shorthand (confusing order), and Tailwind integration (competing paradigm). These are opinionated omissions. If an implementer encounters something CSS supports but this spec doesn't mention, check whether it falls into the "prevents bad practices" category before adding it.

---

## CSS Structural Reference

```
Stylesheet
├── Rule (selector + declaration block)
│   ├── Selector ─── "which elements?"
│   │   ├── Simple selectors (atomic)
│   │   │   ├── Type selector        div, p, h1
│   │   │   ├── Class selector       .card, .active
│   │   │   ├── ID selector          #header (NOT for styling — see §26)
│   │   │   ├── Universal selector   *
│   │   │   └── Attribute selector   [href], [type="text"]
│   │   ├── Pseudo-class             :hover, :focus, :nth-child(2)
│   │   │   (always attached — modifies "when/which state")
│   │   ├── Pseudo-element           ::before, ::after
│   │   │   (always LAST in compound — selects a sub-part)
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
│   ├── @font-face { declarations }
│   ├── @container (query) { rules }
│   └── @property --name { syntax, inherits, initial-value }
└── Custom Properties (CSS Variables)
    ├── Definition       --name: value    (set on any selector, inherits to descendants)
    ├── Usage            var(--name)      (reads the inherited value)
    └── Fallback         var(--name, fallback-value)
```

---

## Build Pipeline

```
┌─────────────┐     ┌──────────┐     ┌──────────┐     ┌──────────────┐
│ .css.cs      │     │ Source   │     │ .scss    │     │ .css          │
│ (you write)  │────▶│ Gen +   │────▶│ (plumbing│────▶│ (static asset)│
│              │     │ Emitter  │     │ you never│     │ in wwwroot/   │
│              │     │          │     │ see)     │     │               │
└─────────────┘     └──────────┘     └──────────┘     └──────────────┘
       │                  │                │                  │
   C# 14 code      Roslyn source     sass compiler     Browser loads
   with types      generator reads    computes SCSS     and caches
   and operators   syntax trees,      functions,
                   emits name         resolves nesting
                   mappings +
                   SCSS output
```

One `StyleSheet` class → one `.scss` file → one `.css` file. File name derived from class name (`AppStyles` → `app-styles.css`).

MSBuild target runs before build:
```xml
<Target Name="CompileCssStyles" BeforeTargets="Build">
    <Exec Command="dotnet run --project tools/CssCompiler -- --output wwwroot/css/" />
</Target>
```

---

## Decision Tracker

### 1. CSS Values

**Status: DECIDED**

Readonly structs implementing `ICssValue` with `ToCss()`. C# 14 extension properties on `int`/`double` provide parentheses-free unit syntax.

```csharp
16.Px           // Length
1.5.Rem         // Length
Color.Hex("#fff")
45.Deg          // Angle
200.Ms          // Duration
50.Percent      // Percentage
1.Fr            // Flex
```

**Intent:** Values read like CSS. `16.Px` not `Length.Px(16)` or `16.Px()`. The extension properties need to be in a namespace that's only imported in `.css.cs` files to avoid polluting IntelliSense globally.

**Pending change:** Rename `CssColor` → `Color` (namespace handles disambiguation with `System.Drawing.Color`).

**Implementation note:** If C# 14 extension properties on `int`/`double` don't work as expected, fall back to extension methods with `()`. The API shape matters more than the parentheses. `Length.Px(16)` static factories must always remain as an alternative.

---

### 2. Class vs Rule

**Status: DECIDED**

Two types, split by HOW THEY'RE USED in C# — not by what they are in CSS (in CSS, everything is a rule).

- **`Class`** — referenced in Razor markup. Has: name (from field), implicit string conversion, `.When()`, `.Variant()`, `+` operator for ClassList.
- **`Rule`** — never referenced in markup. Just exists in the stylesheet. For `:root` variables, element resets, complex selectors.
- **`Rules`** — collection type for grouping anonymous rules.

```csharp
// Class — you reference it in Razor: <div class="@Card">
public static readonly Class Card = new() {
    Background = Color.White,
};

// Rule — just CSS, never in markup
public static readonly Rule ResetBody = new(El.Body) {
    Margin = 0.Px,
};

// Rules — grouped anonymous rules
public static readonly Rules Reset = [
    new(El.All) { BoxSizing = BoxSizing.BorderBox },
    new(El.Body) { Margin = 0.Px },
];
```

**Intent:** `Class` serves double duty — it's a CSS rule AND a Razor identifier. `Rule` is CSS-only. The split exists because Razor needs a string value from `Class` (the class name), while `Rule` has no meaningful string representation.

**Discovery by type:** The source generator collects ALL fields by their C# type (`Class`, `Rule`, `Rules`, `CssVar<T>`, `Keyframes`, `FontFace`). Field names are for human readability — never magic conventions. Multiple `Rules` fields named anything are all collected.

**`.Selector` property:** `Class` implicit string conversion returns the name without dot (`"card"` for `class=""`). `.Selector` returns with dot (`".card"` for CSS selector contexts). Prefer updating consuming component parameters to accept `Class` directly.

**Razor reference style:** `<div class="@DndTestStyles.Card">` — prefer explicit stylesheet reference for discoverability over `using static`.

**Implementation note:** `Class` and `Rule` likely share a common base for CSS declarations (all CSS properties as init-only properties). `Class` adds the name/string conversion. `Rule` adds the selector constructor.

---

### 3. Selector Operators

**Status: DECIDED**

CSS combinators map to C# operators. The choice of operators was driven by C# precedence matching CSS specificity — compound binds tightest, selector list loosest.

| CSS | Meaning | C# Operator | Fluent Method | C# Precedence |
|-----|---------|-------------|---------------|------------|
| `.a:hover` | pseudo-class | `A.Hover` | `A.On(Hover)` | — (property) |
| `.a.b` | compound | `A * B` | `A.And(B)` | 3 (highest binary) |
| `.a + .b` | adjacent sibling | `A + B` | `A.Adjacent(B)` | 4 |
| `.a ~ .b` | general sibling | `A - B` | `A.Sibling(B)` | 4 |
| `.a .b` | descendant | `A >> B` | `A.Descendant(B)` | 5 |
| `.a > .b` | child | `A > B` | `A.Child(B)` | 6 |
| `.a, .b` | selector list | params / `\|` | `A.Or(B)` | 10 (lowest) |

**Why these specific operators:**
- `*` for compound: `&` was the semantic fit but has LOWER precedence than `>` in C#, causing `A & B > C` to parse as `A & (B > C)` — wrong. `*` has precedence 3, binding tighter than all combinators.
- `-` for general sibling: CSS uses `~` but it's unary-only in C#. `-` pairs visually with `+` (adjacent). Mnemonic: `+` = tight (directly next), `-` = loose (anywhere after).
- `>>` for descendant: "going deeper." Higher precedence than `>` (child), so `A >> B > C` naturally parses as `(A >> B) > C`.

**Operator pairing requirement:** C# mandates `>` and `<` declared together, `>>` and `<<` together. The unused pair (`<`, `<<`) throws `NotSupportedException` with a message explaining there's no CSS equivalent.

**Both operator and fluent APIs:** All operators delegate to fluent methods. Both return `Selector`. They can be mixed freely in one expression. All are defined on `Selector`; `Class` has implicit conversion to `Selector`.

**Implementation note:** The fluent methods ARE the implementation. Operators call them. Every operator/method returns `Selector`, enabling unlimited chaining. The `Selector` struct internally accumulates parts as a string or list.

---

### 4. Pseudo-classes

**Status: DECIDED**

Properties on `Selector`, not a separate type. This mirrors CSS where pseudo-classes always attach to a selector.

```csharp
Card.Hover              // .card:hover
Card.Focus              // .card:focus
El.Input.Disabled       // input:disabled
Card.Hover.After        // .card:hover::after

// Functional pseudo-classes:
Card.NthChild(2)        // .card:nth-child(2)
Card.Not(Disabled)      // .card:not(.disabled)
Card.Has(Title)         // .card:has(.title)
```

**Intent:** `.Hover` is a MODIFIER of `Card`, not an independent thing. It returns a new `Selector`. Chaining works: `Card.Hover.After` = `.card:hover::after`. Pseudo-elements (`.Before`, `.After`, `.Placeholder`) are also properties — they should ideally be terminal (no further pseudo-class properties after them) but enforcing this at the type level may be impractical. The SCSS compiler will catch invalid ordering.

**For nesting:** Inside object initializers there's no instance, so `Self.Hover` is used. See §19.

---

### 5. Nesting

**Status: DECIDED**

Recursive indexer on the declarations type. The SAME indexer that accepts `Selector` for nesting also exists on the nested `Declarations` type, enabling unlimited depth.

```csharp
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

    [Media.MaxWidth(768.Px)] = new() {        // @media
        Padding = 4.Px,
    },

    [Container.MinWidth(400.Px)] = new() {    // @container
        Display = Display.Grid,
    },

    [Supports.Grid] = new() {                 // @supports
        Display = Display.Grid,
    },
};
```

**Intent:** Everything related to `.card` lives INSIDE `Card`. No separate `CardHovered`, `CardTitle` fields. This mirrors SCSS nesting. The indexer `[]` is the universal "attach something to this rule" mechanism — it works for pseudo-classes, selectors, media queries, container queries, feature queries, and CSS variable overrides.

**The `Declarations` type** needs: all CSS properties as `init`-only setters, the same `Selector` indexer (for nesting), and the `MediaQuery`/`ContainerQuery`/`CssVar<T>` indexers.

**`Self`** always means `&` (SCSS parent reference). At each nesting level, `&` resolves to the accumulated selector. C# doesn't track this — SCSS handles `&` resolution at compile time.

---

### 6. Selector Lists

**Status: DECIDED**

`params` in Rule constructor and nesting indexer. Confirmed: C# 13+ `params` works in indexer setters within object initializers.

```csharp
// params constructor:
new Rule(Card.Hover, Panel.Hover, Btn.Focus) { ... }

// params indexer in nesting:
[Self.Hover, Self.FocusWithin] = new() { ... }

// | operator for inline composition:
var both = Card.Hover | Panel.Hover;
```

---

### 7. Keyframes

**Status: DECIDED**

Indexer initializer with `Percentage` keys. `From`/`To` are `Percentage` constants (0%, 100%), source-gen-injected into every stylesheet alongside `Self`.

```csharp
public static readonly Keyframes FadeIn = new() {
    [From] = new() { Opacity = 0 },
    [50.Percent] = new() { Opacity = 0.5 },
    [To] = new() { Opacity = 1 },
};
```

**Field name** → keyframe animation name (PascalCase → kebab-case): `FadeIn` → `fade-in`. Referenced by typed identifier in animation declarations, not by string.

---

### 8. Media Queries

**Status: DECIDED**

Typed media features. Used as nesting indexer keys (same `[]` mechanism as pseudo-classes and selectors).

```csharp
Media.MaxWidth(768.Px)
Media.MinWidth(1024.Px)
Media.PrefersDark
Media.PrefersReducedMotion
Media.Print

// Combined:
Media.MinWidth(768.Px) & Media.MaxWidth(1024.Px)
```

---

### 9. Element Selectors

**Status: DECIDED**

Pre-defined `Selector` instances in `El` static class. Generated from HTML spec (same spec source as DOM types).

```csharp
El.Div, El.P, El.H1, El.Ul, El.Li, El.A, El.Input, El.Span, El.Table, ...

El.Li * Active > El.Span   // li.active > span
```

**`El.Root`** represents `:root` (for CSS variable definitions).
**`El.All`** represents `*` (universal selector).

---

### 10. External Classes

**Status: DECIDED**

Auto-generated from framework CSS files by the source generator. Not hand-written. The source gen reads CSS from NuGet packages via `<ExternalCss>` MSBuild items.

```xml
<ItemGroup>
    <ExternalCss Include="$(PkgMudBlazor)/staticwebassets/**/*.css"
                 RootClass="Mud" />
</ItemGroup>
```

The generator extracts `.mud-*` class selectors and `--mud-*` custom properties, groups by dash-separated segments, emits nested static classes:

```csharp
Mud.Palette.Primary          // CssVar<Color> → var(--mud-palette-primary)
Mud.Table.Cell               // Class → .mud-table-cell
Mud.Dialog.Container         // Class → .mud-dialog-container
```

**Intent:** Type `Mud.` → IntelliSense shows every MudBlazor class and variable, organized by component. Update MudBlazor NuGet → types regenerate. Zero manual maintenance.

**Manual fallback:** `Class.External("weird-thing")` for edge cases the parser can't handle.

**Implementation note:** The CSS parser for external files doesn't need to be comprehensive — just extract selector class names and `:root` custom properties. It's not a full CSS parser.

---

### 11. ClassList

**Status: DECIDED**

`+` operator on `Class` returns `ClassList` struct. Zero heap allocation for 1-4 classes (inline struct fields). String escape hatch for raw class names.

```csharp
<div class="@(Card + Active + Round)">       // typed
<div class="@(Card + "some-external-class")"> // string escape hatch
```

**Implementation:** `ClassList` stores up to 4 `Class` references inline (no array). Overflow allocates. Implicit string conversion joins with spaces. `Css.Classes(params Class[])` exists for `IEnumerable` cases.

**`Class + string` operator:** returns `ClassList` that includes the raw string. Needed for classes from frameworks without typed references.

---

### 12. Build Pipeline

**Status: DECIDED**

```
C# StyleSheet → .scss file → sass compiler → .css file → wwwroot/
```

One `StyleSheet` class = one `.css` file. MSBuild target before build. C# is authoring, SCSS is plumbing, CSS is output.

---

### 13. Asset Source Generator

**Status: DECIDED (not yet implemented)**

Roslyn incremental source gen scans `wwwroot/` via `AdditionalFiles`. Emits typed `Assets` class. File rename/delete/add triggers regen in IDE.

```csharp
Assets.Css.App          // "css/app.css"
Assets.Images.Logo      // "images/logo.svg"
```

**Code fix provider (future):** When an asset is renamed/deleted and a reference breaks, suggest the closest new match.

---

### 14. `!important`

**Status: DECIDED**

`.Important` property on each value type. Returns a copy with a boolean flag set. No parentheses.

```csharp
Padding = 0.Px.Important
Display = Display.None.Important
```

Internals:
```csharp
public readonly struct Length : ICssValue {
    private readonly string _value;
    private readonly bool _important;
    public Length Important => new(_value, important: true);
    public string ToCss() => _important ? _value + " !important" : _value;
}
```

**Open sub-issue:** Enum-based CSS keyword types (like `Display`) can't have properties. Options: make them structs instead of enums, or use C# 14 extension properties. Resolve during implementation — structs with static fields mimicking enum members is the likely path (same as `Length`).

---

### 15. Attribute Selectors

**Status: DECIDED**

Five tiers, all returning `AttrSelector` with fluent matchers (`.Equals()`, `.Contains()`, `.StartsWith()`, `.EndsWith()`, `.HasWord()`, `.DashMatch()`).

```csharp
// Tier 1: Standard HTML attributes (generated from HTML spec)
Attr.Type.Equals("text")          // [type="text"]
Attr.Href.StartsWith("https")    // [href^="https"]
Attr.Disabled                     // [disabled]

// Tier 2: ARIA role
Attr.Role.Equals("button")       // [role="button"]

// Tier 3: ARIA attributes (generated from WAI-ARIA spec)
Attr.Aria.Label                   // [aria-label]
Attr.Aria.Hidden.Equals("true")  // [aria-hidden="true"]

// Tier 4: data-* attributes (typed helper, prepends "data-")
Attr.Data("stick-value").Equals("0")  // [data-stick-value="0"]

// Tier 5: Escape hatch (any attribute, raw string)
Attr("potato", "yes")            // [potato="yes"]
```

**Unified with `attr()` function:** Same `Attr` reference works in selector context (attribute selector) and value context (`content: attr(data-label)`) via implicit conversion. Context determines which CSS feature is emitted.

---

### 16. CSS Custom Properties

**Status: DECIDED**

Self-contained `CssVar<T>`. Default in constructor → auto-emits in `:root`. Object initializer for conditional overrides. Name from C# field name (PascalCase → `--kebab-case`).

```csharp
// Simple:
public static readonly CssVar<Length> Radius = new(8.Px);

// With overrides:
public static readonly CssVar<Color> Bg = new(Color.White) {
    [Media.PrefersDark] = Color.Hex("#0a0a0a"),
};

// External (MudBlazor owns it):
public static readonly CssVar<Color> MudPrimary = CssVar.External("--mud-palette-primary");

// Usage — just reference by name:
Background = Bg,         // → var(--bg)
[Radius] = 12.Px,        // scoped override: --radius: 12px
```

**Intent:** Everything about a variable is in ONE place — declaration, default, overrides. No hunting for where the value is set. C# variables (`static readonly`) are compile-time constants (SCSS `$var`). CSS custom properties (`CssVar<T>`) are runtime-reactive browser variables.

**Fallback values:** `.Or(fallback)` for `var(--name, fallback)`. Nesting reads inside-out:
```csharp
Primary.Or(Color.Blue)                    // var(--primary, blue)
Brand.Or(Primary.Or(Color.Blue))          // var(--brand, var(--primary, blue))
```
`.Or()` returns `T` (not `CssVar<T>`), preventing accidental chaining — forces correct nesting.

---

### 17. CSS Functions

**Status: DECIDED**

Functions that return a CSS value type live as static methods on that type. Functions that don't map to a single type live on `Css`. The guiding principle: type `PropertyName = TypeName.` and IntelliSense shows ALL valid expressions.

| CSS Function | C# API | Lives on |
|---|---|---|
| `calc()` | Length operators (`+`, `-`) | `Length` |
| `var()` | `CssVar<T>` reference | — (§16) |
| `rgb()/rgba()` | `Color.Rgb()` / `Color.Rgba()` | `Color` |
| `hsl()/hsla()` | `Color.Hsl()` / `Color.Hsla()` | `Color` |
| `linear-gradient()` | `Gradient.Linear()` | `Gradient` |
| `clamp()` | `Length.Clamp(min, pref, max)` | `Length` |
| `min()/max()` | `Length.Min(a, b)` / `Length.Max(a, b)` | `Length` |
| `fit-content()` | `Length.FitContent` / `Length.FitContent(max)` | `Length` |
| `repeat()` | `GridTemplate.Repeat(n, tracks)` | `GridTemplate` |
| `minmax()` | `GridTemplate.MinMax(min, max)` | `GridTemplate` |
| `env()` | `Css.Env.SafeAreaInsetTop` etc. | `Css.Env` |
| `url()` | `Css.Url(path)` / `Css.Url(asset)` / `Css.Url(mime, base64)` | `Css` |
| `attr()` | `Attr.Data("label")` in value context | `Attr` (unified, §15) |

**`url()` overloads:** String path, typed asset reference (compile-checked!), data URI with typed `Mime` constants (`Mime.Svg`, `Mime.Png`, etc.).

**`GridTemplate`** — type of `GridTemplateColumns`/`GridTemplateRows`. Has `Repeat`, `MinMax`, `Of`, `AutoFill`, `AutoFit`, `None`. Implicit conversions from `Length` and `Flex` for simple cases. Full type chain documented:
- `GridTemplate.Repeat(3, 1.Fr)` → `repeat(3, 1fr)`
- `GridTemplate.Repeat(GridTemplate.AutoFill, GridTemplate.MinMax(200.Px, 1.Fr))` → `repeat(auto-fill, minmax(200px, 1fr))`
- `GridTemplateColumns = 1.Fr` → implicit Flex → GridTemplate

**Percentage → Length implicit conversion:** CSS treats percentages as lengths in most contexts. `Length.Clamp(1.Rem, 50.Percent, 30.Rem)` works via implicit conversion.

---

### 18. Value Shorthands

**Status: DECIDED**

**`Sides` type** for `Padding`, `Margin`, etc. Implicit conversions from `Length` (all sides), 2-tuple (vertical, horizontal), and 4-tuple (top, right, bottom, left). 3-value form deliberately NOT supported — it puts "horizontal" in the middle position which is confusing.

```csharp
Padding = 10.Px,                                                     // all
Padding = (10.Px, 20.Px),                                           // vertical, horizontal
Padding = (top: 10.Px, right: 20.Px, bottom: 30.Px, left: 40.Px),  // named
Padding = Css.Sides(top: 10.Px, right: 20.Px, bottom: 30.Px, left: 40.Px),
PaddingTop = 10.Px,  // individual always available
```

**Border/Outline:** `Border.Solid(1.Px, Color.Black)`, `Border.None`, etc.

**Property-specific types:** Properties accepting lengths AND keywords get generated types with implicit Length conversion: `FontSize = 16.Px` works, `FontSize = FontSize.Large` works, `FontSize = Color.Red` doesn't compile.

**Duration + TimeSpan:** `Duration` accepts `TimeSpan` via implicit conversion for interop.

**Analyzer BCA001:** Warns on 4-value `Sides` without named parameters. Default severity: warning. Configurable via `.editorconfig` or Program.cs `AddBrowserApiCss(options => { options.Analyzers.NamedParamsForSides = Severity.Error })`. Program.cs overrides `.editorconfig`.

---

### 19. Self Keyword

**Status: DECIDED**

`Self` is a `Selector` with value `&`. Available via two mechanisms:

1. **Source generator injects** `Self`, `From`, `To` into every `StyleSheet` partial class. Zero imports inside stylesheets.
2. **Also on `Css`** for use outside stylesheets (tests, shared code, libraries).

```csharp
[Self.Hover] = new() { ... },              // &:hover
[Self > Title] = new() { ... },            // & > .title
[Self.Variant("active")] = new() { ... },  // &--active
[From] = new() { Opacity = 0 },            // 0% (keyframe)
[To] = new() { Opacity = 1 },              // 100% (keyframe)
```

**`Self`** has all pseudo-class properties, all combinator operators, and `.Variant()`. It's just a `Selector` — nothing special about the type, only its value.

---

### 20. Prefixing

**Status: DECIDED**

Two levels: global (project-wide) and per-stylesheet. Chain: `{global}-{stylesheet}-{classname}`.

**Global prefix** — read from `Program.cs` by source generator (AST pattern matching, not runtime execution). The `AddBrowserApiCss()` call is ALSO a real runtime method registering Blazor services — one line, two purposes.

```csharp
builder.Services.AddBrowserApiCss(options => {
    options.GlobalPrefix = "mw";
});
```

**Per-stylesheet prefix** — attribute:
```csharp
[Prefix("sp")]
public static partial class ShiftPlannerStyles : StyleSheet {
    public static readonly Class PeopleList = new() { ... };
    // → .mw-sp-people-list
}
```

Prefix is transparent in Razor — the developer never writes it.

**Implementation note:** The source generator must pattern-match on the `AddBrowserApiCss` lambda in Program.cs syntax tree. The `GlobalPrefix` value MUST be a string literal or const — if it's a variable, emit diagnostic warning. This is the same technique ASP.NET Minimal API source generators use.

---

### 21. File Convention

**Status: DECIDED**

`.css.cs` extension. Human convention, not source-gen dependency. Follows `.razor.cs`, `.g.cs` patterns.

---

### 22. Conditional Classes

**Status: DECIDED**

```csharp
<div class="@(isActive ? Active : Class.None)">
<div class="@Active.When(isActive)">
```

`Class.None` = `default` struct, implicit string → `""`. `.When(bool)` returns name or empty string.

---

### 23. Class Variants

**Status: DECIDED**

`.Variant(slug)` for BEM-style modifiers. Base class is compile-time, variant suffix can be runtime.

```csharp
// In stylesheet — known variants at compile time:
[Self.Variant("backlog")] = new() { Background = Color.Gray(90) },

// In Razor — dynamic variant from data:
<div class="@(KanbanHeader + KanbanHeader.Variant(col.Slug))">
```

Implementation: `Name + "--" + slug`. SCSS output: `&--backlog { ... }`.

**When to use variants vs compound classes:** Variants for data-driven modifiers tied to the parent (column types, states meaningful only in context). Compound classes (`Card * Active`) for reusable states (active, selected, disabled) that apply across components.

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

### 25. `@supports`

**Status: DECIDED**

Same nesting indexer as `@media`:
```csharp
[Supports.Grid] = new() { Display = Display.Grid },
```

---

### 26. Explicitly NOT Supported

| CSS Feature | Why Not |
|---|---|
| **ID selectors for styling** | Specificity bombs. IDs for HTML linking/JS only. |
| **Blazor CSS isolation** | `::deep` is fake CSS. Prefix scoping is strictly better. |
| **`::deep` / `>>>`** | Non-standard, cascading gotchas. |
| **`@import`** | Use C# `using` and type references. |
| **Tailwind** | Competing paradigm. |
| **3-value sides shorthand** | Confusing order (horizontal in middle). |

---

### 27. Source Maps

**Status: DECIDED**

All three layers: C# → SCSS → CSS. Full transparency.

1. Our emitter tracks C# file/line (from `SyntaxNode.GetLocation()`) as it writes SCSS, embeds as comments, generates SCSS→C# source map.
2. Sass generates CSS→SCSS map automatically.
3. We chain the two into a CSS→C# source map.

Result: browser devtools links to C# source. Sources panel shows all three files.

---

### 28. Source Generator DX

**Status: PLANNED**

- **Scaffold:** Code fix on `StyleSheet` → generates template
- **CSS preview:** Generated comments showing compiled CSS on hover
- **Diagnostics:** Dead class, unset CssVar, type mismatch, selector validation, prefix collision, invalid color
- **CSS-to-C# converter:** Paste CSS → code fix converts to typed declarations
- **Extract-to-CssVar:** Select repeated value → refactor to variable
- **Container analyzer:** Warn if `Container.MinWidth()` used without `ContainerType` ancestor

---

### 29. Color Functions

**Status: DECIDED**

All SCSS color functions supported. Auto-dispatch: literal → SCSS function (sass computes, clean output), CSS variable → relative color syntax `hsl(from ...)` or `color-mix()` (browser computes, reactive).

**This is not hidden dispatch.** It's the only correct behavior per case: SCSS can't process variables (compile error), CSS relative syntax can't be pre-computed (no value to compute). Each path is the ONLY one that works for its input.

```csharp
color.Lighten(20)      // literal → lighten(#x, 20%)     | var → hsl(from var(--x) h s calc(l + 20%))
color.Darken(20)       // literal → darken(#x, 20%)      | var → hsl(from var(--x) h s calc(l - 20%))
color.Saturate(30)     // literal → saturate(#x, 30%)    | var → hsl(from var(--x) h calc(s + 30%) l)
color.Desaturate(30)   // literal → desaturate(#x, 30%)  | var → hsl(from var(--x) h calc(s - 30%) l)
color.AdjustHue(30)    // literal → adjust-hue(#x, 30deg)| var → hsl(from var(--x) calc(h + 30deg) s l)
color.Complement       // literal → complement(#x)       | var → hsl(from var(--x) calc(h + 180deg) s l)
color.Grayscale        // literal → grayscale(#x)        | var → hsl(from var(--x) h 0% l)
color.WithAlpha(0.5)   // literal → rgba(#x, 0.5)        | var → hsl(from var(--x) h s l / 0.5)
color.Mix(other, 50%)  // literal → mix(#x, y, 50%)      | var → color-mix(in srgb, var(--x) 50%, y)
color.Invert           // literal → invert(#x)           | var → hsl(from var(--x) calc(h+180) s calc(100%-l))
```

**Why dispatch instead of always CSS:** Literal colors produce cleaner CSS output (`#5dade2` vs `hsl(from #3498db h s calc(l + 20%))`). Easier to read in devtools. Negligible but non-zero runtime cost avoided.

**Testing:** SCSS output serves as test oracle. If C# color math is ever added, verify against sass.

**Implementation note:** The `Color` struct stores a string and an `_isVariable` flag. All methods are string concatenation — no color math in C#. The string is either an SCSS function call or a CSS function call, determined by the flag.

---

### 30. `@property`

**Status: DECIDED**

Auto-generated from `CssVar<T>`. Source gen infers `syntax` from `T`:

| C# type | CSS syntax |
|---|---|
| `CssVar<Color>` | `"<color>"` |
| `CssVar<Length>` | `"<length>"` |
| `CssVar<Percentage>` | `"<percentage>"` |
| `CssVar<Angle>` | `"<angle>"` |

Zero effort for user. Optional `Inherits = false` override.

**Why:** `@property` enables browser-side type checking and animation of custom properties. Without it, `transition: --primary 0.3s` doesn't work because the browser doesn't know `--primary` is a color.

---

### 31. `var()` Fallback Values

**Status: DECIDED**

`.Or()` with inside-out nesting. Type-safe: `.Or()` returns `T` (not `CssVar<T>`), preventing misuse.

```csharp
Primary.Or(Color.Blue)                    // var(--primary, blue)
Brand.Or(Primary.Or(Color.Blue))          // var(--brand, var(--primary, blue))
```

---

### 32. `@container` Queries

**Status: DECIDED**

Same nesting indexer as `@media`. Requires `ContainerType` on parent.

```csharp
public static readonly Class CardContainer = new() {
    ContainerType = ContainerType.InlineSize,
};

[Container(CardContainer).MinWidth(400.Px)] = new() { ... }
```

**Container units:** `cqw`, `cqh`, `cqi`, `cqb`, `cqmin`, `cqmax` as extension properties on `int`/`double`. Same as `Vw`/`Vh`.

**Analyzer:** Warn if `Container.MinWidth()` used without any `ContainerType` in scope. Explore: code fix adding `ContainerType` to likely parent. Also warn if container units used without container context.

---

### 33. Post-MVP

| Feature | Design | Status |
|---|---|---|
| `@layer` | `CssLayer` type, `[LayerOrder]` attribute | Needs deeper exploration |
| `:is()` / `:where()` | `Css.Is(Card, Panel)` → Selector | Low priority |
| CSS trig functions | `Css.Sin()`, `Css.Cos()` inside calc | Niche |
| Scroll-driven animations | `AnimationTimeline.Scroll()` / `.View()` | Firefox ~v150 (mid-2026) |

---

## Summary

| # | Concept | Status |
|---|---------|--------|
| 1 | CSS Values | **Decided** — readonly structs, extension properties, `ICssValue` |
| 2 | Class vs Rule | **Decided** — Class for Razor, Rule/Rules for CSS-only, discovery by type |
| 3 | Selector Operators | **Decided** — `*` `+` `-` `>>` `>` `\|`, precedence matches CSS |
| 4 | Pseudo-classes | **Decided** — properties on Selector |
| 5 | Nesting | **Decided** — recursive indexer, unlimited depth |
| 6 | Selector Lists | **Decided** — params constructor/indexer |
| 7 | Keyframes | **Decided** — Percentage indexer, From/To constants |
| 8 | Media Queries | **Decided** — typed, nesting indexer |
| 9 | Element Selectors | **Decided** — `El.*`, generated from HTML spec |
| 10 | External Classes | **Decided** — auto-generated from framework CSS |
| 11 | ClassList | **Decided** — `+` operator, zero-alloc, string escape hatch |
| 12 | Build Pipeline | **Decided** — C# → SCSS → CSS via sass |
| 13 | Asset Source Generator | **Decided** — wwwroot scanning, typed Assets class |
| 14 | !important | **Decided** — `.Important` property, boolean flag |
| 15 | Attribute Selectors | **Decided** — 5 tiers, unified with attr() function |
| 16 | CSS Custom Properties | **Decided** — `CssVar<T>`, self-contained, `.Or()` fallback |
| 17 | CSS Functions | **Decided** — on value type or `Css`, expand as needed |
| 18 | Value Shorthands | **Decided** — Sides type, tuples, skip 3-value, property-specific types |
| 19 | Self Keyword | **Decided** — source gen injects + on Css |
| 20 | Prefixing | **Decided** — Program.cs global + attribute per-sheet |
| 21 | File Convention | **Decided** — `.css.cs` |
| 22 | Conditional Classes | **Decided** — `.When()` + `Class.None` |
| 23 | Class Variants | **Decided** — `.Variant(slug)` BEM modifiers |
| 24 | @font-face | **Decided** |
| 25 | @supports | **Decided** — nesting indexer |
| 26 | Not Supported | **Decided** — IDs, scoped styles, ::deep, @import, Tailwind, 3-value sides |
| 27 | Source Maps | **Decided** — chained C#→SCSS→CSS, all three visible |
| 28 | Source Gen DX | **Planned** — scaffold, preview, diagnostics, converter |
| 29 | Color Functions | **Decided** — SCSS for literals, CSS for variables, auto-dispatch |
| 30 | @property | **Decided** — auto from CssVar<T> |
| 31 | var() Fallbacks | **Decided** — `.Or()` with nesting |
| 32 | @container | **Decided** — same as @media, analyzer for ContainerType |
| 33 | Post-MVP | **Deferred** — @layer, :is/:where, trig, scroll animations |
