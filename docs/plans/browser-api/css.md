# CSS

**Parent:** [browser-api.md](browser-api.md)

## Purpose

Typed CSS properties, values, selectors, and stylesheet generation. The largest hand-written ergonomic surface in the project — generated property declarations topped with beautiful factory methods and operators.

## Use Cases

- **Type-safe inline styles** — `element.Style.Display = Display.Flex` instead of magic strings
- **Server-side stylesheet generation** — build `.css` files from C# with compile-time safety
- **Email templates** — inline styles without typo risk
- **Design tokens** — define a design system as C# types, export to CSS variables

## Core Types

### ICssValue

Every CSS value type implements this:
```csharp
public interface ICssValue {
    string ToCss();
}
```

### Value Types (hand-written ergonomic layer)

| Type | Represents | Example |
|------|-----------|---------|
| `Length` | `<length>` | `Length.Rem(1.5)` / `1.5.Rem` |
| `Color` | `<color>` | `Color.Hsl(220, 90, 56)` |
| `Duration` | `<time>` | `Duration.Ms(300)` / `300.Ms` |
| `Angle` | `<angle>` | `Angle.Deg(45)` / `45.Deg` |
| `Percentage` | `<percentage>` | `Percentage.Of(50)` / `50.Percent` |
| `Resolution` | `<resolution>` | `Resolution.Dpi(96)` |
| `Flex` | `<flex>` | `Flex.Fr(1)` / `1.Fr` |

**Pending changes:**
- Rename `CssColor` to `Color` (namespace handles disambiguation)
- Replace extension methods (`2.Px()`) with C# 14 extension properties (`2.Px`) — no parentheses

### Composite Types

| Type | Represents | Example |
|------|-----------|---------|
| `Transform` | `<transform-function>` | `Transform.Rotate(45.Deg).Scale(1.5)` |
| `Gradient` | `<gradient>` | `Gradient.Linear(...)` |
| `Shadow` | `<shadow>` | `Shadow.Box(0.Px, 2.Px, blur: 8.Px, color: Color.Rgba(0,0,0,0.1))` |
| `Transition` | `<transition>` | `Transition.For(Property.Opacity, 300.Ms, Easing.EaseInOut)` |

### CssStyleDeclaration (generated)

One property per CSS property, typed with the correct value type. Generated from the 124 CSS JSON files.

```csharp
public partial class CssStyleDeclaration {
    public Display Display { get; set; }
    public Color BackgroundColor { get; set; }
    public Length Gap { get; set; }
}
```

---

## Selector API

### Design Principles

1. **Zero string literals** — every selector part is a typed C# identifier
2. **Operators match CSS** — `>` is child, `+` is adjacent sibling, etc.
3. **Fluent API alongside operators** — both always available, freely mixable
4. **C# precedence matches CSS specificity** — compound binds tightest, selector list loosest

### Core Types

```csharp
// A named CSS class — field name maps to kebab-case class name
// "Container" → ".container", "CardTitle" → ".card-title"
public readonly struct Class {
    // CSS properties via init-only properties (same as CssStyleDeclaration)
    public Display Display { get; init; }
    public Length Gap { get; init; }
    // ... all CSS properties

    // Implicit conversion to string (returns class name for Razor)
    public static implicit operator string(Class c) => c.Name;

    // Implicit conversion to Selector (for operator composition)
    public static implicit operator Selector(Class c) => new(c);

    // Pseudo-class indexer
    public Selector this[PseudoClass pseudo] => ...;
}

// A composed selector — result of operator/fluent composition
public readonly struct Selector {
    // Pseudo-class indexer (enables chaining)
    public Selector this[PseudoClass pseudo] => ...;

    // Fluent API
    public Selector And(Selector other) => ...;
    public Selector Child(Selector other) => ...;
    public Selector Descendant(Selector other) => ...;
    public Selector Adjacent(Selector other) => ...;
    public Selector Sibling(Selector other) => ...;
    public Selector Or(Selector other) => ...;
    public Selector On(PseudoClass pseudo) => ...;
}

// A selector + declarations (for complex selectors beyond simple class rules)
public readonly struct Rule {
    // Constructor takes a selector
    public Rule(Selector selector) { ... }

    // CSS properties via init-only properties
    public Display Display { get; init; }
    public Length Gap { get; init; }
    // ... all CSS properties
}
```

