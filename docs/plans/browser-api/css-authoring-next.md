# CSS-in-C# Authoring — Next Steps

**Parent:** [css-in-csharp.md](css-in-csharp.md)

Tracks decisions and open items from the 2026-05-02 session. Each section is a work item with the chosen approach, alternatives considered, and notes for future expansion.

---

## 1. Gradient API (Image union + Multiple combiner)

**Status: DECIDED — Option A for now**

`Gradient` and `GradientStop` already exist in `BrowserApi.Css`. The gap is a thin `Image` union wrapper so `BackgroundImage` accepts gradients directly.

```csharp
// Single gradient
BackgroundImage = Gradient.Linear(CssColor.Red, CssColor.Blue)

// Multiple backgrounds
BackgroundImage = Image.Multiple(
    Gradient.RepeatingLinear(0.Deg, stops...),
    Gradient.RepeatingLinear(90.Deg, stops...))
```

**Implementation:**
- `Image` readonly struct with `ICssValue`, implicit conversions from `Gradient` and `UrlValue`
- `Image.Multiple(params Image[])` for comma-separated multi-background
- Update `BackgroundImage` property type from `ICssValue` to `Image`

**Future expansion (noted, not blocked):**
- **Naming coherence:** `Transition.Combine()` vs `Image.Multiple()` — pick one naming pattern and align both. `Combine` or `Multiple` or something else.
- **Option B (`+` operator):** `Gradient.Linear(...) + Gradient.Radial(...)` — consider adding alongside `Multiple()` for terse composition. Same pattern as `ClassList`.
- **Option C (same property, collection):** Investigate whether `BackgroundImage` could accept both a single `Image` and an `Image[]` via overloaded init setters or implicit collection conversion. Would be ideal UX if it compiles cleanly.
- **Option D (Background shorthand):** CSS `background` accepts images, not just colors. A `Background` property union of `CssColor | Image` would let `Background = Gradient.Linear(...)` work. Worth considering alongside the existing `Background = CssColor` path — would need a `BackgroundValue` union wrapper.

---

## 2. BackgroundPosition / TransformOrigin

**Status: EXPLORING — Sides-parallel approach**

Both properties share the same value shape: keyword(s) and/or length/percentage offsets.

**Real-world CSS usage:**
```css
background-position: center;                    /* single keyword */
background-position: top right;                 /* two keywords */
background-position: 50% 50%;                   /* percentage pair */
background-position: 10px 20px;                 /* length pair */
background-position: right 10px bottom 20px;    /* edge + offset (rare) */
```

`transform-origin` is structurally identical.

**Sides parallel:** The existing `Sides` type accepts `Length` singles, 2-tuples `(vertical, horizontal)`, and 4-tuples. `BackgroundPosition` could follow the same pattern for the numeric cases — a `PositionValue` struct with implicit conversions from `LengthOrPercentage` (centered on that axis) and `(LengthOrPercentage, LengthOrPercentage)` tuples.

**Open question:** How to blend keywords and tuples. Options:
- Keywords as static properties on the struct: `PositionValue.Center`, `PositionValue.TopRight`, etc.
- Keywords as an enum with implicit conversion to `PositionValue`
- Tuple + keyword in the same expression: `(PositionValue.Right, 10.Px)` — needs design work

Both `BackgroundPosition` and `TransformOrigin` would use the same `PositionValue` type.

---

## 3. ContainerName / Container shorthand

**Status: DECIDED — string name + tuple shorthand**

Container names are user-defined identifiers — no finite set of keywords. String is correct here.

```csharp
// container-name stays string
ContainerName = "sidebar",

// container shorthand: tuple of (name, type)
Container = ("sidebar", ContainerType.InlineSize),
```

**Implementation:**
- `ContainerName` stays `string` (already done)
- `Container` shorthand: accept a `(string, ContainerType)` tuple via implicit conversion to a small `ContainerValue` struct that serializes to `name / type`

---

## 4. CssVar<CssColor> color extensions

**Status: DECIDED — extension methods restricted to CssVar<CssColor>**

Kills the ugly `((CssColor)Primary).WithAlpha(0.10)` cast in every stylesheet.

```csharp
// C# 14 extension block
public static extension CssVarColorExtensions for CssVar<CssColor> {
    public CssColor WithAlpha(double alpha) => ((CssColor)this).WithAlpha(alpha);
    public CssColor Darken(double percent) => ((CssColor)this).Darken(percent);
    public CssColor Lighten(double percent) => ((CssColor)this).Lighten(percent);
    public CssColor Saturate(double percent) => ((CssColor)this).Saturate(percent);
    public CssColor Desaturate(double percent) => ((CssColor)this).Desaturate(percent);
    public CssColor AdjustHue(double degrees) => ((CssColor)this).AdjustHue(degrees);
    public CssColor Mix(CssColor other, double weight) => ((CssColor)this).Mix(other, weight);
}
```

Only `CssVar<CssColor>` gets these — `CssVar<Length>` etc. do not.

---

## 5. ExternalCssGenerator — type inference

**Status: DECIDED — heuristic + MSBuild overrides (A+B)**

MudBlazor ships 214 custom properties with zero `@property` declarations. No type metadata available.

**Approach:**
1. **Naming-convention heuristics** built into the generator:
   - `*-palette-*`, `*-color*` → `CssVar<CssColor>`
   - `*-width*`, `*-height*`, `*-radius*`, `*-size*` → `CssVar<Length>`
   - `*-elevation-*` → `CssVar<Shadow>` (or string)
   - Everything else → `CssVar<string>` (safe default)

