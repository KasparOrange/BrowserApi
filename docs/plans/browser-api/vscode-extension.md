# VS Code Extension — `BrowserApi Tools`

**Parent:** [browser-api.md](browser-api.md)
**Status:** Exploration / feature survey. Nothing implemented.

## Purpose

A VS Code extension that improves the authoring experience for code that uses BrowserApi — specifically CSS-in-C#, generated browser types, and the upcoming asset/class-name source generators. The extension is **not** a replacement for the Roslyn side (analyzers, code fixes, source generators) — it sits on top of it and adds things Roslyn cannot do: editor-only visuals, decorations, file icons, webview previews, and VS Code-specific UX.

## Guiding Principle

Prefer Roslyn over VS Code whenever possible.

- Roslyn analyzers / code fixes run in **every** IDE that understands C# (Rider, Visual Studio, VS Code, MonoDevelop, CLI `dotnet build`).
- A VS Code extension only runs in VS Code.
- Therefore: use the extension only for things Roslyn *cannot* do — pixel-level editor visuals, file-tree decoration, webviews, custom commands, integrations with external tools.

Everything that is "pure code intelligence" (diagnostics, refactorings, completion of identifiers, renames across symbols) belongs in Roslyn.

## Extension Anatomy (background)

A VS Code extension is a TypeScript/JavaScript project with a `package.json` manifest that declares **contribution points** (things the extension adds to VS Code) and an `extension.ts` entry point (runtime code). There is no compiler magic — the API is just plain function calls. The hard part is picking the right contribution point, not the coding.

Relevant contribution points for this project:

| Contribution | What it does | Runs where |
|--------------|--------------|-----------|
| `iconThemes` | Full file-icon theme (all-or-nothing) | UI |
| `fileDecorationProvider` | Badges/tints on files next to any icon theme | Runtime API |
| `grammars` (+ `injectTo`) | TextMate highlighting, including injection into other languages | UI |
| `semanticTokens` | Token-level coloring driven by a language server | Runtime API |
| `decorations` | Arbitrary per-range visuals in editor (swatches, inline text) | Runtime API |
| `codeLens` | Clickable lines above code | Runtime API |
| `hover` | Rich hover cards | Runtime API |
| `codeActions` | Lightbulb quick-fixes & refactorings | Runtime API |
| `completionProvider` | Autocomplete entries | Runtime API |
| `commands` | Command palette entries + keybindings | Runtime API |
| `tasks` | Contribute build/regen tasks | UI |
| `views` / `viewsContainers` | Sidebar tree views | UI |
| `webviewPanels` | Embedded HTML preview panes | Runtime API |
| `languages.configuration` | File extension mapping, comment chars, brackets | UI |
| `notebooks` | Custom editors for `.ipynb`-like files | Runtime API |

Cost frame: a minimal extension (1–2 contributions) is a **weekend project**. A serious one (5+ contributions + language features) is a **1–2 week project**. None of this is research-risk work — the APIs are stable and well-documented.

---

## Feature Inventory

Legend:

- **Difficulty** — 1 = hours, 2 = a day, 3 = a week, 4 = multiple weeks, 5 = research project
- **Usefulness** — 1 = nice-to-have, 3 = useful, 5 = game-changer
- **Where** — `Roslyn` (do it in analyzer/source-gen instead), `VSCode` (needs the extension), `Both` (layered)

### 1. File visuals

| # | Feature | Difficulty | Usefulness | Where | Notes |
|---|---------|:---:|:---:|:---:|-------|
| 1.1 | Custom icon for `.css.cs` files | 2 | 3 | VSCode | Requires an Icon Theme contribution — all-or-nothing. Users must switch to ours. Low adoption. |
| 1.2 | Badge / color tint on `.css.cs` files | 1 | 3 | VSCode | `FileDecorationProvider` API. Works on top of ANY icon theme. Our actual answer to "custom icon per extension." |
| 1.3 | Badge on files containing a specific attribute (e.g. `[GeneratedCss]`) | 2 | 2 | VSCode | Same API, but needs to open/scan files. Scale concern. |
| 1.4 | Custom language mode for `.css.cs` (status bar shows "CSS-in-C#") | 1 | 2 | VSCode | `languages.configuration` + a sub-language that extends `csharp`. |

**Recommendation:** do 1.2 and 1.4. Skip 1.1 (bad UX to force an icon theme).

### 2. Syntax highlighting