### Operator Table

| CSS | Meaning | C# Operator | Fluent Method | Precedence |
|-----|---------|-------------|---------------|------------|
| `.a:hover` | pseudo-class | `A[Hover]` | `A.On(Hover)` | 1 (highest) |
| `.a.b` | compound | `A * B` | `A.And(B)` | 3 |
| `.a + .b` | adjacent sibling | `A + B` | `A.Adjacent(B)` | 4 |
| `.a ~ .b` | general sibling | `A - B` | `A.Sibling(B)` | 4 |
| `.a .b` | descendant | `A >> B` | `A.Descendant(B)` | 5 |
| `.a > .b` | child | `A > B` | `A.Child(B)` | 6 |
| `.a, .b` | selector list | `A \| B` | `A.Or(B)` | 10 (lowest) |

**Why these operators:**
- `>` is literal CSS child combinator
- `>>` is "going deeper" (descendant)
- `+` is literal CSS adjacent sibling
- `-` pairs with `+` visually; `~` can't be binary in C#
- `*` means "both/intersection"; has high precedence (binds tighter than combinators)
- `|` means "or" (selector list / comma)

**Operator implementation notes:**
- `>` requires paired `<` declaration (C# rule) — `<` throws `NotSupportedException`
- `>>` requires paired `<<` declaration — same treatment
- All operators are defined on `Selector`; `Class` has implicit conversion to `Selector`
- All operators return `Selector`, enabling unlimited chaining

### Pseudo-Classes

```csharp
public static class Pseudo {
    // Simple
    public static readonly PseudoClass Hover = new(":hover");
    public static readonly PseudoClass Focus = new(":focus");
    public static readonly PseudoClass Active = new(":active");
    public static readonly PseudoClass Visited = new(":visited");
    public static readonly PseudoClass Disabled = new(":disabled");
    public static readonly PseudoClass FirstChild = new(":first-child");
    public static readonly PseudoClass LastChild = new(":last-child");
    public static readonly PseudoClass Empty = new(":empty");
    public static readonly PseudoClass Odd = new(":nth-child(odd)");
    public static readonly PseudoClass Even = new(":nth-child(even)");

    // Functional
    public static PseudoClass NthChild(int a, int b = 0) => ...;
    public static PseudoClass Not(Class target) => ...;
    public static PseudoClass Has(Class target) => ...;

    // Pseudo-elements
    public static readonly PseudoClass Before = new("::before");
    public static readonly PseudoClass After = new("::after");
    public static readonly PseudoClass Placeholder = new("::placeholder");
}
```

**Disambiguation:** The indexer only accepts `PseudoClass`. Compound (`.card.active`) uses the `*` operator with `Class`. No type ambiguity.

**Import styles** — user chooses verbosity:
- `Card[Pseudo.Hover]` — discoverable (type `Pseudo.` for IntelliSense)
- `Card[Hover]` — smooth (via `using static BrowserApi.Css.Pseudo`)
- `Card[P.Hover]` — middle ground (via `using P = BrowserApi.Css.Pseudo`)

### Complex Selector Examples

```csharp
// Operators
Card[Hover] > Title                           // .card:hover > .title
(Card * Active > Title)[FirstChild]           // .card.active > .title:first-child
Nav * Dark >> Item * Selected > Link[Hover]   // .nav.dark .item.selected > .link:hover
(Card >> Title) | (Panel >> Title)            // .card .title, .panel .title
(Form > Input)[Focus] + Label                // .form > .input:focus + .label

// Fluent
Card.On(Hover).Child(Title)
Card.And(Active).Child(Title).On(FirstChild)
Nav.And(Dark).Descendant(Item.And(Selected).Child(Link.On(Hover)))

// Mixed (recommended for readability)
Card.And(Active) > Title.On(FirstChild)
Nav.And(Dark) >> Item.And(Selected) > Link.On(Hover)
```

### Media Queries (typed)

```csharp
Media.MaxWidth(768.Px)                                     // (max-width: 768px)
Media.MinWidth(1024.Px)                                    // (min-width: 1024px)
Media.PrefersDark                                          // (prefers-color-scheme: dark)
Media.PrefersReducedMotion                                 // (prefers-reduced-motion: reduce)
Media.MinWidth(768.Px) & Media.MaxWidth(1024.Px)           // and
Media.Print | Media.Screen                                 // or
```

### Keyframes (indexer initializer)

```csharp
public static readonly Keyframes FadeIn = new() {
    [From] = new() { Opacity = 0 },
    [50.Percent] = new() { Opacity = 0.5, Transform = Transform.Scale(1.1) },
    [To] = new() { Opacity = 1 },
};
```

`From`/`To` are `Percentage` constants (0%, 100%). The `CssKeyframes` indexer accepts `Percentage`, so `[50.Percent]`, `[From]`, and `[To]` all use the same overload.

### Full Stylesheet Example

```csharp
public static partial class AppStyles : StyleSheet {

    // Variables — just C#
    static readonly Length PageGutter = 24.Px;

    // Mixins — just methods
    static void FlexCenter(Class s) {
        s.Display = Display.Flex;
        s.AlignItems = AlignItems.Center;
        s.JustifyContent = JustifyContent.Center;
    }

    // Simple classes — field name = CSS class name
    public static readonly Class Container = new() {
        Display = Display.Flex,
        Gap = 1.Rem,
        Padding = Css.Sides(16.Px, PageGutter),
    };

    public static readonly Class Card = new() {
        Background = Color.White,
        BorderRadius = 8.Px,
        BoxShadow = Shadow.Box(0.Px, 2.Px, blur: 8.Px, color: Color.Rgba(0,0,0,0.1)),
        Transition = Transition.For(Property.BoxShadow, 200.Ms, Easing.EaseOut),
    };

    public static readonly Class Title = new() {
        FontSize = 1.25.Rem,
        FontWeight = FontWeight.Bold,
    };

    // Complex selectors — constructor takes composed selector
    public static readonly Rule CardHovered = new(Card[Hover]) {
        BoxShadow = Shadow.Box(0.Px, 8.Px, blur: 24.Px, color: Color.Rgba(0,0,0,0.15)),
        Transform = Transform.TranslateY(-2.Px),
    };

    public static readonly Rule CardTitle = new(Card > Title) {
        Color = Color.Gray(20),
    };

    public static readonly Rule CardMobile = new(Card, Media.MaxWidth(768.Px)) {
        BorderRadius = Length.Zero,
    };

    // Keyframes
    public static readonly Keyframes FadeIn = new() {
        [From] = new() { Opacity = 0, Transform = Transform.TranslateY(10.Px) },
        [To] = new() { Opacity = 1, Transform = Transform.TranslateY(0.Px) },
    };
}
```

### How `Class` Knows Its Name

The `Class` struct doesn't know its field name at construction. The source generator in `BrowserApi.SourceGen`:

1. Finds all `static readonly Class` fields in types inheriting `StyleSheet`
2. Converts field name PascalCase → kebab-case (`CardTitle` → `card-title`)
3. Emits a partial static constructor that sets the name

```csharp
// Source-generated
public static partial class AppStyles {
    static AppStyles() {
        Container.Name = "container";
        Card.Name = "card";
        Title.Name = "title";
    }
}
```

### Razor Usage

```razor
@using static MyApp.Styles.AppStyles

<div class="@Container">
    <div class="@Card">
        <h2 class="@Title">Hello</h2>
    </div>
</div>
```

`Class` has implicit conversion to `string` → returns the kebab-case class name.

---

## Stylesheet Compilation (Build-Time)

### Principle

CSS is generated at **build time**, not runtime. C# is the source language. A build step compiles it to `.css` files. Zero runtime cost — same model as SCSS.

### Pipeline

```
C# StyleSheet classes → MSBuild target (dotnet run) → .css files in wwwroot/
```

```xml
<Target Name="CompileCssStyles" BeforeTargets="Build">
    <Exec Command="dotnet run --project tools/CssCompiler -- --output wwwroot/css/" />
</Target>
```

### C# Replaces SCSS

| SCSS Feature | C# Equivalent |
|-------------|---------------|
| `$variable` | `var` / `const` / `static readonly` |
| `@mixin` | Method |
| `@extend` | Method call / inheritance |
| `@if/@for/@each` | `if` / `for` / `foreach` |
| `math()` | `Length` operators (`+`, `-`, calc) |
| `color.adjust()` | `Color.Lighten()` etc. |
| Nesting | Operator/fluent selector composition |
| `@use/@import` | `using` / namespaces |

C# is strictly more powerful than SCSS. SCSS adds nothing that C# doesn't already have natively.

**Open question:** Should the build step emit SCSS as an intermediate format instead of CSS directly? SCSS nesting/mixin compilation is battle-tested. Emitting SCSS would delegate complex CSS output formatting to the SCSS compiler. Trade-off: adds a `sass` dependency vs. implementing CSS serialization ourselves.

---

## Asset Source Generator

### Purpose

Eliminate magic strings for **all** static assets. A Roslyn incremental source generator scans `wwwroot/` and emits typed constants.

### Setup

```xml
<!-- .csproj -->
<ItemGroup>
    <AdditionalFiles Include="wwwroot/**/*" />
</ItemGroup>
```

### Generated Output

```
wwwroot/
  css/
    scheduler.css
    app.css
  images/
    my-picture.png
    logo.svg
  js/
    site.js
```

```csharp
// <auto-generated/>
public static partial class Assets {
    public static class Css {
        public const string Scheduler = "css/scheduler.css";
        public const string App = "css/app.css";
    }
    public static class Images {
        public const string MyPicture = "images/my-picture.png";
        public const string Logo = "images/logo.svg";
    }
    public static class Js {
        public const string Site = "js/site.js";
    }
}
```

**Naming:** directory → nested class, filename (minus extension) → PascalCase constant, dashes/underscores stripped.

### Reactivity

File add/rename/delete in `wwwroot/` triggers regeneration automatically:
- SDK-style projects re-evaluate globs on filesystem changes
- `AdditionalFiles` list updates
- Incremental source generator re-runs
- IntelliSense reflects changes within seconds

**Delete a file** → constant disappears → compile errors at every usage site.
**Rename a file** → old constant gone (errors), new constant appears in IntelliSense.

### Code Fix Provider (planned)

Analyzer + fixer: when an asset reference breaks due to rename/delete, suggest the closest new match:

```
CS0117: 'Assets.Images' does not contain 'MyPicture'
        Did you mean 'HeroBanner'? [Fix: Replace with Assets.Images.HeroBanner]
```

### AssetLink Component

Blazor component in `BrowserApi.Blazor` that renders the correct HTML tag based on file extension:

```razor
<AssetLink For="@Assets.Css.Scheduler" />
@* → <link rel="stylesheet" href="@Assets["css/scheduler.css"]" /> *@

<AssetLink For="@Assets.Js.Site" />
@* → <script src="@Assets["js/site.js"]"></script> *@
```

For images, use the typed path directly (need alt, width, height, etc.):
```razor
<img src="@Assets[Assets.Images.MyPicture]" alt="Schedule" />
```

---

## Scope

The 124 CSS spec JSON files define hundreds of properties. Prioritize by usage:

1. **Layout** — display, flex, grid, position, box model
2. **Visual** — color, background, border, shadow, opacity
3. **Typography** — font, text, line-height, letter-spacing
4. **Animation** — transition, animation, transform
5. **Everything else** — expand over time

## Testing

Pure TDD — every value type's `ToCss()` is tested as string in → string out:
```csharp
Assert.Equal("1.5rem", Length.Rem(1.5).ToCss());
Assert.Equal("hsl(220,90%,56%)", Color.Hsl(220, 90, 56).ToCss());
```

Selector tests — assert CSS output:
```csharp
Assert.Equal(".card:hover > .title", (Card[Hover] > Title).ToCss());
Assert.Equal(".card.active", (Card * Active).ToCss());
```
