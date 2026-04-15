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

### 2. Class Definition

**Status: DECIDED**

A CSS class is a `static readonly Class` field. The field name maps to the CSS class name (PascalCase → kebab-case). Source generator discovers the name.

```csharp
// Chosen:
public static readonly Class Card = new() {
    Background = Color.White,
    BorderRadius = 8.Px,
};
// → .card { background: white; border-radius: 8px; }
```

In Razor: `<div class="@Card">` — implicit string conversion returns `"card"`.

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

Classes from external frameworks (MudBlazor, Bootstrap) that we reference but don't define. Marked with `[External]` attribute — the build step uses the name but doesn't emit declarations.

```csharp
[External] public static readonly Class MudTableCell = new();
// Source gen maps name → "mud-table-cell" (PascalCase → kebab-case)
// Build step does NOT emit .mud-table-cell { ... } — MudBlazor owns that
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

**Status: OPEN — candidates below**

#### Option A: Property (no parentheses)

Each value type has an `Important` property returning a copy with the flag set.

```csharp
Padding = 0.Px.Important
BackgroundColor = Mud.Palette.Primary.Important
Display = Display.None.Important
```

Internals — flag stored on the value struct:
```csharp
public readonly struct Length : ICssValue {
    private readonly string _value;
    private readonly bool _important;

    public Length Important => new(_value, important: true);
    public string ToCss() => _important ? _value + " !important" : _value;
}
```

Every value type needs this property. Could use an extension property on `ICssValue` if C# 14 supports it on interfaces — but return type must be `Self` not `ICssValue`, which requires self-types or generics.

**Pro:** Clean. No parentheses. Chainable.
**Con:** Every value type must add this property (or it's generated).

#### Option B: Wrapper type `Important<T>`

```csharp
Padding = new Important<Length>(0.Px)
// or with extension:
Padding = 0.Px.Important    // returns Important<Length>
```

The property setter must accept `Important<T>` alongside `T`. Needs implicit conversion or union type.

**Pro:** Single wrapper type.
**Con:** Property types become complex (`Length | Important<Length>`).

#### Option C: Extension method `.Important()`

```csharp
Padding = 0.Px.Important()
```

Same as Option A but with parentheses. Works today without extension properties.

**Pro:** Works in current C#.
**Con:** Parentheses (user prefers properties).

#### Option D: `.IMPORTANT` (all-caps property)

```csharp
Padding = 0.Px.IMPORTANT
```

Deliberately breaks C# conventions to visually scream, matching CSS behavior.

**Pro:** Visually distinctive, mirrors CSS convention.
**Con:** Violates .NET naming guidelines. Every value type still needs the property.

#### Option E: At the declaration level, not the value level

```csharp
public static readonly Class Card = new() {
    [Important] Padding = 0.Px,        // attribute-like (not valid C#)
    Padding = 0.Px | Important,        // operator (weird)
    Padding = Css.Important(0.Px),     // wrapper function
};
```

**Con:** Most of these aren't valid C# syntax.

**Leaning toward: A or D** — property on the value, parentheses-free.

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

#### Option A: Function returning Selector

```csharp
Attr("href")                              // [href]
Attr("type", "text")                      // [type="text"]
Attr("style").Contains("width:0%")        // [style*="width:0%"]
Attr("href").StartsWith("https")          // [href^="https"]
Attr("href").EndsWith(".pdf")             // [href$=".pdf"]
Attr("class").HasWord("card")             // [class~="card"]
Attr("lang").DashMatch("en")              // [lang|="en"]
```

**Pro:** Fluent, discoverable. `Attr("href").` shows all matchers in IntelliSense.
**Con:** Attribute names are strings. Could define known attributes as constants.

#### Option B: Typed attribute constants

```csharp
Attr.Href                                 // [href]
Attr.Type.Equals("text")                  // [type="text"]
Attr.Style.Contains("width:0%")           // [style*="width:0%"]
Attr.Data("stick-value").Equals("0")      // [data-stick-value="0"]
```

**Pro:** No strings for known attributes.
**Con:** Need to define every HTML attribute. `data-*` attributes still need strings.

#### Option C: Hybrid (typed for common, string for custom)

```csharp
// Typed for HTML-standard attributes:
Attr.Type.Equals("checkbox")              // [type="checkbox"]
Attr.Href                                 // [href]