| # | Feature | Difficulty | Usefulness | Where | Notes |
|---|---------|:---:|:---:|:---:|-------|
| 2.1 | TextMate injection to color CSS-property identifiers inside `Css.Rule(...)` chains | 2 | 3 | VSCode | Regex-based injection. Works without a language server. |
| 2.2 | Embedded CSS inside raw-string literals (`"""…"""`) tagged with a marker | 3 | 4 | VSCode | Grammar detects a marker (attribute, comment, or method shape) and re-enters CSS grammar inside the string. Gives full CSS highlight + outline inside C#. |
| 2.3 | Embedded HTML / SVG inside literal strings (same pattern) | 3 | 3 | VSCode | Extension of 2.2. Useful for Canvas + markup helpers. |
| 2.4 | Semantic tokens for types declared in `BrowserApi.*` namespaces (color them as "browser type") | 4 | 2 | VSCode | Requires our own language server parallel to Roslyn's LSP. High effort, low payoff. |
| 2.5 | Color identifiers like `Color.Red` visually tinted | 2 | 3 | VSCode | Decorations API, not TextMate — driven by Roslyn-like parsing in TS. |

**Recommendation:** 2.1 and 2.2 are the big wins. Skip 2.4 — Roslyn semantic highlighting already handles identifiers reasonably.

### 3. Extending the Microsoft C# extension

You cannot modify it. You *can* coexist — VS Code allows multiple extensions to contribute providers to the same language. The clean extension points are:

| # | Mechanism | Difficulty | Usefulness | Where | Notes |
|---|-----------|:---:|:---:|:---:|-------|
| 3.1 | Roslyn analyzers | 2 | 5 | Roslyn | Picked up automatically by the C# extension. No VS Code code needed. Already on roadmap. |
| 3.2 | Roslyn code fix providers | 2 | 5 | Roslyn | Same. Planned feature #4 (asset rename fixer) goes here. |
| 3.3 | Roslyn source generators | 3 | 5 | Roslyn | Planned features #3, #5, #7. Feed IntelliSense directly. |
| 3.4 | Contribute *additional* hovers, code lenses, completions from the extension | 3 | 3 | VSCode | Runs alongside Omnisharp/Roslyn. Works for BrowserApi-specific hover cards (MDN link, spec text, generated CSS preview) that Roslyn can't produce. |
| 3.5 | Listen to Omnisharp/Roslyn diagnostics and layer extra UI on top | 4 | 2 | VSCode | Fragile — LSP internals. Not recommended. |

**Recommendation:** do almost everything that's "code intelligence" in Roslyn. Use the extension only for 3.4 (and only where we need something Roslyn can't express).

### 4. In-editor previews & decorations

These are the biggest "wow" features and the strongest case for a VS Code extension in the first place — Roslyn cannot render pixels into the editor.

| # | Feature | Difficulty | Usefulness | Where | Notes |
|---|---------|:---:|:---:|:---:|-------|
| 4.1 | Color swatch next to `Color.Rgb(...)`, `Color.Hex(...)`, `Color.Hsl(...)` | 2 | 5 | VSCode | `DocumentColorProvider` API — built into VS Code, same as used in CSS files. Click → color picker. |
| 4.2 | Length/unit preview (`1.5.Rem` → "24px @ 16px root") as inline hint | 2 | 4 | VSCode | `InlayHintsProvider`. |
| 4.3 | Angle preview (`45.Deg` → rotated arrow glyph) | 2 | 2 | VSCode | Decoration with SVG. Cute, low info value. |
| 4.4 | Gradient preview next to `LinearGradient(...)` | 3 | 3 | VSCode | Decoration with a small inline SVG/canvas image. |
| 4.5 | Image thumbnail hover for `Assets.Images.HeroBanner` | 2 | 4 | VSCode | `HoverProvider` returning a markdown image. Pairs with the asset source generator. |
| 4.6 | Font preview on `FontFamily("Arial")` | 3 | 2 | VSCode | Webview snippet or OS font-rendered SVG. |
| 4.7 | "Show generated CSS" CodeLens above a stylesheet class | 3 | 5 | VSCode | CodeLens opens a side-by-side webview showing the compiled CSS output. The killer CSS-in-C# feature. |
| 4.8 | Live rendered preview of an element styled by a CSS-in-C# class (mini browser pane) | 4 | 4 | VSCode | Webview + a headless rendering step. Heavy but demoable. |
| 4.9 | Gutter icon for selectors that match nothing in the current project's HTML/Razor files | 4 | 3 | Both | Needs project-wide HTML scan. Could live in Roslyn (analyzer) — probably should. |

**Recommendation:** 4.1, 4.2, 4.5, 4.7 are high-leverage. Do them first.

