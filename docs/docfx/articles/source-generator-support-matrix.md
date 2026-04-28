# JS Module Source Generator — Support Matrix

This is the full reference for what the typed parser does with each TypeScript construct, applied uniformly to both `.ts` and `.d.ts` source files. The [main article](source-generator.md) gives the high-level picture; this page is the one to scan when you want to know whether a specific pattern is supported, what it maps to, and whether the generator will emit a `BAPI002` warning.

Every entry is grouped by category. For each, you get a small TypeScript sample, the corresponding C# output, and a short explanation of *why*. When an entry falls back to `object`, the table tells you whether that's intentional (no diagnostic) or a known limitation (the generator emits `BAPI002` so you see it in your build log).

**Source file support.** Preview.7 onwards, the same typed parser reads both `.ts` and `.d.ts`. For `.ts` it skips function bodies automatically (so you don't need a `tsc --emitDeclarationOnly` step). Priority when both exist for the same module stem: `.d.ts` > `.ts` > `.js` (JSDoc-only fallback).

---

## Primitives

| TypeScript | C# |
|---|---|
| `string` | `string` |
| `number` | `double` |
| `boolean` | `bool` |
| `void` | `void` (method returns `Task`) |
| `undefined` | treated as `void` for return types |
| `any` | `object` |
| `null` | `object` |
| `never` | `void` |

**Why `number` → `double`.** JavaScript numbers are IEEE-754 double-precision floats. There is no integer/float distinction on the JS side. Mapping to `double` preserves the full range and precision; mapping to `int` would silently truncate any value over 2^31. If you know a value is always an integer, use a [width-alias annotation](#numeric-width-aliases) — that's exactly what they're for.

**Why `any` and `null` → `object` without a warning.** These are intentional fallbacks. The consumer has explicitly chosen the loosest typing. Emitting `BAPI002` would be noise.

---

## Numeric width aliases

The default `number` → `double` mapping is safe but lossy: every `int`, `long`, or `float` on the C# side gets a `double` it has to cast back. Width aliases let TypeScript authors annotate intent and have the generator emit the matching .NET primitive directly.

| TypeScript | C# | Range |
|---|---|---|
| `int` | `int` | `[-2³¹, 2³¹ − 1]` |
| `uint` | `uint` | `[0, 2³² − 1]` |
| `long` | `long` | `[-2⁶³, 2⁶³ − 1]` (range caveat below) |
| `ulong` | `ulong` | `[0, 2⁶⁴ − 1]` (range caveat below) |
| `short` | `short` | `[-2¹⁵, 2¹⁵ − 1]` |
| `ushort` | `ushort` | `[0, 2¹⁶ − 1]` |
| `byte` | `byte` | `[0, 255]` |
| `sbyte` | `sbyte` | `[-128, 127]` |
| `float` | `float` | IEEE-754 single precision |
| `Guid` | `System.Guid` | canonical 8-4-4-4-12 hyphenated string |

**How they work.** The aliases are declared in the ambient `browserapi.d.ts` shipped with the package — `declare type int = number;` and so on. To TypeScript they're synonyms for `number` (or `string`, in `Guid`'s case), so the JS runtime is unchanged. The generator pattern-matches the alias *name* (it doesn't follow `type` declarations), and JSON serialization is lossless: `System.Text.Json` reads `42` straight into `int`, `1.5` into `float`, and `"550e8400-e29b-41d4-a716-446655440000"` straight into `System.Guid`.

**Range caveat for 64-bit aliases.** JavaScript `number` only safely represents integers up to 2⁵³ − 1 (`Number.MAX_SAFE_INTEGER`). For values that genuinely need the full 64-bit range, use `BigInt` on the JS side and serialize as a `string` on the wire — then parse to `long` / `ulong` yourself on the .NET side. The `long` and `ulong` aliases are still useful below the 2⁵³ ceiling (timestamps in ms, large counters, database row IDs), where the round-trip is exact.

**Wiring up the ambient declarations.** The `BrowserApi.SourceGen` package ships `browserapi.d.ts` and copies it into `obj/browserapi-types/` at build time. To make the aliases visible to your TypeScript compiler, add the path to your `tsconfig.json`:

```json
{
  "include": [
    "wwwroot/js/**/*.ts",
    "obj/browserapi-types/**/*.d.ts"
  ]
}
```

The `obj/` folder is gitignored by the standard .NET project template, so the file doesn't pollute your git history. If your project doesn't use a `tsconfig.json` (plain `.js` workflow), the aliases aren't usable — but `number` and `string` still map the way they always have.

**Example.**

```typescript
// .ts — width-typed signatures
export function createTracker(ref: DotNetObjectReference, intervalMs: int): int;
export function loadEntity(id: Guid): Promise<EntityConfig>;
export function setOpacity(value: float): void;

export interface EntityConfig {
    id: Guid;
    sequenceNumber: long;
    flags: byte;
}
```

```csharp
// generated C# — primitives match the alias, no casts needed
public Task<int> CreateTrackerAsync<TDotNetRef>(
    DotNetObjectReference<TDotNetRef> @ref, int intervalMs) where TDotNetRef : class { … }

public Task<EntityConfig> LoadEntityAsync(System.Guid id) { … }
public Task SetOpacityAsync(float value) { … }

public sealed class EntityConfig {
    public required System.Guid Id { get; init; }
    public required long SequenceNumber { get; init; }
    public required byte Flags { get; init; }
}
```

---

## Containers

| TypeScript | C# |
|---|---|
| `T[]` | `T[]` |
| `Array<T>` | `T[]` |
| `Record<string, T>` | `System.Collections.Generic.Dictionary<string, T>` |
| `Promise<T>` | unwrapped to `T` (method returns `Task<T>`) |
| `T \| null`, `T \| undefined` in property position | `T?` |

**Why `Promise<T>` is unwrapped.** Every generated method is already `async Task<T>`. Keeping the `Promise<...>` wrapper in the C# signature would mean `Task<Task<T>>` at the call site — nonsense. The generator strips it during mapping.

**Why `Record<string, T>` and not `IReadOnlyDictionary`.** JSON deserialization into a concrete `Dictionary<,>` just works. An interface type would need a custom converter. Can change later if it becomes a demand.

**Arrays of complex types.** `DragConfig[]`, `Array<Behavior>`, and nested forms like `Record<string, Behavior>` are all resolved recursively. If any element type is unrecognized, that specific element position falls back to `object` and emits `BAPI002` — the array wrapper itself is fine.

---

## Interfaces

| TypeScript | C# |
|---|---|
| `export interface Foo { ... }` | `public sealed class Foo` with `[JsonPropertyName]` per property |
| `interface Foo { ... }` (no `export`) | Same as above |
| Interface referenced from a function parameter | the sealed class |
| Interface referenced from a property | the sealed class |

**Exported and non-exported interfaces both become records.** TypeScript's `export` keyword controls module-level `import` visibility; it has no bearing on the JSON shape that crosses the JS/C# boundary. The generator treats both the same way. This was fixed in `0.1.0-preview.3` after a brief period when only exported interfaces were recognized.

**Property names are PascalCased for C# and re-tagged with `[JsonPropertyName]`** so they round-trip through `System.Text.Json` using the original casing on the wire.

**Cross-file references are not resolved.** If `moduleA.d.ts` references `SharedType` declared in `moduleB.d.ts`, the parser in `moduleA` has never seen `SharedType` and falls back to `object` with a `BAPI002` warning. Work around it by redeclaring the type in each `.d.ts` that needs it. Each `.d.ts` is its own world — keeping it that way keeps the generator simple and its behavior predictable.

---

## String literal unions → enums

| TypeScript | C# |
|---|---|
| `'a' \| 'b' \| 'c'` as a property type | `enum` named `<InterfaceName><PropertyName>` with `[JsonStringEnumMemberName("a")]` per member, decorated with `[JsonConverter(typeof(JsonStringEnumConverter<...>))]` |
| `'kebab-case'` values | PascalCased to `KebabCase`; original preserved via `[JsonStringEnumMemberName]` |

**Why enums instead of `string`.** A plain `string` loses the invariant that only certain values are valid. An enum gives compile-time enforcement and IntelliSense. JSON round-tripping is handled by the attributes — values serialize to the original TypeScript literal.

**Why the generated name is `<Interface><Property>`.** Unions are usually specific to one property, so the name couples them. If the same union appears on two properties of two different interfaces, they produce two distinct enum types — that's a minor footgun we can address later by deduplicating equal unions across a file if it becomes a pattern.

---

## Blazor interop types

### `DotNetObjectReference` as a direct function parameter

The ambient `browserapi.d.ts` (see [Numeric width aliases](#numeric-width-aliases) for the wiring) declares a fully-typed `DotNetObjectReference` interface with `invokeMethodAsync<TResult>` and `dispose()`. Once the ambient file is in your tsconfig `include`, you can reference the type directly — no per-module stub redeclaration needed.

```typescript
// .ts — no local stub, the ambient declaration provides the type
export function createDrag(dotNetRef: DotNetObjectReference, config: DragConfig): number;
```

If you're not using the ambient declaration, the legacy stub form still works:

```typescript
// .d.ts — the stub declaration satisfies TypeScript
interface DotNetObjectReference {}

export function createDrag(dotNetRef: DotNetObjectReference, config: DragConfig): number;
```

```csharp
// Generated — the method is generic; T is inferred at each call site
// (`using Microsoft.JSInterop;` is in the file header, so DotNetObjectReference is unqualified)
public async System.Threading.Tasks.Task<double> CreateDragAsync<
    [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
        System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
    TDotNetRef>(
    DotNetObjectReference<TDotNetRef> dotNetRef,
    DragConfig config) where TDotNetRef : class { ... }
```

**Why generic over `TDotNetRef` instead of typed as `object`.** `DotNetObjectReference<T>` is a sealed generic class; the non-generic `DotNetObjectReference` is a static factory and cannot be used as a parameter type (that was preview.4's bug — CS0721). We also can't pick a concrete `T` from the `.d.ts` because `T` lives in the consumer's assembly, not TypeScript. The compromise: make the *method* generic, let the consumer's argument type drive inference. `var r = DotNetObjectReference.Create(this); await m.CreateDragAsync(r, cfg);` — `TDotNetRef` resolves to whatever class `this` is.

**Why the stub `interface DotNetObjectReference {}` is recognized and skipped.** TypeScript requires some declaration for any name used in a signature. If the generator emitted a `JsModules.DotNetObjectReference` class for the stub, it would collide with `Microsoft.JSInterop.DotNetObjectReference` in any consumer file that had `using JsModules;` in scope — a pre-preview.4 bug. The skip-list treats it as a Blazor interop primitive, not a consumer shape. The ambient `browserapi.d.ts` (preview.8 onwards) provides a richer typed declaration with the same behavior — the skip-list applies to either form.

**Why the `DynamicallyAccessedMembers(PublicMethods)` attribute.** `DotNetObjectReference<TValue>` itself carries this annotation, which the AOT/trimmer uses to preserve the target object's `[JSInvokable]` methods. When our generated method forwards `TDotNetRef` to `DotNetObjectReference<TDotNetRef>`, without the matching annotation the trimmer emits `IL2091`. With it, Blazor WebAssembly AOT builds stay quiet.

**Multiple `DotNetObjectReference` parameters in one function.** Each gets its own type parameter (`TDotNetRef`, `TDotNetRef1`, `TDotNetRef2`, …), independently inferred per-argument. The consumer can pass references to different classes; the C# compiler doesn't try to unify them.

### `DotNetObjectReference` in other positions — fallback to `object`

| Where it appears | C# output | Diagnostic |
|---|---|---|
| As an interface property | `object` | none (intentional) |
| As a return type | `object` | none (intentional) |
| Inside `Array<...>`, `Record<string, ...>`, or any nested container | `object` | none (intentional) |

**Why these cases fall back.** The generic-method trick only works when `DotNetObjectReference` is a *top-level parameter* — that's the only position where C# can infer `T` from a call-site argument. In other positions (record property, array element, return type) we'd need to promote the containing type to generic too, which `.d.ts` can't convey. The generator doesn't emit `BAPI002` for these because they're a known, documented fallback — not silent degradation of an unrecognized type.

In practice this is fine: `DotNetObjectReference` is almost always passed as a direct argument when wiring up a JS ↔ .NET callback. The other positions are rare.

---

## Nullability / optionality

| TypeScript | C# |
|---|---|
| `foo?: string` in a property | `string? Foo` |
| `foo?: number` in a property | `double? Foo` |
| `foo?: CustomType` in a property | `CustomType? Foo` (adds `?` if missing) |
| `foo?: Type` as a function parameter | `Type? foo` |
| `foo?: DotNetObjectReference` | `DotNetObjectReference<TDotNetRef>? foo` (generic method, nullable generic param) |

**Why `?` only on value-producing positions.** Generated records use `required` for non-optional properties and plain `get; init;` for optional ones. The nullable `?` conveys the JSON-shape contract both to the C# compiler and to `System.Text.Json` — deserialization sees a missing key and sets the property to `null` / default without complaint.

---

## JSDoc → C# XML documentation

JSDoc comments in your `.ts` / `.d.ts` flow through to C# XML docs so IntelliSense shows the same text a TypeScript editor would. Three positions are recognized:

| JSDoc position | C# output |
|---|---|
| Leading text on an `export function` | `/// <summary>` on the generated async method |
| `@param name - text` on a function | `/// <param name="…">` on the matching C# parameter |
| `@returns text` on a function | `/// <returns>` on the method |
| Leading text on an `interface` declaration | `/// <summary>` on the generated sealed class |
| Leading text on an interface property | `/// <summary>` on the generated property |

**Example round-trip:**

```typescript
/** Configuration for a drag context. */
export interface DragConfig {
    /** CSS selector for the container. */
    container: string;
}
```

```csharp
/// <summary>Configuration for a drag context.</summary>
/// <remarks>Generated from the TypeScript interface <c>DragConfig</c>.</remarks>
public sealed class DragConfig {
    /// <summary>CSS selector for the container.</summary>
    [JsonPropertyName("container")]
    public required string Container { get; init; }
}
```

The `<remarks>` tag is added automatically whenever the interface has a JSDoc summary — it preserves the "this was generated from `X`" breadcrumb without losing the author's description.

**How the summary text is extracted.** The parser takes everything between `/**` and the first `@tag` line. Multi-line JSDoc is collapsed to a single space-joined string — good for `<summary>` which expects one paragraph. Lines starting with the canonical `*` prefix have it stripped automatically. Content after the first `@tag` is either used by the tagged-section parser (for functions, `@param` / `@returns`) or ignored (for properties — no per-property tag support today).

**No JSDoc?** The generator falls back to a descriptive boilerplate: `Maps to TypeScript property <c>foo</c>` for properties, `Generated from the TypeScript interface <c>Foo</c>` for interfaces. Consumers still get *some* documentation, never an empty `<summary>`.

**Attachment is strict.** A JSDoc block attaches only to the declaration *immediately* below it (whitespace-only separation). A non-JSDoc comment or another declaration between them breaks the attachment — that prevents us from accidentally leaking someone else's docs onto a later member.

---

## Diagnostics

### `BAPI002` — Unknown TypeScript type

Emitted when the parser meets a type it cannot map. The message identifies the exact function, parameter (or interface + property), and the TypeScript type text that couldn't be resolved.

```
BAPI002: Unknown TypeScript type 'FancyGeneric<T>' at createDrag(config) — falling back to 'object'.
```

Causes that trigger `BAPI002`:

- **Complex generics** other than the supported short list (`Array<T>`, `Promise<T>`, `Record<string, T>`). E.g. `Foo<T extends Bar>`, conditional types, mapped types, and type aliases built from those.
- **Intersection types** like `A & B`. A single interface that lists all members is better for interop anyway.
- **Cross-file interface references.** If you reference a type declared in a different `.d.ts` file, the parser can't find it and falls back.
- **Typos and simple typos in type names.** Same code path as "unknown reference" — you'll see the typo in the warning message.

### Intentional mappings that do NOT emit `BAPI002`

These are fallbacks by design, not silent failures. No warning is emitted:

- `any` → `object`
- `null` → `object`
- `DotNetObjectReference` in non-direct-param positions → `object`

---

## Unsupported / not recognized

These patterns don't get parsed at all — the generator ignores them silently because there's no meaningful C# mapping. If you need one of these, a small named re-export in your `.d.ts` / `.js` is usually the cleanest workaround.

### Function declaration forms

- **`export default function ...`** — not picked up. Blazor JS modules call named functions by string (`module.InvokeAsync("foo", ...)`), so default exports don't fit the dispatch model. Workaround: export under a named alias — `export function foo(...) { ... }`.
- **`export const foo = (...) => ...`** (arrow function exports) — not picked up. Same reason: the parser keys on `export function ...` declarations specifically.
- **Class methods** — not picked up. A class instance can't be invoked as a module-level function by name. If you have a JavaScript class, expose its methods as named module-level functions and delegate to an instance.

### TypeScript features

- **Type aliases** (`type Foo = ...`) — not expanded. Use an `interface` for shape declarations that should become C# records.
- **Conditional types** (`T extends U ? X : Y`) — not evaluated.
- **Mapped types** (`{ [K in keyof T]: ... }`) — not expanded.
- **Namespaces** (`namespace N { ... }`) — not recognized. Declare everything at the file's top level.
- **Module re-exports** (`export * from '...'`) — not followed. Each `.d.ts` is parsed in isolation.

---

## What to do when you hit a limitation

The generator is meant to cover the common shape of hand-written interop `.d.ts` files — not to be a full TypeScript compiler. When you hit something on this page that doesn't work for you, the usual recipes:

1. **For complex generic / conditional / mapped types**: declare a simpler `interface` in the `.d.ts` that matches the JSON shape on the wire. Keep the fancy TypeScript for your JS code's own internal types if you want; give the interop boundary a plain interface.
2. **For intersection types**: flatten into a single `interface` that lists all members directly. Same JSON shape, clearer on both sides.
3. **For cross-file references**: redeclare the type in each `.d.ts` that needs it, or move both modules into one file. The duplication is explicit and easy to audit.
4. **For default / arrow / class exports**: add a named function re-export that forwards to them.
5. **For a pattern that's none of the above and you think should work**: open an issue with the `.d.ts` snippet, the expected C# output, and why it matters. The limits are mostly conservative, not structural — if there's a clean mapping we missed, we'll add it.
