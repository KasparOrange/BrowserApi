// browserapi.d.ts — Ambient declarations for BrowserApi.SourceGen.
//
// This file is shipped inside the BrowserApi.SourceGen NuGet package and copied
// into your project at build time. It declares a small set of TypeScript-side
// names that the generator pattern-matches on the C# side — width-typed numeric
// aliases, `Guid`, and the canonical `DotNetObjectReference` interface used for
// Blazor JS interop callbacks.
//
// Why ambient (not `export`): the BrowserApi generator parses TypeScript as text
// and pattern-matches *unqualified* names. For the names below to be visible in
// every consumer module without an `import`, they must live in the global scope —
// which is exactly what `declare type` and `declare interface` (in a `.d.ts`
// without exports) provide. This is the same mechanism `@types/node` and friends
// use to provide global types like `Buffer` or `process`.
//
// Reference docs:
//   https://kasparorange.github.io/BrowserApi/articles/source-generator-support-matrix.html

// ─── Numeric width aliases ────────────────────────────────────────────────────
//
// TypeScript has only one numeric type (`number`, IEEE-754 double). The generator
// has to pick *one* C# type for every `number`, and the safe default is `double`
// — widest range, no precision loss. But most numbers crossing the JS/C# boundary
// are integers (IDs, indices, counters, durations in ms), and on the C# side the
// `double` choice forces a cast at every storage site that wants an integer.
//
// The aliases below let TypeScript authors annotate intent. Each maps to the
// corresponding .NET primitive on the generated side. JSON serialization is
// lossless: `System.Text.Json` reads `42` straight into `int`, `1.5` into `float`,
// and so on. JavaScript runtime values are still doubles — the narrowing happens
// at the C# deserialization boundary.
//
// Range caveat (signed/unsigned 64-bit): JavaScript `number` can only safely
// represent integers up to `Number.MAX_SAFE_INTEGER` (2^53 − 1). For values that
// genuinely need the full 64-bit range, use `BigInt` on the JS side and a `string`
// on the wire (then parse to `long`/`ulong` yourself on the .NET side).

/**
 * 32-bit signed integer. Maps to C# `System.Int32` (`int`) on the generated side.
 *
 * Use for IDs, indices, counts, and any value that fits in `[-2_147_483_648, 2_147_483_647]`.
 * JSON integer literals round-trip cleanly because `System.Text.Json` reads `42` straight
 * into `int` — no custom converter required.
 *
 * @example
 * ```ts
 * export function createTracker(ref: DotNetObjectReference, intervalMs: int): int;
 * // → C#: Task<int> CreateTrackerAsync<TDotNetRef>(DotNetObjectReference<TDotNetRef> ref, int intervalMs)
 * ```
 */
declare type int = number;

/**
 * 32-bit unsigned integer. Maps to C# `System.UInt32` (`uint`) on the generated side.
 *
 * Useful for bit-packed values like RGBA colors, file modes, and any 32-bit field where
 * the sign bit carries meaningful data rather than indicating negative values.
 *
 * @example
 * ```ts
 * export function getPackedColor(): uint;
 * // → C#: Task<uint> GetPackedColorAsync()
 * ```
 */
declare type uint = number;

/**
 * 64-bit signed integer. Maps to C# `System.Int64` (`long`) on the generated side.
 *
 * Use for timestamps in milliseconds (Unix epoch fits safely until year 2038 in `int`,
 * but `long` is the conventional .NET choice), large counters, and database row IDs
 * that may exceed 2^31.
 *
 * **Range caveat:** JavaScript `number` only safely represents integers up to 2^53 − 1.
 * For full 64-bit values, use `BigInt` on the JS side and serialize as a string,
 * then parse to `long` on the .NET side with `long.Parse`.
 *
 * @example
 * ```ts
 * export function getCurrentTicks(): long;
 * // → C#: Task<long> GetCurrentTicksAsync()
 * ```
 */
declare type long = number;

/**
 * 64-bit unsigned integer. Maps to C# `System.UInt64` (`ulong`) on the generated side.
 *
 * Same range caveat as `long`: JavaScript `number` cannot safely represent values above
 * 2^53 − 1. Use `BigInt` + string-on-the-wire for full unsigned 64-bit range.
 */
declare type ulong = number;

/**
 * 16-bit signed integer. Maps to C# `System.Int16` (`short`) on the generated side.
 *
 * Useful for compact value storage (audio samples, small enums, network protocol fields)
 * where memory footprint matters. Range: `[-32_768, 32_767]`.
 */
declare type short = number;

/**
 * 16-bit unsigned integer. Maps to C# `System.UInt16` (`ushort`) on the generated side.
 *
 * Range: `[0, 65_535]`. Common for port numbers, code points, and similar.
 */
declare type ushort = number;

/**
 * 8-bit unsigned integer. Maps to C# `System.Byte` (`byte`) on the generated side.
 *
 * Range: `[0, 255]`. Use for raw byte values, color channels (when not packed into a
 * single `uint`), and binary protocol fields.
 *
 * @example
 * ```ts
 * export function setRgbChannel(channel: byte, value: byte): void;
 * // → C#: Task SetRgbChannelAsync(byte channel, byte value)
 * ```
 */
declare type byte = number;

/**
 * 8-bit signed integer. Maps to C# `System.SByte` (`sbyte`) on the generated side.
 *
 * Range: `[-128, 127]`. Rare in JS interop — included for completeness so the alias
 * table is symmetric across signed/unsigned at every width.
 */
