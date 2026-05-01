# BrowserApi.Css.Authoring — implementation status

This namespace holds the **CSS-in-C# authoring API** described in
[`docs/plans/browser-api/css-in-csharp.md`](../../../../docs/plans/browser-api/css-in-csharp.md).

## What's shipped here (MVP slice)

| File | Spec section | Status |
|---|---|---|
| `Selector.cs` | §3 selector operators, §4 pseudo-classes (subset) | ✓ working |
| `PseudoElementSelector.cs` | §4 pseudo-element terminal type | ✓ working |
| `Class.cs` | §2 Class type, §22 conditional classes, §23 variants | ✓ working |
| `ClassList.cs` | §11 ClassList | ✓ working (4-slot inline + overflow) |
| `Declarations.cs` | §5 nesting, partial §17 typed properties | ✓ representative subset |
| `Rule.cs` | §2 Rule type | ✓ working |
| `StyleSheet.cs` | §19 injected helpers, runtime render path | ✓ reflection-based |
| `El.cs` | §9 element selectors | ✓ common HTML elements (subset) |
| `CssVar.cs` | §16 custom properties | ✓ basic; default-emission TBD |

11 unit tests pass, covering operator precedence, pseudo-class chaining,
selector composition, `Class.None`/`ClassList`, `Self`/`Is`/`Where`,
nested-rule emission, and the `<` / `<<` reverse-operator runtime guard.

## What's NOT here yet (queued, in priority order)

1. **Roslyn source generator.** The MVP renders CSS at runtime via reflection.
   The spike scope from §12 — generator project, generated typed surface,
   `.scss` emission, sass-shell MSBuild target — is the next deliverable.
2. **Full property surface.** `Declarations` exposes ~10 representative
   properties. The full set (~hundreds) is generated from the CSS spec data
   at `specs/css/*.json` by the same generator that produces the existing
   `BrowserApi/Generated/` types.
3. **Property-specific value types.** `FontSize` accepts `Length` only; the
   spec wants `FontSize.Large` keywords too. Generated alongside the property
   surface.
4. **`LengthOrPercentage` / `NumberOrPercentage` / `Image` union wrappers.**
   §17. Replaces ad-hoc cross-primitive implicit conversions.
5. **`IsVariable` taint propagation across all `ICssValue`.** §29. Currently
   every value is treated as literal.
6. **`@media` / `@supports` / `@container` indexers.** §8, §25, §32. The
   `Declarations` indexer accepts `Selector` only for now.
7. **`Keyframes` / `FontFace` types.** §7, §24.
8. **Color functions with SCSS-vs-CSS dispatch.** §29.
9. **Analyzers BCA001/2/3.** §3, §18, §35.
10. **External CSS parser** (MudBlazor, etc.). §10.
11. **Sass invocation MSBuild target.** §12.
12. **Source maps.** §27.

## How to use what's here today

```csharp
using BrowserApi.Css;
using BrowserApi.Css.Authoring;
using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;  // disambiguate from CSSOM

public class AppStyles : StyleSheet {
    public static readonly Class Card = new() {
        Padding = Length.Px(16),
        Background = CssColor.White,
        BorderRadius = Length.Px(8),
        [Self.Hover] = new MyHover {
            Background = CssColor.Hex("#f5f5f5"),
        },
    };

    private class MyHover : Declarations { }
}

string css = StyleSheet.Render<AppStyles>();
// .card { padding: 16px; background: #fff; border-radius: 8px; }
// .card:hover { background: #f5f5f5; }
```

## How MitWare can sanity-check today

Drop one trivial stylesheet into MitWare, call
`StyleSheet.Render<MyStyles>()` at startup, write the result to `wwwroot/`,
and link it from `_Host.cshtml` / `App.razor`. This proves the authoring
syntax is workable in the real codebase before we invest in the source
generator. **Don't migrate `app.css` yet** — the property surface is too
narrow for that.

## Architectural notes

- **Two `StyleSheet` types coexist.** `BrowserApi.Css.StyleSheet` is the
  CSSOM type generated from WebIDL (DOM API for runtime CSSOM access).
  `BrowserApi.Css.Authoring.StyleSheet` is the static-stylesheet authoring
  base added here. They serve unrelated purposes; consumers either pick
  one namespace or alias as shown above. See `docs/plans/browser-api/css-in-csharp.md`
  §9 for the naming philosophy.
- **Render order.** `StyleSheet.Render` walks `static readonly` fields in
  declaration order via reflection. Source order is preserved naturally —
  see §5 of the spec.
- **`&` resolution at render time.** The MVP resolves the SCSS parent
  reference itself rather than deferring to sass, which keeps the path
  testable without a sass dependency. When sass is wired in (per §12) the
  emitter emits raw `&:hover` and lets sass resolve.
