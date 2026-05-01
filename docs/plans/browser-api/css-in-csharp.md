# CSS-in-C# API Design

**Parent:** [css.md](css.md)

This is the complete design spec for expressing CSS in C#. It was designed across multiple sessions with deep discussion of trade-offs, edge cases, and real-world validation against MitWare's `app.css` (800+ lines of production CSS).

---

## Philosophy

These principles drove every decision. When something doesn't work during implementation, resolve it in the spirit of these principles — don't just patch around it.

### 1. Zero string literals in user-facing APIs

Every CSS class name, selector, attribute, property, and value should be a typed C# identifier with IntelliSense. String literals are invisible to the compiler — typos become silent 404s or broken styles. The type system catches errors at compile time.

**Escape hatches, explicitly.** When the typed surface can't cover a case — a class from a CSS file we can't parse, an attribute our generator doesn't know, a bleeding-edge CSS feature — offer a string-based escape hatch (`Class.External`, `Attr(name, value)`, raw `Css.Raw(...)`). These are the pragmatic completion of the typed API, not its failure. The "no string literals" rule serves developer experience — it catches typos the compiler couldn't see otherwise. When a string IS the only signal (a framework's arbitrary class name, an unrecognized attribute), a typed identifier adds zero value and a string is correct. Use escape hatches without apology, just make them visually distinct from the typed path so reviewers notice them.

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

### 9. Drift from CSS/SCSS terminology when a C# name is clearer

CSS spec language ("custom property"), SCSS preprocessor language ("variable"), and colloquial developer usage ("CSS variable") conflict in confusing ways. When the established term is ambiguous in context, pick the name that most clearly communicates intent to a C# developer — even if it diverges from the spec. Concretely for this codebase:

- **"Variable"** = a runtime CSS custom property (`--name`, read via `var()`). User-facing type is `CssVar<T>`. Matches the mental model most devs have, even though W3C formally calls this a "custom property."
- **"Property"** = a regular CSS property (`color`, `display`, `padding`) — a field on the `Declarations` type. Never used for custom properties, to avoid the overload.
- **SCSS `$var`** is internal plumbing and never appears in the user-facing API, so there's no conflict with "variable" in our vocabulary.

Apply the same judgment when future terms collide.

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

**Operator pairing requirement:** C# mandates `>` and `<` declared together, `>>` and `<<` together. The unused pair (`<`, `<<`) is enforced at compile time via analyzer **BCA002** (`DiagnosticSeverity.Error`) — any use stops the build with a message pointing at the intended operator (`>` or `>>`). The operators still throw `NotSupportedException` at runtime as a backstop for reflection/dynamic edge cases the analyzer can't see.

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

**Intent:** `.Hover` is a MODIFIER of `Card`, not an independent thing. It returns a new `Selector`. Chaining works: `Card.Hover.After` = `.card:hover::after`.

**Pseudo-element terminal state:** Properties that attach a pseudo-element (`.Before`, `.After`, `.Placeholder`, etc.) return a `PseudoElementSelector` — a constrained type that allows only pseudo-classes on top (since `::after:hover` is valid CSS) but forbids:

- further pseudo-elements (`.After.Before` → **compile error**, CSS forbids two pseudo-elements)
- combinators (`.After > El.Span` → **compile error**, CSS forbids descendants after pseudo-elements)
- functional/structural pseudo-classes (`.NthChild(2)` → **compile error**, no structure past a pseudo-element)

| Expression | Result type | Valid CSS |
|---|---|---|
| `Card.Hover` | `Selector` | `.card:hover` ✓ |
| `Card.After` | `PseudoElementSelector` | `.card::after` ✓ |
| `Card.After.Hover` | `PseudoElementSelector` | `.card::after:hover` ✓ |
| `Card.Hover.After` | `PseudoElementSelector` | `.card:hover::after` ✓ |
| `Card.After.Before` | **compile error** | two pseudo-elements ✗ |
| `Card.After > El.Span` | **compile error** | combinator after pseudo-element ✗ |

This catches invalid CSS at C# compile time instead of at SCSS compilation or browser parse time.

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

**Source order is preserved by the source generator.** The generator walks the object initializer's syntax tree assignment-by-assignment in declared order and emits SCSS in that same order. There is no runtime backing collection — the `Declarations` type's setters and indexer exist only to satisfy the C# compiler. Because of this, "what does `OrderedDictionary` do on duplicate key?" is a non-question; we never construct one. The behaviors that do matter:

- Same property assigned twice (`Padding = 10.Px; …; Padding = 20.Px`) → both emit; second wins per CSS cascade. Future analyzer warns on the duplicate.
- Same nested key with disjoint declarations (`[Self.Hover] = new() { Background = … }; …; [Self.Hover] = new() { Opacity = … }`) → emits two SCSS blocks with the same selector. CSS naturally combines them; no data loss.
- Same nested key with overlapping declarations → both emit; analyzer warns on the overlap.

Order is exactly what the developer wrote. No silent merge or overwrite logic to reason about.

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

**Status: DECIDED — single Roslyn source generator drives both the typed surface and SCSS emission.**

```
C# StyleSheet  ──┬──▶  generated typed surface (class names, CssVar mappings, Assets)
                 │       (visible in IDE the moment you save — instant rename, navigation, IntelliSense)
                 │
                 └──▶  generated .scss file
                         │
                         └──▶  sass (peer dep) ──▶  .css ──▶  wwwroot/
```

One `StyleSheet` class = one `.scss` file = one `.css` file. The source generator is the single authoring tool. An MSBuild target shells `sass` (developer-installed peer dep) to produce the final `.css`.

**Why source gen (not a CLI tool):**

- **IDE responsiveness.** Typed surface updates as you type — refactor, rename, navigation work without a build cycle.
- **Roslyn semantic model.** Selector validity, type checks, prefix collisions all available at edit time, attached to real `SyntaxNode`s for diagnostics with squiggles.
- **Single tool.** The previous "CLI tool runs `dotnet run` against your project" architecture had two compilation units and a fragile invocation path. Source gen collapses it.

**MVP spike — must validate before committing.** The smallest end-to-end slice that proves the architecture:

1. New Roslyn incremental source generator (project location TBD — likely a new `BrowserApi.Css.SourceGen` to keep it independent of the existing `BrowserApi.SourceGen` for the .ts-first JSInterop workflow).
2. Generator finds subclasses of `StyleSheet`, walks `static readonly Class`/`Rule`/`CssVar<T>` fields, emits:
   - a `partial class` with `const string` for each class name (the typed surface)
   - an `.scss` file via `context.AddSource`, or to `$(IntermediateOutputPath)` if `AddSource` proves awkward for non-`.cs` outputs
3. Test project using `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing` — assert byte-exact SCSS output for known input. Same testing pattern `BrowserApi.Generator.Tests` already uses; pure TDD.
4. MSBuild target shells `sass` against the emitted `.scss`, writes to `wwwroot/css/`.
5. End-to-end sample with three rules: a flat `Class`, a `Class` with `[Self.Hover]` nesting, a `CssVar<Color>` with default + `:root` emission.

If 1–5 land cleanly the architecture is validated. If source gens turn out unable to write `.scss` alongside the compilation cleanly, fall back to the prior CLI-tool design (kept on standby).

**Sass dependency.** `dart-sass` is the canonical implementation. We document it as a peer dependency and let users install it the way they install other build tools (`npm i -g sass` or platform package manager). No NuGet wrapping for MVP.

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

**Enum keywords keep enum semantics.** CSS keyword types (`Display`, `Position`, `Cursor`, etc.) stay as `enum` — C# 14 extension properties make `.Important` work without losing switch exhaustiveness, zero allocation, or compact generated code:

```csharp
public enum Display { None, Block, Flex, Grid, InlineBlock, /* … */ }

public static class DisplayExtensions {
    extension(Display value) {
        public Important<Display> Important => new(value);
    }
}

// Usage stays identical to a struct-with-static-fields approach:
Display = Display.None.Important;
```

The property setter for `Display` accepts both the bare enum (via implicit conversion) and `Important<Display>`. Each generated keyword enum gets a one-line extension declaration emitted alongside it. **Requires C# 14+** project-wide.

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

**Primitive union types — no implicit conversions between primitives.** CSS has several primitive value categories that overlap in some properties (length/percentage), but not in others (font-weight is number-only). Instead of giving primitives implicit conversions to each other (which would let `FontWeight = 50.Percent` compile by accident), we define small union-wrapper structs that primitives implicitly convert TO. Properties and functions accept the wrapper.

| Wrapper | Accepts | Used by |
|---|---|---|
| `LengthOrPercentage` | `Length`, `Percentage` | `Padding`, `Margin`, `Width`, `Height`, `Top`/`Right`/`Bottom`/`Left`, `BorderRadius`, `Gap`, `TextIndent`, translate values, `BackgroundPosition`/`Size`, etc. |
| `NumberOrPercentage` | `double` (number), `Percentage` | `Opacity`, filter `brightness()`/`contrast()`/`saturate()`, etc. |
| `Image` | `Css.Url(...)`, `Gradient.*`, `Css.ImageSet(...)` | `BackgroundImage`, `ListStyleImage`, `Cursor` (with fallback) |

```csharp
// Consumer syntax is unchanged from primitives:
Padding = 10.Px,                                 // Length → LengthOrPercentage
Padding = 50.Percent,                            // Percentage → LengthOrPercentage
Padding = (10.Px, 20.Percent),                   // tuple of LengthOrPercentage
Width = Length.Clamp(1.Rem, 50.Percent, 30.Rem), // Clamp signature: (LengthOrPercentage × 3) → LengthOrPercentage

// What no longer compiles (good — these were latent bugs):
FontWeight = 50.Percent,    // FontWeight only accepts Number — Percentage doesn't convert
LineHeight = 16.Px,          // LineHeight is unitless multiplier OR Length, not Percentage
```

**`Length` and `Percentage` do NOT implicitly convert to each other.** Each represents a distinct CSS primitive. The wrapper is the only meeting point, and only properties that genuinely accept both expose it.

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

### 19. Stylesheet-Injected Helpers (`Self`, `From`/`To`, `Is`, `Where`)

**Status: DECIDED**

The `StyleSheet` base class exposes `protected static` members that derived stylesheets reference unqualified. This is plain C# inheritance — no source-gen trickery, no magic naming.

```csharp
public abstract class StyleSheet {
    // Selector parent reference:
    protected static Selector Self { get; } = new("&");

    // Keyframe stops:
    protected static Percentage From { get; } = 0.Percent;
    protected static Percentage To   { get; } = 100.Percent;

    // :is() / :where() grouping helpers (see §34):
    protected static Selector Is(params Selector[] selectors)    => …;
    protected static Selector Where(params Selector[] selectors) => …;
}
```

Used inside any `partial class … : StyleSheet`:

```csharp
[Self.Hover] = new() { … },                          // &:hover
[Self > Title] = new() { … },                        // & > .title
[Self.Variant("active")] = new() { … },              // &--active
[From] = new() { Opacity = 0 },                      // 0% keyframe
[To]   = new() { Opacity = 1 },                      // 100% keyframe
[Is(Self.Hover, Self.FocusVisible)] = new() { … },   // :is(&:hover, &:focus-visible)
[Where(El.H1, El.H2, El.H3)] = new() { … },          // :where(h1, h2, h3) — zero specificity
```

**Outside stylesheets** (tests, shared library code, anywhere there's no `StyleSheet` base): the same names live on `Css` — `Css.Self`, `Css.From`, `Css.To`, `Css.Is(...)`, `Css.Where(...)`. The `protected static` form is just the in-stylesheet shortcut.

**Adding to this list later** should be deliberate: only inject names that are frequently used inside stylesheets and meaningfully shorter unqualified than as `Css.X`. Sparingly.

---

### 20. Prefixing & Configuration

**Status: DECIDED for MVP. Single-place `Program.cs` config noted as the aspirational end state.**

Two prefix levels: global (project-wide) and per-stylesheet. Chain: `{global}-{stylesheet}-{classname}`.

**Global prefix — MSBuild property:**

```xml
<PropertyGroup>
    <BrowserApiCssGlobalPrefix>mw</BrowserApiCssGlobalPrefix>
</PropertyGroup>
```

The package's shipped `.props` exposes this to the source generator via `<CompilerVisibleProperty>`. The generator reads it from `GlobalOptions` — no AST parsing of `Program.cs`, no runtime indirection. Survives refactors, works in libraries that have no `Program.cs`, can't be set to a non-literal value.

**Per-stylesheet prefix — attribute:**

```csharp
[Prefix("sp")]
public static partial class ShiftPlannerStyles : StyleSheet {
    public static readonly Class PeopleList = new() { ... };
    // → .mw-sp-people-list
}
```

Prefix is transparent in Razor — the developer never writes it.

**Configuration ladder (MVP).** Three surfaces, each for what it's natively good at:

| Setting type | Where | Why there |
|---|---|---|
| Build-time toggles (prefix, feature flags) | MSBuild properties (`.csproj`, `Directory.Build.props`, package `.props` defaults) | Source gens read these natively via `CompilerVisibleProperty`. |
| Analyzer severity & per-rule options | `.editorconfig` | Idiomatic for Roslyn analyzers; same place as built-in `CAxxxx` rules. |
| Runtime DI registration | `Program.cs` (`AddBrowserApiCss(...)`) | What `Program.cs` is for. No build-time reading happens here. |

**Aspirational: single-place config in `Program.cs` (post-MVP).**

The "configure once in `Program.cs` and the analyzers, prefix, and runtime all pick it up" UX matches what most NuGet packages already deliver, and devs expect it. The friction is build-time vs runtime: source gens and analyzers run before `Program.cs` ever executes. Paths to revisit:

- **Source gen reads `AddBrowserApiCss(opts => …)` syntax tree** for literal/const values, with an analyzer error on non-literal arguments. Same pattern `LoggerMessage` and Minimal-API source gens already use. Brittle for arbitrary expressions, fine for the literal case (≈99% of usage).
- **Assembly-level attribute** `[assembly: BrowserApiCss(GlobalPrefix = "mw")]` — single declarative place, source-gen-readable, lives in `AssemblyInfo.cs`-ish scope rather than `Program.cs`.
- **Layered config** — MSBuild/`.editorconfig` defaults with `Program.cs`-driven overrides parsed by source gen. Most flexible, most moving parts.

Pick deliberately later, once the MVP shows where the friction actually is. Tracked in §33 (Post-MVP).

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

**Status: PLANNED — MVP ships the typed surface and emitter; the items below are layered in after.**

The "developer experience deluxe" north star applies most directly here. Each item should be evaluated for whether it catches real mistakes early or makes real workflows shorter — speculation features get cut.

**Scaffolding & content tools:**

- **Scaffold:** code fix on `StyleSheet` → generates a starter template.
- **CSS preview:** generated XML doc comment on each `Class`/`Rule` showing the compiled CSS, visible on hover.
- **CSS-to-C# converter:** paste CSS into a `.css.cs` file → code fix converts to typed declarations. Highest-leverage onboarding feature.
- **Extract-to-CssVar:** select a repeated value (color, length) → code fix refactors all uses to a new `CssVar<T>` and inserts the declaration.

**Diagnostics — confirmed (defined elsewhere in spec):**

- **BCA001** — 4-value `Sides` without named parameters (§18).
- **BCA002** — use of unsupported `<` / `<<` selector operators (§3).
- **BCA003** — selector specificity above threshold; suggests `:where()` wrap (§35).

**Diagnostics — planned:**

- **Dead class** — `static readonly Class Foo` never referenced in any Razor file or other stylesheet.
- **Unset CssVar** — `CssVar<T>` referenced via `var()` but never declared.
- **Container without container type** — `Container.MinWidth(...)` used inside a class with no ancestor declaring `ContainerType` (best-effort scope analysis).
- **Prefix collision** — two stylesheets with the same `[Prefix(...)]` value.
- **Invalid color** — `Color.Hex("notahex")` validated at compile time.
- **Duplicate property/selector key in same initializer** — flagged when overlapping (see §5).
- **Pseudo-element ordering** — already covered by the type split in §4; the analyzer is a backstop for paths that bypass the type system.

**Configuration.** Each diagnostic's severity is set via `.editorconfig` (idiomatic Roslyn) — see §20's config ladder.

**Testability — open question.** Stylesheet authors will want to assert SCSS output. Likely path: `StyleSheet.ToScss()` is a publicly invokable method emitted by the source gen, returning the same string the file would. Resolve once the emitter exists; until then, integration-test by inspecting the generated `.scss`/`.css` artifacts in the build output. Tracked as a question, not a blocker.

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

**The `IsVariable` flag generalizes — it lives on `ICssValue`.** Color is the most visible example, but the same dispatch issue applies to any value type with SCSS-vs-CSS function pairs (notably `Length` arithmetic, where `2 * var(--gap)` must emit `calc()` rather than be pre-computed). Every value carries an `IsVariable` boolean; every operation that takes other values OR's the inputs' flags to produce the result's flag.

```csharp
public interface ICssValue {
    string ToCss();
    bool IsVariable { get; }   // true if this value contains or is derived from a var(...) reference
                               // — the source gen must emit it through the CSS branch, not SCSS,
                               // since sass cannot compute against custom-property references.
}
```

**Taint propagation rule.** Any operation involving a variable-backed input produces a variable-backed output. Once a value is variable-backed, it stays that way through every subsequent operation, even ones with literal-only operands.

```
literal Color · literal Color    →  literal      (both branches available; SCSS path picked for cleaner output)
literal Color · variable Color   →  variable     (sass can't see var() — must use CSS branch)
variable Color · literal Color   →  variable     (taint propagates)
variable Color · variable Color  →  variable     (CSS branch)
literal Length + variable Length →  variable     (calc(... + var(--x)))
```

The `Color` struct's two-branch implementation in the table above is an instance of this pattern, not a special case for color. `Length`, `Percentage`, and any future arithmetic-bearing type follow the same rule.

**Naming:** "variable" matches our project vocabulary (§9) — a `CssVar<T>` reference, or any value derived from one. The flag is internal, never user-facing.

**Implementation note:** The `Color` struct stores a string and the `IsVariable` flag inherited from `ICssValue`. All methods are string concatenation — no color math in C#. The emitted string is either an SCSS function call or a CSS function call, determined by the flag at emit time.

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
| CSS trig functions | `Css.Sin()`, `Css.Cos()` inside calc | Niche |
| Scroll-driven animations | `AnimationTimeline.Scroll()` / `.View()` | Firefox ~v150 (mid-2026) |
| Single-place config in `Program.cs` | See §20 aspirational note | Investigate after MVP friction maps |
| BCA004 override-conflict analyzer | See §35 | Layer in after BCA003 ships |

---

### 34. `:is()` / `:where()`

**Status: DECIDED**

Both are exposed as helper functions injected into every `StyleSheet` (see §19). `:is()` matches any of its arguments and inherits the highest specificity among them; `:where()` is identical in matching but has specificity zero — the specificity-deflation tool.

```csharp
// :where for zero-specificity base styles — overridable without wars:
public static readonly Rule HeadingBase =
    new(Where(El.H1, El.H2, El.H3, El.H4, El.H5, El.H6)) {
        LineHeight = 1.2,
        FontWeight = 600,
    };

// :is for state grouping inside a rule:
public static readonly Class Btn = new() {
    [Is(Self.Hover, Self.FocusVisible)] = new() {
        Outline = Outline.Solid(2.Px, FocusRing),
    },
};

// :is after a combinator — unify several descendant element types:
public static readonly Class Article = new() {
    [Self >> Is(El.P, El.Ul, El.Ol)] = new() { MarginBlock = 1.Em },
};

// :where as a near-universal reset — zero specificity so component styles win:
public static readonly Rule BoxReset =
    new(Where(El.All, El.All.Before, El.All.After)) {
        BoxSizing = BoxSizing.BorderBox,
    };
```

**Why the injected-function form (and not `|` selector lists or fluent methods on `Selector`):**

- The `|` operator (§3) produces comma-separated rules with *independent* per-arm specificity. `:is()` collapses to one rule with the max specificity; `:where()` to one rule with zero specificity. Different semantics deserve different syntax.
- Fluent forms (`Card.Is(Panel, Dialog)`) read as "card that's also a panel or dialog" — rarely the intended use of `:is()`. The common pattern starts a selector or follows a combinator, which fits a function-shaped helper.
- Static helpers (`Css.Is(...)`) work but feel foreign next to the unqualified `Self`/`From`/`To` already in scope. Same mechanism, same invocation style.

---

### 35. Specificity Analyzer

**Status: PLANNED (post-MVP, queued behind MVP shipping)**

CSS specificity drives override wars. The API makes compound selectors easy to write, so we need a guardrail to keep authors honest. This section is intentionally detailed because it's deferred — the design should be ready when implementation slot opens.

**BCA003** — warn when a selector's specificity exceeds a configured threshold. Specificity is computed during emission (we already walk every selector), then compared against:

```ini
# .editorconfig
[*.cs]
# Defaults — opinionated but configurable:
browserapi_css_specificity_class_threshold = 2     # warn at 3+ classes/attrs/pseudos in one selector
browserapi_css_specificity_total_threshold = 4     # warn when (b + c) exceeds this
dotnet_diagnostic.BCA003.severity = warning
```

Specificity tuple `(b, c)`: `b` = classes + attributes + pseudo-classes, `c` = type selectors + pseudo-elements. IDs are always zero (we don't support them for styling, §26).

**Examples:**

```csharp
// (1, 0) — fine
public static readonly Class Card = new() { ... };

// (1, 1) — fine, type selectors are cheap
[Self > El.A] = new() { ... }

// (2, 0) — fine, common BEM-style state
[Self * Active] = new() { ... }

// (3, 0) — at threshold, BCA003 fires:
[Self * Active * Featured] = new() { ... }
//   ╰── analyzer: "Selector specificity (3, 0) exceeds threshold (2).
//                  Code fix: wrap in :where(...) to flatten to (0, 0)."

// Opting out by intent — wrap in :where() and the warning vanishes:
[Where(Self * Active * Featured)] = new() { ... }
//   specificity (0, 0)

// Type-only chains stay quiet under reasonable depth:
[El.Article > El.Section > El.H2 > El.A] = new() { ... }
//   (0, 4) — typically fine; controlled by total_threshold
```

**BCA004 — override-conflict detection (later, harder, more valuable).** Within a single stylesheet, when two rules both set the same CSS property, the later rule has lower-or-equal specificity than the earlier one, *and* the rules could match the same element, warn. The "could match the same element" analysis is the hard part — start with the obvious case (same base class with extra modifiers later in source) and grow from there. Skip until BCA003 ships and we have signal on real-world specificity patterns.

**Future analyzer ideas (deluxe DX, listed here for tracking):**

- Unused declaration (a property assignment that's overridden everywhere it could apply).
- Suspected typo on `Class.External("…")` — fuzzy-match against known classes from `<ExternalCss>` packages.
- `!important` density — warn when `.Important` shows up on more than N declarations in a sheet (signals override-war pain).
- Dead nesting — `[selector] = new() { }` with no declarations.
- Variable shadowing — local `CssVar<T>` declared with the same name as one inherited from an external package.
- Pseudo-element-only rules with no body.

**North-star reminder:** every analyzer here should catch real mistakes humans hit, not chase theoretical purity. If a rule fires more on legitimate code than on bugs, raise its threshold or kill it.

---

## Summary

| # | Concept | Status |
|---|---------|--------|
| 1 | CSS Values | **Decided** — readonly structs, extension properties, `ICssValue` |
| 2 | Class vs Rule | **Decided** — Class for Razor, Rule/Rules for CSS-only, discovery by type |
| 3 | Selector Operators | **Decided** — `*` `+` `-` `>>` `>` `\|`; BCA002 errors on unsupported `<`/`<<` |
| 4 | Pseudo-classes | **Decided** — properties on Selector; pseudo-elements return terminal `PseudoElementSelector` |
| 5 | Nesting | **Decided** — recursive indexer; source gen walks syntax in source order, no runtime collection |
| 6 | Selector Lists | **Decided** — params constructor/indexer |
| 7 | Keyframes | **Decided** — Percentage indexer, From/To constants |
| 8 | Media Queries | **Decided** — typed, nesting indexer |
| 9 | Element Selectors | **Decided** — `El.*`, generated from HTML spec |
| 10 | External Classes | **Decided** — auto-generated from framework CSS |
| 11 | ClassList | **Decided** — `+` operator, zero-alloc, string escape hatch |
| 12 | Build Pipeline | **Decided** — single Roslyn source gen for typed surface AND `.scss`; sass → `.css`; spike validates first |
| 13 | Asset Source Generator | **Decided** — wwwroot scanning, typed Assets class |
| 14 | !important | **Decided** — `.Important` via C# 14 extension properties; enums stay as enums |
| 15 | Attribute Selectors | **Decided** — 5 tiers, unified with attr() function |
| 16 | CSS Custom Properties | **Decided** — `CssVar<T>`, self-contained, `.Or()` fallback |
| 17 | CSS Functions | **Decided** — on value type or `Css`; primitive union wrappers (`LengthOrPercentage`, etc.) replace cross-primitive implicit conversions |
| 18 | Value Shorthands | **Decided** — Sides type, tuples, skip 3-value, property-specific types |
| 19 | Stylesheet-Injected Helpers | **Decided** — `Self`, `From`/`To`, `Is`, `Where` via `protected static` on `StyleSheet` base; same names also on `Css` |
| 20 | Prefixing & Config | **Decided** — MSBuild property + `.editorconfig` + per-sheet attribute; single-place `Program.cs` config is the post-MVP aspiration |
| 21 | File Convention | **Decided** — `.css.cs` |
| 22 | Conditional Classes | **Decided** — `.When()` + `Class.None` |
| 23 | Class Variants | **Decided** — `.Variant(slug)` BEM modifiers |
| 24 | @font-face | **Decided** |
| 25 | @supports | **Decided** — nesting indexer |
| 26 | Not Supported | **Decided** — IDs, scoped styles, ::deep, @import, Tailwind, 3-value sides |
| 27 | Source Maps | **Decided** — chained C#→SCSS→CSS, all three visible |
| 28 | Source Gen DX | **Planned** — scaffold, preview, diagnostics, converter; testability TBD |
| 29 | Color Functions | **Decided** — SCSS for literals, CSS for variables; taint-propagating `IsVariable` flag on `ICssValue` |
| 30 | @property | **Decided** — auto from CssVar<T> |
| 31 | var() Fallbacks | **Decided** — `.Or()` with nesting |
| 32 | @container | **Decided** — same as @media, analyzer for ContainerType |
| 33 | Post-MVP | **Deferred** — @layer, trig, scroll animations, single-place config, BCA004 |
| 34 | :is() / :where() | **Decided** — injected helpers `Is(...)` / `Where(...)`, see §19 + §34 |
| 35 | Specificity Analyzer | **Planned** — BCA003 with `.editorconfig` thresholds; BCA004 layered in later |