declare type sbyte = number;

/**
 * 32-bit single-precision floating point. Maps to C# `System.Single` (`float`) on the
 * generated side.
 *
 * Useful when the C# side wants `float` for memory or API-shape reasons (graphics
 * coordinates, normalized 0..1 ratios, GPU buffer formats). JavaScript runtime values
 * are still doubles; the narrowing to single precision happens during JSON
 * deserialization on the .NET side.
 *
 * @example
 * ```ts
 * export function setOpacity(value: float): void;
 * // → C#: Task SetOpacityAsync(float value)
 * ```
 */
declare type float = number;

/**
 * GUID / UUID. Maps to C# `System.Guid` on the generated side.
 *
 * Wire format is the canonical 8-4-4-4-12 hyphenated string (lowercase or uppercase,
 * `System.Text.Json` accepts both — e.g. `"550e8400-e29b-41d4-a716-446655440000"`).
 * `System.Text.Json` round-trips strings of this shape directly into `System.Guid`
 * without any custom converter.
 *
 * The alias resolves to `string` on the TypeScript side because that's what crosses
 * the wire — JavaScript has no native UUID type. Annotate as `Guid` (rather than
 * plain `string`) to communicate intent and have the C# side receive a real
 * `System.Guid` instead of a `string`.
 *
 * @example
 * ```ts
 * export function getEntityId(): Guid;
 * export function loadEntity(id: Guid): EntityConfig;
 * // → C#: Task<Guid> GetEntityIdAsync()
 * //       Task<EntityConfig> LoadEntityAsync(System.Guid id)
 * ```
 */
declare type Guid = string;

// ─── Blazor interop ───────────────────────────────────────────────────────────

/**
 * Reference to a .NET object passed from C# to JavaScript via Blazor interop.
 *
 * The C# side creates one with `DotNetObjectReference.Create(this)` and passes it
 * as a parameter into a JS function; the JS side uses {@link invokeMethodAsync} to
 * call back into `[JSInvokable]` methods on the wrapped .NET object.
 *
 * **Generator behavior.** When the BrowserApi source generator sees a parameter of
 * type `DotNetObjectReference` (with or without a type argument) on an exported
 * function, it promotes the generated C# method to *generic over* `TDotNetRef` and
 * emits `Microsoft.JSInterop.DotNetObjectReference<TDotNetRef>` as the parameter
 * type. The consumer's `this` type is inferred at the call site — no explicit type
 * argument needed:
 *
 * ```ts
 * // your-module.ts
 * export function startTracking(ref: DotNetObjectReference, intervalMs: int): int;
 * ```
 *
 * ```csharp
 * // call site
 * var token = await Tracker.StartTrackingAsync(
 *     DotNetObjectReference.Create(this),  // TDotNetRef inferred from `this`
 *     250);
 * ```
 *
 * **Why this declaration ships with the package.** Before this file existed, every
 * consumer module had to redeclare a local stub `interface DotNetObjectReference {
 * invokeMethodAsync(...): Promise<unknown>; }` just to satisfy TypeScript. That
 * stub was duplicated per module, didn't expose `dispose()`, and produced
 * `Promise<unknown>` for every `invokeMethodAsync` call. This canonical declaration
 * removes the duplication and gives JS authors typed return values, IntelliSense,
 * and discoverability of the dispose contract — without touching the generator
 * (the C#-side pattern matching is unchanged).
 *
 * @see {@link https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/call-dotnet-from-javascript} — Microsoft Learn
 */
declare interface DotNetObjectReference {
    /**
     * Invoke a `[JSInvokable]` method on the wrapped .NET object.
     *
     * @typeParam TResult The C# method's return type, projected to TypeScript. Defaults
     *   to `void` — explicitly type the call (`invokeMethodAsync<MyResult>(...)`) when
     *   you want a typed result. Note that complex `TResult` types must be JSON-shape
     *   compatible — the value is round-tripped through `System.Text.Json`.
     * @param methodName Must match a `[JSInvokable]` method name on the .NET object
     *   exactly (case-sensitive). Blazor resolves overloads by parameter *count*, not
     *   parameter type — so two `[JSInvokable]` methods with the same name and same
     *   arity is an error.
     * @param args Positional arguments for the .NET method. Each is JSON-serialized
     *   on the JS side and deserialized into the corresponding C# parameter on the
     *   .NET side. Complex objects must be plain JSON-shape — class instances with
     *   custom serialization need an explicit converter on the .NET side.
     * @returns A promise resolving to the .NET method's return value, deserialized
     *   from JSON into `TResult`. If the .NET method returns `Task` (no value), the
     *   promise resolves to `undefined`.
     */
    invokeMethodAsync<TResult = void>(
        methodName: string,
        ...args: unknown[]
    ): Promise<TResult>;

    /**
     * Release the .NET reference. JavaScript holders should call this in their
     * dispose / teardown paths to allow the underlying .NET object to be garbage-
     * collected.
     *
     * **Why this matters.** Forgetting `dispose()` keeps the .NET reference alive
     * for as long as the JS holder lives — which can cascade into leaking the
     * entire object graph the .NET object holds (subscriptions, child components,
     * cached data). The standard pattern is to call `dispose()` from the same JS
     * function that's invoked by the C# `IAsyncDisposable.DisposeAsync` path
     * (typically a `dispose()` or `destroy()` exported function on your module).
     */
    dispose(): void;
}