### 5. Navigation & spec integration

BrowserApi types carry `href` back to the source spec (see [generator.md](generator.md)). That metadata is a huge enabler for navigation features.

| # | Feature | Difficulty | Usefulness | Where | Notes |
|---|---------|:---:|:---:|:---:|-------|
| 5.1 | Hover on `HTMLDivElement` shows MDN / spec link + excerpt | 2 | 4 | VSCode | Needs a hover provider + a small lookup table. The `href` from webref gives us the URL. |
| 5.2 | "Go to spec" command (right-click → opens MDN) | 1 | 3 | VSCode | Palette command using current symbol. |
| 5.3 | Inline WebIDL source for a generated type (hover shows the raw `interface X {...}`) | 2 | 3 | Both | Could also be an XML doc comment on the generated type — probably cheaper that way (Roslyn). |
| 5.4 | Quick-open "by spec" — type a spec name, see all types from that `.idl` file | 3 | 2 | VSCode | `WorkspaceSymbolProvider`. |
| 5.5 | Sidebar tree view: "Browser APIs" grouped by spec → interface → member | 3 | 2 | VSCode | TreeDataProvider. Nice-to-have; VS Code's own outline covers most of it. |

**Recommendation:** 5.1 and 5.2 are cheap and delightful. 5.3 is better done in Roslyn as an XML doc comment.

### 6. Code actions & refactorings

These almost all belong in Roslyn, not in the extension. Listed here so we have the complete picture.

| # | Feature | Difficulty | Usefulness | Where | Notes |
|---|---------|:---:|:---:|:---:|-------|
| 6.1 | "Convert `Length.Px(16)` → `Length.Rem(1)`" | 2 | 3 | Roslyn | Analyzer + fixer. |
| 6.2 | "Extract declarations to a shared class" | 3 | 3 | Roslyn | Refactoring. |
| 6.3 | "Convert to CSS shorthand" (`padding-top/right/bottom/left` → `Padding(...)`) | 3 | 3 | Roslyn | Analyzer. |
| 6.4 | "Extract to CSS custom property" (`Color.Red` → `var("--accent")` + definition) | 3 | 3 | Roslyn | Refactoring. |
| 6.5 | Suggest closest match when an `Assets.*` reference breaks | 3 | 5 | Roslyn | Planned feature #4. Analyzer + fixer. |
| 6.6 | Suggest closest match when a CSS class name reference breaks | 3 | 4 | Roslyn | Same pattern, for feature #5. |
| 6.7 | "Add `!important`" / "Remove `!important`" | 1 | 2 | Roslyn | Trivial analyzer. |

**Recommendation:** all of these go in Roslyn. The extension has no role here.

### 7. Commands & tooling

| # | Feature | Difficulty | Usefulness | Where | Notes |
|---|---------|:---:|:---:|:---:|-------|
| 7.1 | `BrowserApi: Regenerate Types` command | 1 | 4 | VSCode | Wraps `dotnet run --project src/BrowserApi.Generator`. |
| 7.2 | `BrowserApi: Update Specs from webref` | 1 | 3 | VSCode | Wraps the clone+copy from CLAUDE.md. |
| 7.3 | Task provider so "Regenerate" shows up in `tasks.json` | 2 | 3 | VSCode | Nicer integration than a raw command. |
| 7.4 | Status bar: "BrowserApi · webref @ 2026-04-01 · 2,690 types" | 2 | 2 | VSCode | Informational. |
| 7.5 | Webview "Spec Diff" — show which WebIDL interfaces changed between two `specs/idlparsed/` snapshots | 4 | 3 | VSCode | Useful around spec updates, otherwise dormant. |
| 7.6 | Drag-drop: drop a file from Finder onto editor → inserts `Assets.Images.TheFile` reference | 3 | 4 | VSCode | Needs the asset source generator first. |
| 7.7 | `BrowserApi: New CSS Stylesheet` scaffolds a `.css.cs` file from a template | 1 | 2 | VSCode | Template string + command. |

**Recommendation:** 7.1 is a freebie. 7.6 is high value once the asset generator lands.

### 8. Diagnostics unique to VS Code

Most diagnostics belong in Roslyn. A few only make sense at the editor level:

| # | Feature | Difficulty | Usefulness | Where | Notes |
|---|---------|:---:|:---:|:---:|-------|
| 8.1 | Live preview: as you type a CSS rule, webview re-renders | 4 | 4 | VSCode | Subset of 4.8. |
| 8.2 | "You are using a CSS property not supported in Baseline 2024" warning with caniuse data | 3 | 3 | Both | Can be Roslyn diagnostic if caniuse data is bundled; pairs well with a hover link. |
| 8.3 | Visual gutter when selector specificity is unusually high | 3 | 2 | VSCode | Decoration. Cute but niche. |

