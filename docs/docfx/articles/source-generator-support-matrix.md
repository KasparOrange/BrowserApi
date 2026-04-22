# JS Module Source Generator — Support Matrix

This is the full reference for what the `.d.ts` parser does with each TypeScript construct. The [main article](source-generator.md) gives the high-level picture; this page is the one to scan when you want to know whether a specific pattern is supported, what it maps to, and whether the generator will emit a `BAPI002` warning.

Every entry is grouped by category. For each, you get a small TypeScript sample, the corresponding C# output, and a short explanation of *why*. When an entry falls back to `object`, the table tells you whether that's intentional (no diagnostic) or a known limitation (the generator emits `BAPI002` so you see it in your build log).

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

**Why `number` → `double`.** JavaScript numbers are IEEE-754 double-precision floats. There is no integer/float distinction on the JS side. Mapping to `double` preserves the full range and precision; mapping to `int` would silently truncate any value over 2^31. If you know a value is always an integer, you can `[JSInvokable]` a method on a C# class whose signature uses `int` — but the generator can't assume that from a plain `.d.ts`.

**Why `any` and `null` → `object` without a warning.** These are intentional fallbacks. The consumer has explicitly chosen the loosest typing. Emitting `BAPI002` would be noise.

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

```typescript
// .d.ts — the stub declaration satisfies TypeScript
interface DotNetObjectReference {}

export function createDrag(dotNetRef: DotNetObjectReference, config: DragConfig): number;
```

```csharp
// Generated — the method is generic; T is inferred at each call site
public async System.Threading.Tasks.Task<double> CreateDragAsync<
    [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
        System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
    TDotNetRef>(
    Microsoft.JSInterop.DotNetObjectReference<TDotNetRef> dotNetRef,
    DragConfig config) where TDotNetRef : class { ... }
```

**Why generic over `TDotNetRef` instead of typed as `object`.** `DotNetObjectReference<T>` is a sealed generic class; the non-generic `DotNetObjectReference` is a static factory and cannot be used as a parameter type (that was preview.4's bug — CS0721). We also can't pick a concrete `T` from the `.d.ts` because `T` lives in the consumer's assembly, not TypeScript. The compromise: make the *method* generic, let the consumer's argument type drive inference. `var r = DotNetObjectReference.Create(this); await m.CreateDragAsync(r, cfg);` — `TDotNetRef` resolves to whatever class `this` is.

**Why the stub `interface DotNetObjectReference {}` is recognized and skipped.** TypeScript requires some declaration for any name used in a signature. If the generator emitted a `JsModules.DotNetObjectReference` class for the stub, it would collide with `Microsoft.JSInterop.DotNetObjectReference` in any consumer file that had `using JsModules;` in scope — a pre-preview.4 bug. The skip-list treats it as a Blazor interop primitive, not a consumer shape.

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