// String for data-* and custom:
Attr("data-stick-value", "0")             // [data-stick-value="0"]
Attr("style").Contains("width:0%")        // [style*="width:0%"]
```

**Pro:** Best of both — typed where possible, flexible where needed.
**Con:** Two patterns to learn.

**Leaning toward: C** — hybrid gives typed IntelliSense for common attributes while keeping flexibility for data-* attributes.

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

#### Option A: RootVar attribute + Css.Var<T>()

```csharp
// Defining global variables (emits :root { --mw-primary: ... })
[RootVar] public static readonly Color MwPrimary = Color.Hex("#3498db");

// Defining scoped variables (emits inside a rule)
public static readonly Class Card = new() {
    [Css.Prop("--card-radius")] = 8.Px,         // define
    BorderRadius = Css.Var<Length>("--card-radius"),  // use
};

// Referencing external variables (MudBlazor)
static readonly Color Primary = Css.Var<Color>("--mud-palette-primary");
```

**Pro:** Typed references. Global vars have zero-string declaration via attribute.
**Con:** Scoped vars still use strings for names.

#### Option B: CssVar<T> type with static fields

```csharp
// Defining
public static readonly CssVar<Length> CardRadius = new("card-radius", 8.Px);
// Emits: --card-radius: 8px on whatever selector it's attached to

// Using
public static readonly Class Card = new() {
    Vars = { CardRadius },                  // attaches to this rule
    BorderRadius = CardRadius.Value,        // var(--card-radius)
};

public static readonly Class Title = new() {
    BorderRadius = CardRadius.Value,        // inherits from parent .card
};
```

**Pro:** Fully typed. Name derived from field name.
**Con:** More complex type system. `Vars = { ... }` is a new collection concept.

#### Option C: Inline definition with CssVar wrapper

```csharp
// Define + use in same rule
public static readonly CssVar<Length> CardRadius = new(8.Px);

public static readonly Class Card = new() {
    [CardRadius] = 8.Px,                    // --card-radius: 8px
    BorderRadius = CardRadius,              // var(--card-radius)
};
```

**Pro:** Clean nesting. Variable name from C# field name.
**Con:** Indexer overload for `CssVar<T>` alongside `Selector` indexer.

**Leaning toward: B or C** — typed variables with names from C# field names.

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

### 20. Source Maps

**Status: OPEN**

Chain: C# → SCSS map (our emitter tracks line numbers) → SCSS → CSS map (sass generates). Browser devtools could show C# source.

---

### Summary

| # | Concept | Status |
|---|---------|--------|
| 1 | CSS Values | **Decided** |
| 2 | Class Definition | **Decided** |
| 3 | Selector Operators | **Decided** |
| 4 | Pseudo-classes | **Decided** |
| 5 | Nesting | **Decided** |
| 6 | Selector Lists | **Decided** |
| 7 | Keyframes | **Decided** |
| 8 | Media Queries | **Decided** |
| 9 | Element Selectors | **Decided** |
| 10 | External Classes | **Decided** |
| 11 | ClassList | **Decided** |
| 12 | Build Pipeline | **Decided** |
| 13 | Asset Source Generator | **Decided** (not implemented) |
| 14 | !important | **Open** — leaning A or D |
| 15 | Attribute Selectors | **Open** — leaning C (hybrid) |
| 16 | CSS Custom Properties | **Open** — leaning B or C |
| 17 | CSS Functions | **Partially decided** |
| 18 | Value Shorthands | **Open** |
| 19 | Self Keyword | **Mostly decided** |
| 20 | Source Maps | **Open** |
