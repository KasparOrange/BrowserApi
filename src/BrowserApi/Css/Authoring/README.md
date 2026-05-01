# BrowserApi.Css.Authoring

C#-authored stylesheets that render to CSS at runtime, ready for Blazor.
Complete spec: [`docs/plans/browser-api/css-in-csharp.md`](../../../../docs/plans/browser-api/css-in-csharp.md).

## TL;DR — three places to wire it

```csharp
// 1. Program.cs — eager scan with options:
builder.Services.AddBrowserApiCss(opts => opts.GlobalPrefix = "mw");
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
    public static readonly CssVar<Length>   SpacingMd = new(Length.Px(12));
    public static readonly CssVar<CssColor> Primary   = new(CssColor.Hex("#0066cc"));
}

public class AppStyles : StyleSheet {
    public static readonly Class Btn = new() {
        Display       = Display.InlineFlex,
        AlignItems    = AlignItems.Center,
        Padding       = Sides.Of(Tokens.SpacingMd, Tokens.SpacingMd),
        Background    = Tokens.Primary,
        BorderRadius  = Length.Px(8),
        Color         = CssColor.White,
        Cursor        = Cursor.Pointer,
        FontWeight    = 600,
        BoxSizing     = BrowserApi.Css.BoxSizing.BorderBox,
        [Self.Hover]  = new() { Background = ((CssColor)Tokens.Primary).Darken(15) },
        [MediaQuery.PrefersDark] = new() { Background = CssColor.Hex("#003a80") },
    };
}
```

```razor
@* In any component *@
<button class="@AppStyles.Btn">Click me</button>
```

## Spec coverage matrix

| § | Concept | Status | Notes |
|---|---|---|---|
| 1 | CSS Values | ✓ | `Length`, `CssColor`, `Percentage`, `Angle`, `Duration`, `Flex`, `Resolution` from `BrowserApi.Css`. `.Important` on each. |
| 2 | Class vs Rule | ✓ | `Class`, `Rule`, `Rules` discovered by type. |
| 3 | Selector Operators | ✓ | `*` `+` `-` `>` `>>` `\|`, BCA002 errors on `<`/`<<`. |
| 4 | Pseudo-classes | ✓ | 30+ properties on `Selector`, terminal `PseudoElementSelector` for `::before`/`::after`/`::placeholder`. |
| 5 | Nesting | ✓ | Recursive indexer; source order preserved. |
| 6 | Selector Lists | ✓ | `params` constructor / indexer, `\|` operator. |
| 7 | Keyframes | ✓ | `From`/`To` constants, `Percentage` indexer, kebab-cased animation names. |
| 8 | Media Queries | ✓ | `MediaQuery.MaxWidth/MinWidth/PrefersDark/...` (renamed to dodge namespace collision). |
| 9 | Element Selectors | ✓ | Common HTML subset; full HTML-spec list is a generator addition. |
| 10 | External Classes | ✓ | `ExternalCssGenerator` parses CSS files via `<AdditionalFiles>` + `BrowserApiExternalCss="true"`. |
| 11 | ClassList | ✓ | `+` operator, 4-slot inline + overflow. |
| 12 | Build Pipeline | ✓ | Runtime `StyleSheet.Render` + `CssClassNameGenerator` for compile-time names. Sass intermediate is the remaining stretch goal. |
| 13 | Asset Source Generator | ✓ | `AssetGenerator` parses `<AdditionalFiles>` + `BrowserApiAsset="true"`. |
| 14 | !important | ✓ | `.Important` on `Length`/`CssColor`/`Percentage` partials + C# 14 extension property on every keyword enum (returns `Keyword<TEnum>`). |
| 15 | Attribute Selectors | ✓ | `Attr.Type`, `Attr.Aria.Hidden`, `Attr.Data(...)`, `Attr.Of(...)` with `.Equals`/`.HasWord`/`.DashMatch`/`.StartsWith`/`.EndsWith`/`.Contains`. |
| 16 | CSS Custom Properties | ✓ | `CssVar<T>`, auto-`:root` emission, `.Or()` fallback with deferred resolution. |
| 17 | CSS Functions | ✓ | `Length.Clamp/Min/Max/FitContent`, `GridTemplate.Repeat/MinMax`, `CssFn.Url/Env/String`, `CssFn.SafeArea.*`. |
| 18 | Value Shorthands | ✓ | `Sides` (1 / 2-tuple / 4-tuple via `Sides.Of`), `Border.Solid/Dashed/...`, BCA001 warns on unnamed 4-tuples. |
| 19 | Stylesheet-Injected Helpers | ✓ | `Self`, `From`, `To`, `Is(...)`, `Where(...)` via `protected static` on `StyleSheet`. |
| 20 | Prefixing & Config | ✓ | `[Prefix("...")]` attribute + `CssOptions.GlobalPrefix` via `AddBrowserApiCss(opts => ...)`. Single-place Program.cs config remains the post-MVP aspiration. |
| 21 | File Convention | ✓ | `.css.cs` extension is a human convention; the source gen and runtime walker don't depend on it. |
| 22 | Conditional Classes | ✓ | `.When(bool)` + `Class.None`. |
| 23 | Class Variants | ✓ | `.Variant(slug)` for BEM-style modifiers. |
| 24 | @font-face | ✓ | `FontFace` discovered by type, init-only properties for family/src/weight/style/display/unicode-range. |
| 25 | @supports | ✓ | `Supports.Property/Grid/Flex/Nesting`, `&`/`\|`/`!` operators. |
| 26 | Not Supported | ✓ | IDs for styling, `::deep`, 3-value sides, `@import`, Tailwind. |
| 27 | Source Maps | — | Deferred; runtime emits CSS directly without an SCSS intermediate. |
| 28 | Source Gen DX | partial | BCA001/BCA002/BCA003 analyzers shipped; scaffold/converter/code-fix providers deferred. |
| 29 | Color Functions | ✓ | `.Lighten/.Darken/.Saturate/.Desaturate/.AdjustHue/.Complement/.Grayscale/.Invert/.WithAlpha/.Mix` using CSS relative-color syntax (works for both literals and variables). |
| 30 | @property | ✓ | Auto-emitted from `CssVar<T>` with syntax inferred from T (Length → `<length>`, etc.). |
| 31 | var() Fallbacks | ✓ | `.Or(fallback)` returns T for inside-out nesting. |
| 32 | @container | ✓ | `ContainerQuery.MinWidth/MaxWidth`, container units (`50.Cqw()` etc.). |
| 33 | Post-MVP | partial | `@layer` shipped (attribute + indexer form). Trig functions, scroll-driven animations not yet. |
| 34 | :is() / :where() | ✓ | Injected helpers + post-fix `.Is(...)`/`.Where(...)`. |
| 35 | Specificity Analyzer | ✓ | BCA003 with `.editorconfig` threshold (`browserapi_css_specificity_class_threshold`). |

