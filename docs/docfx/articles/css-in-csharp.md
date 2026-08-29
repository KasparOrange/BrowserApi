# CSS-in-C# Authoring

Write your stylesheets in C#. Class names, selectors, values, and at-rules are all typed C# identifiers — typos are compile errors, rename refactoring works, and IntelliSense shows what's valid at every position.

This guide is the user-facing introduction. The full design rationale (every decision and its trade-offs) lives in the spec at [`docs/plans/browser-api/css-in-csharp.md`](https://github.com/KasparOrange/BrowserApi/blob/main/docs/plans/browser-api/css-in-csharp.md).

## When to use it

**Use CSS-in-C# for:** new components in a Blazor app where you'd otherwise hand-write a `.css` file. You get type safety, refactoring, IntelliSense, and design tokens-as-types — at the cost of a small runtime overhead per `class` attribute (typically 5–10 ns; invisible against the rest of a Blazor render).

**Don't migrate everything at once.** The property surface is ~80 of CSS's hundreds; source maps aren't emitted yet, so browser-side debugging shows the rendered CSS (readable prefixed class names), not your C# source. Migrate one stylesheet at a time and you'll find the gaps before they bite. See `docs/plans/browser-api/lessons-learned.md` in the repo for the staged playbook.

## Setup — three places to wire it

```csharp
// Program.cs — the ONLY place CSS-in-C# is configured (see "Configuration" below)
builder.Services.AddBrowserApiCss(css => {
    css.GlobalPrefix = "mw";
    css.HotReload    = true;   // dev live loop; ignored outside Development
});
```

```razor
@* App.razor — inside <head> *@
<HeadContent>
    <BrowserApiCss />
</HeadContent>
```

```csharp
// AppStyles.css.cs — the .css.cs suffix matters (see "The island rule")
using BrowserApi.Css;
using BrowserApi.Css.Authoring;
using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;

public class AppStyles : StyleSheet {
    public static readonly Class Btn = new() {
        Display       = Display.InlineFlex,
        Padding       = (8.Px, 16.Px),
        Background    = CssColor.Hex("#0066cc"),
        BorderRadius  = 8.Px,
        Cursor        = Cursor.Pointer,
        [Self.Hover]  = new() { Background = CssColor.Hex("#0052aa") },
    };
}
```

```razor
@* anywhere *@
<button class="@AppStyles.Btn">Click me</button>
```

That's it. `<BrowserApiCss />` walks the AppDomain at first render, finds every `StyleSheet` subclass, and emits one combined `<style>` block. The class name `AppStyles.Btn` resolves to `mw-btn` automatically (kebab-cased field name, prefixed with the global prefix you configured).

Packages: `BrowserApi` + `BrowserApi.Blazor` (runtime), `BrowserApi.Css.SourceGen` (analyzer — compile-time names, diagnostics, and the config bridge described below), and `BrowserApi.Css.Compiler` whenever you use live reload or static assets (it carries the island compiler and the MSBuild target; Roslyn is only loaded in Development).

## Core concepts

### `Class`, `Rule`, `Rules`

Three rule shapes by usage:

- **`Class`** is the most common: a CSS rule whose name is also referenced from Razor markup. Its identity is the field name (PascalCase → kebab-case). `<button class="@AppStyles.Btn">`.
- **`Rule`** is a stylesheet-only rule that takes its selector via constructor: `new Rule(El.Body) { Margin = 0.Px }`. No Razor reference; useful for resets and element styles.
- **`Rules`** is a collection of anonymous rules — for grouping resets where individual field names would be noise.

### Values are typed

```csharp
Padding = 16.Px;                                 // Length
Padding = (8.Px, 16.Px);                          // Sides — vertical/horizontal tuple
Padding = (top: 4.Px, right: 8.Px, bottom: 4.Px, left: 8.Px);
FontSize = 1.25.Rem;
Width = 50.Percent;
Width = Length.Clamp(1.Rem, 5.Vw, 30.Rem);
Color = CssColor.Hex("#0066cc");
Color = CssColor.Rgb(0, 102, 204);
Background = ((CssColor)Primary).Darken(8);      // typed color manipulation
```

The `16.Px` form is a C# 14 extension property on `int`/`double`. Same for `Em`/`Rem`/`Vh`/`Vw`/`Cqw`/`Cqh`/`Ms`/`S`/`Deg`/`Percent`/`Fr`.

### Selectors compose with C# operators

```csharp
Card * Active                  // .card.active        — compound
Card.Hover                     // .card:hover         — pseudo-class
Card > El.A                    // .card > a           — child
Card >> El.Span                // .card span          — descendant
Card + Sibling                 // .card + .sibling    — adjacent sibling
Card - Sibling                 // .card ~ .sibling    — general sibling
Card | Panel | Dialog          // .card, .panel, .dialog — selector list
Card.Not(Disabled)             // .card:not(.disabled)
Card.Has(Title)                // .card:has(.title)
```

Operator precedence is chosen so that `A * B > C` parses as `(A * B) > C` (compound binds tightest) and `A | B.Hover` parses as `A | (B.Hover)` (selector list binds loosest) — matching how a CSS author reads them.

The reverse-direction operators `<` and `<<` are reserved by C# operator-pair rules and have no CSS meaning. The **BCA002** analyzer turns any use into a compile error pointing at the right operator.

### Pseudo-elements terminate

```csharp
Card.After                     // PseudoElementSelector — .card::after
Card.After.Hover               // valid CSS — .card::after:hover
Card.After.Before              // COMPILE ERROR — CSS forbids two pseudo-elements
Card.After > El.Span           // COMPILE ERROR — combinators after pseudo-elements forbidden
```

The type changes at the moment a pseudo-element is attached. Invalid CSS becomes invalid C#.

### Nesting

The `[selector]` indexer is the universal "attach this in that context" mechanism — pseudo-class rules, descendant rules, media queries, container queries, feature queries, layer wrappers, all use it.

```csharp
public static readonly Class Card = new() {
    Padding = 16.Px,
    Background = CssColor.White,

    [Self.Hover] = new() { Background = CssColor.Hex("#f5f5f5") },
    [Self > El.A] = new() { Color = CssColor.Hex("#0066cc") },

    [MediaQuery.MaxWidth(768.Px)] = new() { Padding = 8.Px },
    [MediaQuery.PrefersDark] = new() { Background = CssColor.Hex("#1a1a1a") },

    [ContainerQuery.MinWidth(400.Px)] = new() { Display = Display.Grid },
    [Supports.Grid] = new() { Display = Display.Grid },
};
```

`Self` is the SCSS `&` parent reference — provided as a `protected static` member of `StyleSheet`, so it's available unqualified inside any subclass.

### Variables — `CssVar<T>`

```csharp
public class Tokens : StyleSheet {
    public static readonly CssVar<Length>   SpacingMd = new(12.Px);
    public static readonly CssVar<CssColor> Primary   = new(CssColor.Hex("#0066cc"));
    public static readonly CssVar<CssColor> Brand     = new(CssColor.Hex("#003399"));
}

public class Components : StyleSheet {
    public static readonly Class Btn = new() {
        Background = Tokens.Primary,                  // emits var(--primary)
        Padding    = Tokens.SpacingMd,                // emits var(--spacing-md)
        BorderColor = Tokens.Brand.Or(Tokens.Primary.Or(CssColor.Blue)),
                                                       // var(--brand, var(--primary, blue))
    };
}
```

`CssVar<T>` auto-emits its default to a `:root` block plus an `@property` rule (with the syntax inferred from `T`). The implicit conversion to `T` produces `var(--name)`. `.Or(fallback)` returns `T`, not `CssVar<T>`, so accidental `.Or().Or()` chaining is impossible — you nest inside-out.

### BEM-style variants

```csharp
public static readonly Class KanbanHeader = new() {
    FontWeight = 600,
    Padding = (8.Px, 12.Px),

    [Self.Variant("todo")]     = new() { Background = ((CssColor)Info).WithAlpha(0.15) },
    [Self.Variant("progress")] = new() { Background = ((CssColor)Warning).WithAlpha(0.15) },
    [Self.Variant("done")]     = new() { Background = ((CssColor)Success).WithAlpha(0.15) },
};
```

```razor
<div class="@(KanbanHeader + KanbanHeader.Variant(col.Slug))">
```

`Class.Variant(slug)` returns a sibling `Class` named `{name}--{slug}`. Combined with `+` it produces a `ClassList` with two class tokens — `class="kanban-header kanban-header--todo"`. (Don't confuse with `Selector.Variant(slug)` which returns a `Selector` for use as a nesting indexer key — different type, same name, two different concerns.)

### At-rules

```csharp
// Media queries
[MediaQuery.PrefersDark] = new() { ... }
[MediaQuery.MinWidth(640.Px) & MediaQuery.MaxWidth(1024.Px)] = new() { ... }

// Container queries — the parent must declare ContainerType
public static readonly Class CardWrapper = new() {
    ContainerType = "inline-size",
    [ContainerQuery.MinWidth(400.Px)] = new() { ... },
};

// @supports
[Supports.Grid] = new() { ... }
[Supports.Property("backdrop-filter", "blur(10px)")] = new() { ... }

// @keyframes — typed Percentage indexer plus injected From/To constants
public static readonly Keyframes FadeIn = new() {
    [From]       = new() { Opacity = 0 },
    [50.Percent] = new() { Opacity = 0.5 },
    [To]         = new() { Opacity = 1 },
};

// Reference an animation by typed name in a transition string
public static readonly Class Toast = new() {
    Animation = FadeIn + " 200ms ease-out",
};

// @font-face
public static readonly FontFace Inter = new() {
    Family = "Inter",
    Src = "url('/fonts/Inter.woff2') format('woff2')",
    Weight = "400 700",
    Display = "swap",
};

// @layer — both attribute form and indexer form
[Layer("utilities")]
public class UtilityStyles : StyleSheet { ... }    // wraps the whole stylesheet

public static readonly Class Card = new() {
    Padding = 16.Px,
    [CssLayer.Of("components")] = new() { ... },   // wraps just the nested block
};

// @property — auto-emitted from CssVar<T>, no work needed
```

### Color manipulation

```csharp
var blue = CssColor.Hex("#3498db");

blue.Lighten(20)      // hsl(from #3498db h s calc(l + 20%))
blue.Darken(15)
blue.Saturate(30)
blue.Desaturate(20)
blue.AdjustHue(45)
blue.Complement
blue.Grayscale
blue.Invert
blue.WithAlpha(0.5)   // hsl(from #3498db h s l / 0.5)
blue.Mix(red, 60)     // color-mix(in srgb, #3498db 60%, #ff0000)
```

All methods use CSS relative-color syntax — they work the same way on literal colors and on `var(...)` references, so theming works without ceremony.

### Important

```csharp
Padding   = 0.Px.Important;
Display   = Display.None.Important;
Color     = CssColor.Red.Important;
```

`.Important` is a property on every value type — primitives via partial structs, keyword enums via C# 14 extension properties.

### Conditional and composed classes

```razor
<div class="@(Card + Active.When(isActive))">       @* "card active" or just "card" *@
<div class="@(isActive ? Active : Class.None)">     @* explicit conditional *@
<div class="@(Card + "vendor-specific-class")">      @* string escape hatch *@
```

`+` between two `Class` values produces a `ClassList` (struct, four-slot inline storage, no heap allocation for the common case). `Class.None` is a sentinel that renders as the empty string.

## Prefix system

Two levels of prefix combine into the final class name:

```csharp
// Project-wide (Program.cs):
builder.Services.AddBrowserApiCss(css => css.GlobalPrefix = "mw");

// Per-stylesheet:
[Prefix("dt")]
public class DnDTestStyles : StyleSheet {
    public static readonly Class Card = new() { ... };
    // → ".mw-dt-card"
}
```

Prefixes scope your CSS away from third-party stylesheets (no specificity wars) and avoid name collisions across feature areas.

## Configuration — one place, `Program.cs`

Everything CSS-in-C# does is configured in a single call. There is no MSBuild property, no `.editorconfig` key, no CLI flag you have to keep in sync with it:

```csharp
builder.Services.AddBrowserApiCss(css => {
    css.GlobalPrefix       = "mw";          // prefix on every class / variable / keyframes name
    css.StaticAssetPath    = "css/app.css"; // set ⇒ build writes the file, app serves <link>
    css.HotReload          = true;          // dev live loop; safe to commit, Development-only
    css.AllowAppReferences = false;         // may .css.cs files reference the app assembly?
});
```

| Option | Default | Who reads it |
|---|---|---|
| `GlobalPrefix` | `""` | runtime, live reload, static build, source generator |
| `StaticAssetPath` | `null` (inline) | runtime, static build |
| `HotReload` | `false` | runtime (Development only) |
| `AllowAppReferences` | `false` | live reload, static build |

The only per-file override is `[Prefix("…")]` on a stylesheet — a *scoping* decision that belongs next to the styles it scopes.

### Why this works — and the one rule it imposes

The natural objection: source generators and the `browserapi-css` build tool run *before* `Program.cs` executes. How can they read it?

They don't, directly. `BrowserApi.Css.SourceGen` reads the lambda **during your normal build**, takes every `css.X = …` assignment whose right-hand side is a compile-time constant, and emits

```csharp
// obj/…/BrowserApiCssConfig.g.cs  (generated — never edit)
[assembly: BrowserApi.Css.Authoring.BrowserApiCssConfig(GlobalPrefix = "mw", StaticAssetPath = "css/app.css")]
```

into your assembly. The static build reads that attribute from the compiled DLL; the class-name generator uses the same prefix; the runtime simply runs the lambda. One declaration, every consumer.

**The rule:** options that build-time tools need — `GlobalPrefix`, `StaticAssetPath`, `AllowAppReferences` — must be **literals or `const`s** in the lambda. A value from `IConfiguration` or an environment variable still works for the running app, but the build can't see it; analyzer **BCA005** warns so you're never surprised when `browserapi-css build` emits with a different prefix and the drift guard rejects the file. `HotReload` is runtime-only and may be anything.

We chose this over the alternatives deliberately. MSBuild properties and CLI flags are what build tools *natively* read, but they would give you three places to keep in sync and three places to look — the drift guard would catch mismatches, yet "correct but annoying" is not the bar. An assembly-level attribute you write yourself is one place, but it's not the place .NET developers expect (`Program.cs` is). Parsing the lambda costs one constraint (constants) that matches how 99% of apps configure a prefix anyway, and it keeps the file everyone already opens as the single source of truth. Developer experience is the north star of this library; this is that principle applied to configuration.

## Emission modes — inline, live reload, static asset

One renderer, three places its output can land. Authoring never changes, and the output is byte-identical by construction because every mode calls the same `StyleSheet.Render`.

| Mode | When | What you do |
|---|---|---|
| **Inline** | default, zero build | nothing — `<BrowserApiCss />` emits one `<style>` tag, rendered once per process |
| **Live reload** | development | `css.HotReload = true` + reference `BrowserApi.Css.Compiler` |
| **Static asset** | production, strict CSP, exports | `css.StaticAssetPath = "css/app.css"` + reference `BrowserApi.Css.Compiler` + install the `browserapi-css` tool once |

### Live reload — save a `.css.cs`, see it in ~100 ms

```csharp
css.HotReload = true;   // Program.cs
```
```xml
<PackageReference Include="BrowserApi.Css.Compiler" Version="…" />   <!-- dev-time dependency -->
```

Why this isn't .NET Hot Reload: your stylesheet fields are `static readonly`, initialized once by the type initializer. Hot Reload can patch that initializer's body but the runtime never runs it again, so the old values stay in the fields and the rendered CSS never changes. (We verified there is no supported way around this on .NET 10.)

What happens instead: a hosted service — registered for you when `HotReload` is set and the Compiler package is present — watches `**/*.css.cs`. On save it recompiles **just those files** with Roslyn (tens of milliseconds; no MSBuild, no Razor), loads the result into a collectible load context, renders it through the same `StyleSheet.Render`, and swaps the CSS into `CssRegistry`. `<BrowserApiCss />` observes `CssRegistry.Changed` and re-renders its `<style>` body through the normal Blazor diff. No page reload; circuit state survives.

The running app's own `AppStyles.Btn` objects are never touched — the island is a *second copy* whose CSS replaces the registry output. Class names are a pure function of field name + prefix, so existing markup keeps pointing at the right selectors while the rules behind them change. Renames, deletions, and new classes all work because every save is a full island compile. A Razor file that references a *new* class still goes through `dotnet watch`'s own hot reload; that's outside the CSS loop.

Works under plain `dotnet run`. Only Development pays for Roslyn and a file watcher, so leaving `HotReload = true` in committed code is fine. A compile error in the island is logged and the last good CSS keeps serving.

### Static asset — build-time `.css`, served via `<link>`

```csharp
css.StaticAssetPath = "css/app.css";   // Program.cs
```
```bash
dotnet new tool-manifest                # once per repo, if you don't have one
dotnet tool install BrowserApi.Css.Cli  # once; commit the manifest
```

That's the whole setup. `BrowserApi.Css.Compiler` ships an MSBuild target that runs after every build: it hands the compiled app DLL to `dotnet browserapi-css build`, which reads the `[BrowserApiCssConfig]` attribute, compiles the island, and writes `wwwroot/css/app.css` — only touching the file when the content changed. If `StaticAssetPath` was never set, the tool exits quietly; if the tool isn't installed, the build prints a one-line hint and continues.

`<BrowserApiCss />` now emits `<link rel="stylesheet" href="/css/app.css?v=<hash>">`. Gains: browser-cacheable across sessions, no inline `<style>` (strict CSP without `unsafe-inline`), styles exist without a running circuit, and the `.css` file is the export for non-.NET consumers — design tokens for a Svelte build, say.

**Drift guard.** Before trusting the file, the component compares its content once per process against what the loaded assemblies render. Any mismatch — stale build output, a prefix that reached the runtime but not the build (BCA005) — falls back to inline delivery and logs a warning. Wrong styles are never served silently. While live reload has swapped in newer CSS, the component also serves inline, since the file on disk is older than what you just saved.

`dotnet browserapi-css watch --app bin/Debug/net10.0/MyApp.dll` keeps the file current without the app running (design-mode iteration, token export) — `--app` is how the tool finds your configuration when the build isn't driving it. `--out`, `--prefix`, `--allow-app-references`, `--reference` exist as explicit overrides for export scenarios; you never need them for the app itself.

### The island rule — what a `.css.cs` file may reference

Live reload and the static build compile your `*.css.cs` files **in isolation**: the files plus a reference to `BrowserApi`, nothing else. That's what makes the compile take milliseconds instead of an MSBuild round-trip. It imposes one rule:

> A `.css.cs` file may reference `BrowserApi` and other stylesheets. Nothing else from your app.

| You want to… | Do this |
|---|---|
| Share a value between stylesheets | `CssVar<T>` or a `const` in a stylesheet; reference it as `Tokens.Spacing` |
| Read an app constant / enum / static helper | `css.AllowAppReferences = true` — the island then also references the app's **last built** assembly (a newly added constant appears after the next normal build) |
| Use a per-user, per-theme, or per-component value | **Not a stylesheet concern.** Declare a `CssVar<T>` with a default and set the variable on an element at runtime (`style="--accent: …"`). The browser recomputes instantly; no CSS is regenerated. This is also how a theme editor works. |
| Read a DI service or request state | Impossible in any mode — stylesheets are static by design, same for every user |

A file that breaks the rule compiles fine in the app (inline mode is unaffected) but fails the island build. The watcher logs the normal C# diagnostic and keeps serving the last good CSS; the static build fails loudly.

## Companion source generator — `BrowserApi.Css.SourceGen`

Add the package as an analyzer reference:

```xml
<PackageReference Include="BrowserApi.Css.SourceGen" Version="..." PrivateAssets="all" />
```

It ships four generators and four analyzers:

### `CssConfigGenerator` — the `Program.cs` bridge

Reads `AddBrowserApiCss(css => …)` and emits `[assembly: BrowserApiCssConfig(…)]` with the constant option values, so `browserapi-css` and the other generators read the same configuration as the runtime. Details and the constants-only rule in "Configuration" above.

### `CssClassNameGenerator` — module-init pre-population

Discovers every `StyleSheet` subclass at compile time and emits a `[ModuleInitializer]` that pre-populates `Class.Name` / `CssVar<T>.Name` / `Keyframes.Name` before user code runs. This means `class="@AppStyles.Card"` in Razor doesn't need to trigger a runtime AppDomain scan.

### `AssetGenerator` — typed `Assets.*`

```xml
<ItemGroup>
    <AdditionalFiles Include="wwwroot/**/*.*">
        <BrowserApiAsset>true</BrowserApiAsset>
        <AssetRootDir>wwwroot/</AssetRootDir>
    </AdditionalFiles>
    <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="BrowserApiAsset" />
    <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="AssetRootDir" />
</ItemGroup>
```

```csharp
Assets.Css.App;          // → "css/app.css"
Assets.Images.Logo;      // → "images/logo.svg"
```

### `ExternalCssGenerator` — typed `Mud.*`

```xml
<AdditionalFiles Include="$(NuGetPackageRoot)mudblazor/.../MudBlazor.css">
    <BrowserApiExternalCss>true</BrowserApiExternalCss>
    <ExternalCssRoot>Mud</ExternalCssRoot>
    <ExternalCssPrefix>mud-</ExternalCssPrefix>
</AdditionalFiles>
```

```csharp
Mud.Button.Primary;            // Class for .mud-button-primary
Mud.Palette.Primary;           // CssVar<CssColor> for --mud-palette-primary
```

### Analyzers

- **BCA001** — warns when a 4-element tuple is converted to `Sides` without named elements (CSS shorthand goes top-right-bottom-left clockwise; without names the order is too easy to get wrong). Recommended fix: name the tuple elements (`top:`, `right:`, `bottom:`, `left:`) or use `Sides.Of(top:, right:, bottom:, left:)`.
- **BCA002** — errors on the unsupported `<` / `<<` selector operators with a message pointing at the intended operator (`>` for child, `>>` for descendant).
- **BCA003** — warns when selector specificity exceeds a configurable threshold. Each `*` operator counts as one class/attribute/pseudo-class in CSS specificity's *b* component. Configure via `.editorconfig`:
  ```ini
  [*.cs]
  browserapi_css_specificity_class_threshold = 2
  dotnet_diagnostic.BCA003.severity = warning
  ```
  Recommended fix: wrap in `:where(...)` to flatten specificity to zero, or reduce the modifier count.
- **BCA005** — warns when an `AddBrowserApiCss` option that build-time tools need (`GlobalPrefix`, `StaticAssetPath`, `AllowAppReferences`) is assigned a non-constant. The runtime still honors it; the static build won't see it. Fix: use a literal or `const`. See "Configuration" above for why.

## Disambiguation — two `StyleSheet` types

`BrowserApi.Css` already has a `StyleSheet` type — the CSSOM type generated from WebIDL, used for runtime DOM-level CSS access. `BrowserApi.Css.Authoring.StyleSheet` is the authoring base. They serve unrelated purposes; consumers alias to disambiguate:

```csharp
using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;
```

The same pattern often applies to `Position`, `AlignItems`, `JustifyContent`, `Transition`, `Shadow`, `Easing`, `FlexDirection`, `BoxSizing`, `Visibility`, `FlexWrap` — MudBlazor or the CSSOM bring same-named types into scope. A single using-alias block at the top of your stylesheet keeps the body clean.

## Performance

A `class="@AppStyles.X"` access today does a static field load + an implicit `Class → string` conversion + a property getter — typically 5–10 ns per access. That's invisible against the rest of a Blazor render in normal use; a kanban with hundreds of cards still has render time dominated by the rendertree-builder bookkeeping and SignalR diff. The performance plan (`docs/plans/const-equivalent-cscss-performance.md`) describes how to benchmark each cost in isolation and which optimizations might close the remaining gap to true const-equivalent (~1 ns).

For now: don't worry about it for typical UI components. Re-measure if you hit a render-heavy page.

## Where to go from here

- The full design rationale (35 sections, every decision and trade-off) — [`docs/plans/browser-api/css-in-csharp.md`](https://github.com/KasparOrange/BrowserApi/blob/main/docs/plans/browser-api/css-in-csharp.md).
- Migration playbook, spec-violation audit checklist, known gotchas — [`docs/plans/browser-api/lessons-learned.md`](https://github.com/KasparOrange/BrowserApi/blob/main/docs/plans/browser-api/lessons-learned.md).
- Performance measurement plan — [`docs/plans/const-equivalent-cscss-performance.md`](https://github.com/KasparOrange/BrowserApi/blob/main/docs/plans/const-equivalent-cscss-performance.md).
- The implementation itself with XML docs on every public type — [`src/BrowserApi/Css/Authoring/`](https://github.com/KasparOrange/BrowserApi/tree/main/src/BrowserApi/Css/Authoring).