2. **MSBuild metadata overrides** for exceptions:
   ```xml
   <ExternalCss ...
       TypeMappings="--mud-default-borderradius=Length;--mud-elevation-*=Shadow" />
   ```

3. **Community config files** (future): a `configs/` directory in the repo where contributors add library-specific rule files (e.g., `mudblazor.json`, `bootstrap.json`) with naming patterns and one-off exceptions. The generator reads these when the library is detected. Pattern:
   ```json
   {
     "library": "MudBlazor",
     "prefix": "mud-",
     "rules": [
       { "pattern": "palette-*", "type": "CssColor" },
       { "pattern": "*-width*", "type": "Length" }
     ],
     "overrides": {
       "--mud-default-borderradius": "Length"
     }
   }
   ```

---

## 6. Remaining string-typed properties (13 left)

Five simple enums shipped (UserSelect, TextOverflow, PointerEvents, BackgroundRepeat, ContainerType). Remaining:

| Property | Difficulty | Approach |
|---|---|---|
| BackgroundSize | Medium | Enum (Cover/Contain/Auto) + LengthOrPercentage union |
| BackgroundPosition | Medium | PositionValue struct (see §2) |
| TransformOrigin | Medium | PositionValue struct (see §2) |
| ContainerName | Keep string | User-defined identifier — string is correct |
| Container | Small | Tuple `(string, ContainerType)` (see §3) |
| FontFamily | Medium | Enum for generics + `FontFamily.Of("name")` factory + stack combiner |
| Transform | Hard | Builder: `Transform.Rotate(45.Deg) + Transform.Scale(1.2)` |
| Animation | Hard | Builder referencing Keyframes + duration + easing |
| Filter / BackdropFilter | Hard | Builder: `Filter.Blur(4.Px) + Filter.Brightness(0.8)` |
| GridArea / GridColumn / GridRow | Medium | Grid placement values — `span N`, `N / M`, named lines |

---

## 7. Transition property names

**Status: CONSIDERING — Option B (static shortcuts)**

`Transition.For("opacity", 150.Ms)` has a string literal for the property name.

Preferred approach: static factory methods for the ~20 commonly transitioned properties:
```csharp
Transition.Opacity(150.Ms)
Transition.Background(150.Ms, Easing.EaseInOut)
Transition.Transform(300.Ms)
Transition.BorderColor(150.Ms)
// string fallback for rare properties
Transition.For("custom-property", 150.Ms)
```

---

## 8. Analyzers

**Status: DOCUMENTED — no urgency**

BCA001-003 are shipped. BCA004 and future ideas are well-documented in [css-in-csharp.md §35](css-in-csharp.md). No critical gaps — add analyzers when real usage shows which mistakes are most common.

---

## 9. XML documentation — MDN-sourced hover info

**Status: DECIDED — next milestone after property surface gaps close**

**Goal:** Every CSS property, value enum, and method in the authoring API has hover info at least as good as:

```xml
/// <summary>The CSS <c>user-select</c> property controls whether the user
/// can select text within an element.</summary>
/// <param name="value">
/// <see cref="UserSelect.None"/> — disables selection entirely.<br/>
/// <see cref="UserSelect.Text"/> — allows text selection.<br/>
/// <see cref="UserSelect.All"/> — clicking selects the entire element at once.
/// </param>
/// <remarks>
/// <para><b>CSS-in-C# note:</b> In standard CSS this accepts a string keyword;
/// here it's a typed <see cref="UserSelect"/> enum.</para>
/// <para><b>MDN:</b> The user-select CSS property controls whether the user can
/// select text. This doesn't have any effect on content loaded as part of a
/// browser's user interface (its chrome), except in textboxes.
/// <a href="https://developer.mozilla.org/en-US/docs/Web/CSS/user-select">
/// MDN reference</a></para>
/// </remarks>
```

**Layered structure per property:**
1. `<summary>` — one-sentence description (ours, concise)
2. `<param>`/cref section — typed C# values with `<see cref="..."/>` (ours)
3. CSS-in-C# note — what's special about this property in our system (ours)
4. `<remarks>` MDN section — extracted from MDN content (auto-generated)
5. MDN link — always present

**Implementation approach:**
- `[CssProperty("user-select", Mdn = "...")]` attribute on each property for tool metadata
- MDN content sourced from `github.com/mdn/content` (structured markdown, extremely stable)
- Auto-generation tool downloads MDN markdown → extracts summary → generates `docs/xml/css-properties.xml`
- Properties use `<include file="..." path="..."/>` for the MDN portion, or inline for simpler cases
- The `<summary>`, `<param>` crefs, and CSS-in-C# notes are hand-written — that's our value-add
- Cases that don't map 1:1 (Sides, Border.Solid, tuples) get custom docs explaining the C# shape

**Milestone definition:** Every public member on `Declarations`, every keyword enum value, every factory method on `Gradient`/`Transition`/`Shadow`/`Border`/`GridTemplate`/`CssVar` has the full layered doc structure above.

---

## 10. How-to-write-a-stylesheet guide

**Status: DEFERRED — come back after property surface and docs milestone**

Single focused doc combining: three-place wiring, core patterns, using-alias block, audit checklist. Blocked on having the complete property surface and docs in place first.