### 9. Speculative / long-tail

Features we haven't discussed but are technically reachable:

| # | Feature | Difficulty | Usefulness | Notes |
|---|---------|:---:|:---:|-------|
| 9.1 | Bidirectional sync with a live Blazor dev server — edit CSS-in-C#, see page reflow instantly | 5 | 5 | Hot-reload hook + webview. Serious infra. |
| 9.2 | "Record styles from a running page" — pick an element in a Chromium instance, generate the C# stylesheet that reproduces it | 5 | 4 | Requires a DevTools Protocol bridge. |
| 9.3 | CSS-in-C# formatter (aligns declarations, groups properties) | 3 | 2 | `DocumentFormattingEditProvider`. |
| 9.4 | Snippet pack for common patterns (`css-rule`, `event-handler`, `canvas-path`) | 1 | 2 | Pure JSON contribution. Low effort. |
| 9.5 | Notebook-style scratchpad: C# cell → rendered HTML cell, for experimenting with styles | 5 | 3 | `notebooks` API + kernel. Cool, but basically a second product. |
| 9.6 | Integration with the server-side Jint runtime ([runtime.md](runtime.md)) — run a DOM snippet and see the result inline | 4 | 3 | Requires runtime to be working. |
| 9.7 | Theme: a color theme tuned for CSS-in-C# that highlights units / colors / selectors distinctly | 2 | 2 | Separate extension, ships for free. |

---

## Proposed Phasing

Split into extensions by dependency surface, not by feature. Ship the smallest useful thing first.

### Phase A — Hygiene (1 week, no dependencies)

Minimum viable extension. Useful standalone.

- 1.2 File decoration for `.css.cs`
- 1.4 Language mode
- 4.1 Color swatches + picker
- 4.2 Length unit inlay hints
- 5.1 Hover with MDN/spec link
- 5.2 "Go to spec" command
- 7.1 Regenerate command
- 9.4 Snippet pack

**Value:** meaningful CSS-in-C# authoring UX without shipping anything speculative.

### Phase B — Previews (1–2 weeks, depends on stable CSS-in-C# API)

The "wow" tier — only worth doing after the CSS-in-C# API in [css-in-csharp.md](css-in-csharp.md) is stable enough to commit to identifiers.

- 2.2 Embedded CSS highlighting in raw strings
- 4.4 Gradient preview
- 4.7 "Show generated CSS" webview — **the flagship feature**
- 4.5 Image thumbnails on `Assets.*` (requires asset source gen)
- 7.6 Drag-drop asset insertion (requires asset source gen)

### Phase C — Tooling polish (as-needed)

- 7.3 Task provider
- 7.4 Status bar
- 7.5 Spec diff webview — only around webref update cycles
- 9.3 Formatter

### Never / deferred

- 1.1 Icon theme (hostile UX)
- 2.4 Semantic-token server (not worth the effort next to Roslyn's)
- 3.5 Hooking into Roslyn's diagnostics (fragile)
- 9.1, 9.2, 9.5 — research-scale, not in scope

## Open Questions

- **Ship as one extension or several?** One initially. Split only if adoption of a heavy feature (e.g., live preview) would make the core extension too big.
- **Marketplace name?** `BrowserApi Tools` vs `CSS-in-C#`. Probably `BrowserApi Tools` — broader umbrella.
- **Do we bundle a Rider/Visual Studio story?** Not in the extension. The Roslyn half covers both IDEs for free; only the editor-visual half is VS Code-only, and that's acceptable for the first iteration.
- **MDN data licensing** — MDN content is CC-BY-SA 2.5. We can link and quote; bundling excerpts needs attribution.
- **Does the "Show generated CSS" preview require the project to build?** Almost certainly yes. Cache the last successful build output and show stale-with-warning if the build is failing.

## What NOT to Do

- Do not put diagnostics, analyzers, or refactorings in the extension. They belong in Roslyn so Rider/VS/CLI get them too.
- Do not re-implement Roslyn's semantic highlighting in TypeScript.
- Do not ship an icon theme as the sole way to get the `.css.cs` badge — use file decorations.
- Do not build features that require a live Blazor runtime until the runtime story ([runtime.md](runtime.md)) is solid.
- Do not scrape or bundle MDN content beyond what its license permits.