## Test surface

678 unit tests in `tests/BrowserApi.Tests/Css/Authoring/`:

- `StyleSheetTests` / `SelectorTests` — operator precedence, pseudo-class chaining, nesting.
- `InjectedHelperTests` — `Self`, `Is`, `Where` from inside a stylesheet.
- `CssRegistryTests` — AppDomain auto-discovery, refresh semantics.
- `PropertySurfaceTests` — keyword enum serialization, layout/flex/cursor properties.
- `SidesTests` / `MediaQueryTests` / `KeyframesTests` — multi-form shorthand, breakpoints, animations.
- `EndToEndTests` — design-tokens stylesheet rendered with CssVar consumers, snapshot-pinned canonical output.
- `CookbookTests` — realistic UI patterns (button, card, form, grid, animation, container, supports, important).
- `Round1FeaturesTests` — `.Or()` fallback chains, `@font-face`, `@property` syntax inference, `Rules` collections, keyframe-name resolution.
- `PrefixTests` — global + per-stylesheet prefixing, refresh-on-configure.
- `EnumImportantTests` — C# 14 extension properties on keywords.
- `ColorFunctionTests` — relative-color syntax for every manipulation method.
- `UnionTypeTests` — `LengthOrPercentage` flow through Width/Height/Padding setters.
- `AttrSelectorTests` — five-tier attribute selector surface.
- `LengthFunctionTests` — clamp/min/max/fit-content forms.
- `GridAndFunctionsTests` — repeat/minmax, url/env/string helpers, safe-area shortcuts.
- `ExtendedPseudoTests` — form/link/functional pseudo-classes.
- `LayerTests` — `[Layer]` attribute and `CssLayer.Of()` indexer.
- `AssetGeneratorTests` — typed `Assets.*` from `<AdditionalFiles>`.
- `ExternalCssTests` — typed `Mud.*`-style hierarchies parsed from external CSS.

## What's NOT here (real follow-up work)

1. **Sass intermediate + chained source maps** (spec §27) — would require
   sass invocation from an MSBuild target plus tooling to chain source
   maps across the C# → SCSS → CSS hops.
2. **Full HTML / CSS spec generators** — `El.*` and the typed-property
   surface are hand-written subsets. Generator-driven completion is
   plumbing work that doesn't change the API shape.
3. **Source-gen DX (§28)** beyond analyzers — scaffold code fix,
   CSS-preview comments, CSS-to-C# converter, extract-to-CssVar refactor.
4. **CSS trig functions** (sin/cos/tan inside calc) — listed as post-MVP.
5. **Scroll-driven animations** — listed as post-MVP.

## Architectural notes

- **Two `StyleSheet` types coexist.** `BrowserApi.Css.StyleSheet` is the
  CSSOM type generated from WebIDL (DOM API for runtime CSSOM access).
  `BrowserApi.Css.Authoring.StyleSheet` is the static-stylesheet authoring
  base added here. Use the alias pattern in consumers.
- **Some keyword enums are reused from CSSOM.** Where the CSSOM generator
  already produces an enum with `[StringValue]` attributes (BoxSizing,
  Visibility, FlexDirection, FlexWrap), we use those directly.
- **Two-pass scan.** `CssRegistry` does pass 1 = populate names on every
  Class/CssVar/Keyframes field; pass 2 = render to CSS. Cross-stylesheet
  references resolve correctly regardless of declaration order.
- **Late-binding registry.** `Or()` and `Keyframes`-as-string capture
  placeholder tokens at static-cctor time and resolve them at render time
  (after names are populated), so users can compose freely in field
  initializers without ordering hazards.
- **Source generator is additive.** The runtime registry's lazy
  AppDomain scan still works without `BrowserApi.Css.SourceGen`; the
  generator just moves the cost to compile time and gives compile-time
  visibility to names.
