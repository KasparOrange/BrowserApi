using BrowserApi.SourceGen;

namespace BrowserApi.SourceGen.Tests;

public class TsDeclarationParserTests {
    [Fact]
    public void Parse_exported_function_with_typed_params() {
        var dts = "export function greet(name: string, times: number): string;";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Single(result.Functions);
        var func = result.Functions[0];
        Assert.Equal("greet", func.JsName);
        Assert.Equal("GreetAsync", func.CSharpName);
        Assert.Equal("string", func.ReturnType);
        Assert.Equal(2, func.Params.Count);
        Assert.Equal("string", func.Params[0].CSharpType);
        Assert.Equal("double", func.Params[1].CSharpType);
    }

    [Fact]
    public void Parse_void_function() {
        var dts = "export function dispose(): void;";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Single(result.Functions);
        Assert.Null(result.Functions[0].ReturnType);
    }

    [Fact]
    public void Parse_declare_function() {
        var dts = "export declare function doStuff(x: number): boolean;";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Single(result.Functions);
        Assert.Equal("doStuff", result.Functions[0].JsName);
        Assert.Equal("bool", result.Functions[0].ReturnType);
    }

    [Fact]
    public void Parse_interface_to_record() {
        var dts = @"
export interface UserConfig {
    name: string;
    age: number;
    active: boolean;
}";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Single(result.Interfaces);
        var iface = result.Interfaces[0];
        Assert.Equal("UserConfig", iface.TsName);
        Assert.Equal("UserConfig", iface.CSharpName);
        Assert.Equal(3, iface.Properties.Count);

        Assert.Equal("Name", iface.Properties[0].CSharpName);
        Assert.Equal("string", iface.Properties[0].CSharpType);
        Assert.True(iface.Properties[0].IsRequired);

        Assert.Equal("Age", iface.Properties[1].CSharpName);
        Assert.Equal("double", iface.Properties[1].CSharpType);

        Assert.Equal("Active", iface.Properties[2].CSharpName);
        Assert.Equal("bool", iface.Properties[2].CSharpType);
    }

    [Fact]
    public void Parse_optional_properties() {
        var dts = @"
export interface Config {
    required: string;
    optional?: number;
}";
        var result = TsDeclarationParser.Parse(dts);

        var iface = result.Interfaces[0];
        Assert.True(iface.Properties[0].IsRequired);
        Assert.False(iface.Properties[0].IsOptional);
        Assert.Equal("string", iface.Properties[0].CSharpType);

        Assert.False(iface.Properties[1].IsRequired);
        Assert.True(iface.Properties[1].IsOptional);
        Assert.Equal("double?", iface.Properties[1].CSharpType);
    }

    [Fact]
    public void Parse_string_literal_union_generates_enum() {
        var dts = @"
export interface Ghost {
    mode: 'clone' | 'template' | 'label' | 'none';
}";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Single(result.Enums);
        var enumInfo = result.Enums[0];
        Assert.Equal("GhostMode", enumInfo.CSharpName);
        Assert.Equal(4, enumInfo.Members.Count);
        Assert.Equal("clone", enumInfo.Members[0].JsValue);
        Assert.Equal("Clone", enumInfo.Members[0].CSharpName);
        Assert.Equal("template", enumInfo.Members[1].JsValue);
        Assert.Equal("Template", enumInfo.Members[1].CSharpName);

        // The property should use the enum type
        Assert.Equal("GhostMode", result.Interfaces[0].Properties[0].CSharpType);
    }

    [Fact]
    public void Parse_optional_string_literal_union_is_nullable_enum() {
        var dts = @"
export interface Strategy {
    axis?: 'x' | 'y';
}";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Single(result.Enums);
        Assert.Equal("StrategyAxis", result.Enums[0].CSharpName);
        Assert.Equal("StrategyAxis?", result.Interfaces[0].Properties[0].CSharpType);
    }

    [Fact]
    public void Parse_array_type() {
        var dts = @"
export interface Config {
    items: string[];
    numbers: Array<number>;
}";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Equal("string[]", result.Interfaces[0].Properties[0].CSharpType);
        Assert.Equal("double[]", result.Interfaces[0].Properties[1].CSharpType);
    }

    [Fact]
    public void Parse_record_type_to_dictionary() {
        var dts = @"
export interface Config {
    behaviors: Record<string, BehaviorConfig>;
}
export interface BehaviorConfig {
    enabled: boolean;
}";
        var result = TsDeclarationParser.Parse(dts);

        var prop = result.Interfaces[0].Properties[0];
        Assert.Contains("Dictionary<string, BehaviorConfig>", prop.CSharpType);
    }

    [Fact]
    public void Parse_interface_reference_in_property() {
        var dts = @"
export interface Outer {
    inner: InnerConfig;
}
export interface InnerConfig {
    value: number;
}";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Equal(2, result.Interfaces.Count);
        Assert.Equal("InnerConfig", result.Interfaces[0].Properties[0].CSharpType);
    }

    [Fact]
    public void Parse_function_with_interface_param() {
        var dts = @"
export interface DragConfig {
    container: string;
}
export function createDrag(config: DragConfig): number;";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Single(result.Functions);
        Assert.Equal("DragConfig", result.Functions[0].Params[0].CSharpType);
    }

    [Fact]
    public void Parse_promise_return_type_unwrapped() {
        var dts = "export function fetchData(): Promise<string>;";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Equal("string", result.Functions[0].ReturnType);
    }

    [Fact]
    public void Parse_optional_function_param() {
        var dts = "export function greet(name: string, greeting?: string): void;";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Equal("string", result.Functions[0].Params[0].CSharpType);
        Assert.Equal("string?", result.Functions[0].Params[1].CSharpType);
    }

    [Fact]
    public void Parse_jsdoc_summaries_attached_to_functions() {
        var dts = @"
/** Create a new drag context. */
export function createDrag(config: string): number;";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Equal("Create a new drag context.", result.Functions[0].Summary);
    }

    [Fact]
    public void Parse_multiple_functions() {
        var dts = @"
export function foo(): void;
export function bar(x: number): string;
export function baz(a: string, b: boolean): number;";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Equal(3, result.Functions.Count);
        Assert.Equal("foo", result.Functions[0].JsName);
        Assert.Equal("bar", result.Functions[1].JsName);
        Assert.Equal("baz", result.Functions[2].JsName);
    }

    [Fact]
    public void Parse_enum_member_with_hyphen() {
        var dts = @"
export interface Config {
    direction: 'ease-in' | 'ease-out' | 'ease-in-out';
}";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Equal("EaseIn", result.Enums[0].Members[0].CSharpName);
        Assert.Equal("ease-in", result.Enums[0].Members[0].JsValue);
    }

    [Fact]
    public void IsStringLiteralUnion_detects_unions() {
        Assert.True(TsDeclarationParser.IsStringLiteralUnion("'a' | 'b'"));
        Assert.True(TsDeclarationParser.IsStringLiteralUnion("'clone' | 'template' | 'none'"));
        Assert.False(TsDeclarationParser.IsStringLiteralUnion("string"));
        Assert.False(TsDeclarationParser.IsStringLiteralUnion("number | string"));
        Assert.False(TsDeclarationParser.IsStringLiteralUnion("'single'"));
    }

    [Fact]
    public void MapTsType_primitives() {
        var map = new System.Collections.Generic.Dictionary<string, string>();
        Assert.Equal("double", TsDeclarationParser.MapTsType("number", map));
        Assert.Equal("string", TsDeclarationParser.MapTsType("string", map));
        Assert.Equal("bool", TsDeclarationParser.MapTsType("boolean", map));
        Assert.Equal("void", TsDeclarationParser.MapTsType("void", map));
        Assert.Equal("object", TsDeclarationParser.MapTsType("any", map));
    }

    [Fact]
    public void MapTsType_width_aliases_signed_integers() {
        // The ambient `browserapi.d.ts` declares these as `declare type X = number`.
        // The generator pattern-matches the alias names directly — no symbol resolution.
        var map = new System.Collections.Generic.Dictionary<string, string>();
        Assert.Equal("int", TsDeclarationParser.MapTsType("int", map));
        Assert.Equal("long", TsDeclarationParser.MapTsType("long", map));
        Assert.Equal("short", TsDeclarationParser.MapTsType("short", map));
        Assert.Equal("sbyte", TsDeclarationParser.MapTsType("sbyte", map));
    }

    [Fact]
    public void MapTsType_width_aliases_unsigned_integers() {
        var map = new System.Collections.Generic.Dictionary<string, string>();
        Assert.Equal("uint", TsDeclarationParser.MapTsType("uint", map));
        Assert.Equal("ulong", TsDeclarationParser.MapTsType("ulong", map));
        Assert.Equal("ushort", TsDeclarationParser.MapTsType("ushort", map));
        Assert.Equal("byte", TsDeclarationParser.MapTsType("byte", map));
    }

    [Fact]
    public void MapTsType_width_alias_float() {
        var map = new System.Collections.Generic.Dictionary<string, string>();
        Assert.Equal("float", TsDeclarationParser.MapTsType("float", map));
    }

    [Fact]
    public void MapTsType_Guid_is_fully_qualified() {
        // Generated files emit `using System.Text.Json.Serialization;` and (for module
        // files) `using Microsoft.JSInterop;` — they do NOT import `using System;`.
        // Returning fully-qualified `System.Guid` keeps the mapping safe in every
        // namespace context.
        var map = new System.Collections.Generic.Dictionary<string, string>();
        Assert.Equal("System.Guid", TsDeclarationParser.MapTsType("Guid", map));
    }

    [Fact]
    public void MapTsType_width_aliases_do_not_record_fallbacks() {
        // Width aliases are recognized first-class (same priority as `number`/`string`),
        // so they must never trigger BAPI002. Verifies they go through the primitives
        // switch and exit before the unknown-fallback path.
        var map = new System.Collections.Generic.Dictionary<string, string>();
        var fallbacks = new System.Collections.Generic.List<TsTypeFallback>();

        foreach (var alias in new[] { "int", "uint", "long", "ulong", "short", "ushort",
                                       "byte", "sbyte", "float", "Guid" }) {
            TsDeclarationParser.MapTsType(alias, map, fallbacks, "ctx");
        }

        Assert.Empty(fallbacks);
    }

    [Fact]
    public void MapTsType_width_aliases_inside_arrays() {
        // Container handling is recursive — `int[]` should resolve via the existing
        // array branch, calling MapTsType("int") and appending "[]".
        var map = new System.Collections.Generic.Dictionary<string, string>();
        Assert.Equal("int[]", TsDeclarationParser.MapTsType("int[]", map));
        Assert.Equal("long[]", TsDeclarationParser.MapTsType("Array<long>", map));
        Assert.Equal("System.Guid[]", TsDeclarationParser.MapTsType("Guid[]", map));
    }

    [Fact]
    public void MapTsType_width_aliases_inside_record() {
        // Record<string, T> resolves T recursively — width aliases must work as values.
        var map = new System.Collections.Generic.Dictionary<string, string>();
        Assert.Equal("System.Collections.Generic.Dictionary<string, int>",
            TsDeclarationParser.MapTsType("Record<string, int>", map));
        Assert.Equal("System.Collections.Generic.Dictionary<string, System.Guid>",
            TsDeclarationParser.MapTsType("Record<string, Guid>", map));
    }

    [Fact]
    public void MapTsType_width_aliases_inside_promise() {
        // Promise<T> unwraps to T; the generator emits `Task<int>` etc. on the C# side.
        var map = new System.Collections.Generic.Dictionary<string, string>();
        Assert.Equal("int", TsDeclarationParser.MapTsType("Promise<int>", map));
        Assert.Equal("System.Guid", TsDeclarationParser.MapTsType("Promise<Guid>", map));
    }

    [Fact]
    public void Parse_function_with_width_alias_params_and_return() {
        // End-to-end: width aliases in parameter and return positions flow through
        // to the generated method's signature without any fallback diagnostic.
        var dts = "export function createTracker(intervalMs: int, throttleHz: float): long;";
        var result = TsDeclarationParser.Parse(dts);

        var func = Assert.Single(result.Functions);
        Assert.Equal("long", func.ReturnType);
        Assert.Equal("int", func.Params[0].CSharpType);
        Assert.Equal("float", func.Params[1].CSharpType);
        Assert.Empty(result.UnknownTypeFallbacks);
    }

    [Fact]
    public void Parse_interface_with_width_aliases_and_Guid() {
        // Width aliases in property positions become typed C# properties — including
        // optional ones (the existing `?` → `T?` logic kicks in because `int` doesn't
        // end in `?` or `[]`).
        var dts = @"
export interface EntityRef {
    id: Guid;
    version: long;
    sequenceNumber: int;
    confidence?: float;
    flags: byte;
}";
        var result = TsDeclarationParser.Parse(dts);

        var iface = Assert.Single(result.Interfaces);
        Assert.Equal("System.Guid", iface.Properties[0].CSharpType);
        Assert.Equal("long", iface.Properties[1].CSharpType);
        Assert.Equal("int", iface.Properties[2].CSharpType);
        Assert.Equal("float?", iface.Properties[3].CSharpType);
        Assert.Equal("byte", iface.Properties[4].CSharpType);
        Assert.Empty(result.UnknownTypeFallbacks);
    }

    [Fact]
    public void MapTsType_known_interface() {
        var map = new System.Collections.Generic.Dictionary<string, string> {
            ["DragConfig"] = "DragConfig"
        };
        Assert.Equal("DragConfig", TsDeclarationParser.MapTsType("DragConfig", map));
    }

    [Fact]
    public void MapTsType_unknown_returns_object() {
        var map = new System.Collections.Generic.Dictionary<string, string>();
        Assert.Equal("object", TsDeclarationParser.MapTsType("UnknownType", map));
    }

    [Fact]
    public void MapTsType_records_unknown_fallback_when_accumulator_provided() {
        var map = new System.Collections.Generic.Dictionary<string, string>();
        var fallbacks = new System.Collections.Generic.List<TsTypeFallback>();

        var mapped = TsDeclarationParser.MapTsType("FancyGeneric<T>", map, fallbacks, "foo.bar");

        Assert.Equal("object", mapped);
        Assert.Single(fallbacks);
        Assert.Equal("FancyGeneric<T>", fallbacks[0].TsType);
        Assert.Equal("foo.bar", fallbacks[0].Context);
    }

    [Fact]
    public void MapTsType_does_not_record_intentional_mappings() {
        // `any`, `null`, and `DotNetObjectReference` intentionally map to `object`.
        // None of these should trigger a diagnostic.
        var map = new System.Collections.Generic.Dictionary<string, string>();
        var fallbacks = new System.Collections.Generic.List<TsTypeFallback>();

        Assert.Equal("object", TsDeclarationParser.MapTsType("any", map, fallbacks, "ctx"));
        Assert.Equal("object", TsDeclarationParser.MapTsType("null", map, fallbacks, "ctx"));
        Assert.Equal("object",
            TsDeclarationParser.MapTsType("DotNetObjectReference", map, fallbacks, "ctx"));
        Assert.Equal("object",
            TsDeclarationParser.MapTsType("DotNetObjectReference<Foo>", map, fallbacks, "ctx"));

        Assert.Empty(fallbacks);
    }

    [Fact]
    public void Parse_skips_DotNetObjectReference_stub_interface_declaration() {
        // Stub `interface DotNetObjectReference {}` in a .d.ts is only there to satisfy
        // TypeScript. The generator must not register it in the type map or emit a C#
        // class for it — that would collide with the real Microsoft.JSInterop type.
        // A top-level `DotNetObjectReference` parameter is flagged for Path C generic
        // method emission (no fallback diagnostic).
        var dts = @"
interface DotNetObjectReference {}

export function createDrag(dotNetRef: DotNetObjectReference, name: string): number;
";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Empty(result.Interfaces);
        Assert.DoesNotContain("DotNetObjectReference", result.TypeMap.Keys);
        Assert.Empty(result.UnknownTypeFallbacks);

        var func = Assert.Single(result.Functions);
        Assert.True(func.Params[0].IsDotNetObjectRef);
        Assert.Equal("string", func.Params[1].CSharpType);
    }

    [Fact]
    public void Parse_flags_DotNetObjectReference_param_for_generic_method() {
        // Path C: top-level DotNetObjectReference parameter gets flagged so the emitter
        // promotes the whole method to generic-over-TDotNetRef. Works with or without
        // a type argument (`DotNetObjectReference` vs `DotNetObjectReference<X>`).
        var dts = @"
export function a(r: DotNetObjectReference): void;
export function b(r: DotNetObjectReference<SomeType>): void;
";
        var result = TsDeclarationParser.Parse(dts);

        Assert.True(result.Functions[0].Params[0].IsDotNetObjectRef);
        Assert.True(result.Functions[1].Params[0].IsDotNetObjectRef);
        Assert.Empty(result.UnknownTypeFallbacks);
    }

    [Fact]
    public void Parse_flags_optional_DotNetObjectReference_param() {
        var dts = "export function init(ref?: DotNetObjectReference): void;";
        var result = TsDeclarationParser.Parse(dts);

        var param = result.Functions[0].Params[0];
        Assert.True(param.IsDotNetObjectRef);
        Assert.True(param.IsOptional);
    }

    [Fact]
    public void Parse_DotNetObjectReference_as_property_falls_back_to_object() {
        // Only top-level parameter position gets Path C treatment. In an interface
        // property, we can't make the record generic, so we fall back to object.
        // This is an intentional mapping (no BAPI002).
        var dts = @"
export interface Cfg {
    ref: DotNetObjectReference;
}
";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Equal("object", result.Interfaces[0].Properties[0].CSharpType);
        Assert.Empty(result.UnknownTypeFallbacks);
    }

    [Fact]
    public void Parse_multiple_DotNetObjectReference_params() {
        var dts = "export function link(a: DotNetObjectReference, b: DotNetObjectReference): void;";
        var result = TsDeclarationParser.Parse(dts);

        Assert.True(result.Functions[0].Params[0].IsDotNetObjectRef);
        Assert.True(result.Functions[0].Params[1].IsDotNetObjectRef);
    }

    [Fact]
    public void MapTsType_records_unknown_inside_array_with_outer_context() {
        var map = new System.Collections.Generic.Dictionary<string, string>();
        var fallbacks = new System.Collections.Generic.List<TsTypeFallback>();

        Assert.Equal("object[]", TsDeclarationParser.MapTsType("FancyThing[]", map, fallbacks, "outer"));
        Assert.Single(fallbacks);
        Assert.Equal("FancyThing", fallbacks[0].TsType);
        Assert.Equal("outer", fallbacks[0].Context);
    }

    [Fact]
    public void Parse_non_exported_interface_is_registered_and_referenced_as_typed() {
        // Regression/feature: a private helper interface (no `export`) referenced
        // from a public signature should still produce a typed record instead of
        // silently falling back to `object`.
        var dts = @"
interface CacheEntry {
    key: string;
    value: number;
}
export function getCache(): CacheEntry;
";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Single(result.Interfaces);
        Assert.Equal("CacheEntry", result.Interfaces[0].TsName);
        Assert.Equal("CacheEntry", result.Functions[0].ReturnType);
        Assert.Empty(result.UnknownTypeFallbacks);
    }

    [Fact]
    public void Parse_non_exported_interface_referenced_as_property() {
        var dts = @"
interface InnerConfig {
    x: number;
}
export interface Outer {
    inner: InnerConfig;
}
";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Equal(2, result.Interfaces.Count);
        var outer = result.Interfaces.Find(i => i.TsName == "Outer");
        Assert.NotNull(outer);
        Assert.Equal("InnerConfig", outer!.Properties[0].CSharpType);
        Assert.Empty(result.UnknownTypeFallbacks);
    }

    [Fact]
    public void Parse_records_unknown_type_in_function_param() {
        var dts = "export function configure(opts: FancyOptions): void;";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Equal("object", result.Functions[0].Params[0].CSharpType);
        Assert.Single(result.UnknownTypeFallbacks);
        Assert.Equal("FancyOptions", result.UnknownTypeFallbacks[0].TsType);
        Assert.Contains("configure", result.UnknownTypeFallbacks[0].Context);
        Assert.Contains("opts", result.UnknownTypeFallbacks[0].Context);
    }

    [Fact]
    public void Parse_records_unknown_type_in_interface_property() {
        var dts = @"
export interface Outer {
    data: SomeIntersection & OtherThing;
}
";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Equal("object", result.Interfaces[0].Properties[0].CSharpType);
        Assert.Single(result.UnknownTypeFallbacks);
        Assert.Contains("Outer.data", result.UnknownTypeFallbacks[0].Context);
    }

    [Fact]
    public void Parse_records_unknown_return_type_context() {
        var dts = "export function build(): CustomThing;";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Single(result.UnknownTypeFallbacks);
        Assert.Equal("CustomThing", result.UnknownTypeFallbacks[0].TsType);
        Assert.Contains("build", result.UnknownTypeFallbacks[0].Context);
        Assert.Contains("return", result.UnknownTypeFallbacks[0].Context);
    }

    [Fact]
    public void Parse_known_types_produce_no_fallbacks() {
        var dts = @"
export interface Good {
    name: string;
    count: number;
    active: boolean;
    tags: string[];
    meta?: Record<string, string>;
    data: any;
    ref: DotNetObjectReference;
}
export function handle(dotNet: DotNetObjectReference, g: Good): void;
";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Empty(result.UnknownTypeFallbacks);
    }

    [Fact]
    public void Parse_interface_with_union_and_many_optional_properties() {
        // Regression test: Bug 1 — properties after a string literal union were dropped
        var dts = @"
export interface GhostConfig {
    mode: 'clone' | 'template' | 'label' | 'moveSource' | 'none';
    sourceClass?: string;
    templateSelector?: string;
    labelAttribute?: string;
    className?: string;
    offsetX?: number;
    offsetY?: number;
}";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Single(result.Interfaces);
        var iface = result.Interfaces[0];
        Assert.Equal(7, iface.Properties.Count);
        Assert.Equal("Mode", iface.Properties[0].CSharpName);
        Assert.Equal("SourceClass", iface.Properties[1].CSharpName);
        Assert.Equal("TemplateSelector", iface.Properties[2].CSharpName);
        Assert.Equal("LabelAttribute", iface.Properties[3].CSharpName);
        Assert.Equal("ClassName", iface.Properties[4].CSharpName);
        Assert.Equal("OffsetX", iface.Properties[5].CSharpName);
        Assert.Equal("OffsetY", iface.Properties[6].CSharpName);
        Assert.Equal("double?", iface.Properties[5].CSharpType);
        Assert.Equal("double?", iface.Properties[6].CSharpType);
    }

    [Fact]
    public void Parse_interface_with_jsdoc_comments_on_properties() {
        // Regression test: JSDoc comments between properties shouldn't break parsing
        var dts = @"
export interface Config {
    /** The container selector */
    container: string;
    /** Optional handle */
    handle?: string;
    /** Threshold in pixels */
    threshold?: number;
}";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Single(result.Interfaces);
        Assert.Equal(3, result.Interfaces[0].Properties.Count);
        Assert.Equal("Container", result.Interfaces[0].Properties[0].CSharpName);
        Assert.Equal("Handle", result.Interfaces[0].Properties[1].CSharpName);
        Assert.Equal("Threshold", result.Interfaces[0].Properties[2].CSharpName);
    }

    [Fact]
    public void Parse_interface_with_jsdoc_apostrophe_in_comment() {
        // Regression: stray single quote in JSDoc comment entered string mode
        // and swallowed the closing brace, breaking body extraction
        var dts = @"
export interface Config {
    /** It's a container selector — don't change at runtime */
    container: string;
    /** The user's handle */
    handle?: string;
}";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Single(result.Interfaces);
        Assert.Equal(2, result.Interfaces[0].Properties.Count);
        Assert.Equal("Container", result.Interfaces[0].Properties[0].CSharpName);
        Assert.Equal("Handle", result.Interfaces[0].Properties[1].CSharpName);
    }

    [Fact]
    public void Parse_interface_with_jsdoc_link_braces() {
        // Regression: {@link ...} braces in JSDoc threw off depth counting
        var dts = @"
export interface Config {
    /** See {@link OtherConfig} for details. */
    name: string;
    /** Use {@link Defaults.timeout} if unset */
    timeout?: number;
}";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Single(result.Interfaces);
        Assert.Equal(2, result.Interfaces[0].Properties.Count);
        Assert.Equal("Name", result.Interfaces[0].Properties[0].CSharpName);
        Assert.Equal("Timeout", result.Interfaces[0].Properties[1].CSharpName);
    }

    [Fact]
    public void Parse_interface_with_jsdoc_property_like_example() {
        // Regression: JSDoc @example lines matching PropertyRegex created
        // spurious properties and duplicate enum names
        var dts = @"
export interface Config {
    /**
     * @example
     * mode: 'fast' | 'slow';
     */
    mode: 'fast' | 'slow';
    name: string;
}";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Single(result.Interfaces);
        Assert.Equal(2, result.Interfaces[0].Properties.Count);
        Assert.Single(result.Enums); // only ONE enum, not two
    }

    [Fact]
    public void Parse_property_JSDoc_becomes_Summary() {
        // Per-property JSDoc above a property line should be captured as
        // TsPropertyInfo.Summary so the generator can emit it as a C# XML <summary>.
        // This is the IntelliSense pipeline: JSDoc in .ts/.d.ts → XML docs in .cs.
        var dts = @"
export interface Config {
    /** CSS selector for the drag container. */
    container: string;
    /** Optional pointer-move threshold in pixels before a drag starts. */
    threshold?: number;
    /** Kind of ghost to display while dragging. */
    mode: 'clone' | 'none';
}
";
        var result = TsDeclarationParser.Parse(dts);
        var iface = result.Interfaces[0];

        Assert.Equal("CSS selector for the drag container.", iface.Properties[0].Summary);
        Assert.Equal("Optional pointer-move threshold in pixels before a drag starts.", iface.Properties[1].Summary);
        Assert.Equal("Kind of ghost to display while dragging.", iface.Properties[2].Summary);
    }

    [Fact]
    public void Parse_property_without_JSDoc_has_null_Summary() {
        // Properties without JSDoc should leave Summary null, so the emitter can fall
        // back to the "Maps to TypeScript property X" boilerplate.
        var dts = @"
export interface Config {
    container: string;
    threshold?: number;
}
";
        var result = TsDeclarationParser.Parse(dts);
        var iface = result.Interfaces[0];

        Assert.Null(iface.Properties[0].Summary);
        Assert.Null(iface.Properties[1].Summary);
    }

    [Fact]
    public void Parse_interface_JSDoc_becomes_Summary() {
        // Interface-level JSDoc (above `export interface Foo`) flows through to
        // TsInterfaceInfo.Summary for emission as the record's C# XML <summary>.
        var dts = @"
/** Configuration for creating a drag-and-drop context. */
export interface DragConfig {
    container: string;
}
";
        var result = TsDeclarationParser.Parse(dts);
        Assert.Equal("Configuration for creating a drag-and-drop context.", result.Interfaces[0].Summary);
    }

    [Fact]
    public void Parse_interface_without_JSDoc_has_null_Summary() {
        var dts = "export interface Plain { x: number; }";
        var result = TsDeclarationParser.Parse(dts);
        Assert.Null(result.Interfaces[0].Summary);
    }

    [Fact]
    public void Parse_multiline_JSDoc_is_joined_as_single_summary() {
        // Multi-line JSDoc (common for longer descriptions) should be collapsed into
        // a single string with spaces. The "*" prefix on canonical JSDoc lines is
        // stripped so the resulting text doesn't have visual artifacts.
        var dts = @"
export interface Cfg {
    /**
     * The CSS selector for the source element.
     * Must match exactly one element per drag context.
     */
    source: string;
}
";
        var result = TsDeclarationParser.Parse(dts);
        Assert.Equal(
            "The CSS selector for the source element. Must match exactly one element per drag context.",
            result.Interfaces[0].Properties[0].Summary);
    }

    [Fact]
    public void Parse_JSDoc_stops_at_first_tag() {
        // The summary section is the paragraph before any @tag. Content after the
        // first tag is owned by the tagged-section parser (for functions: @param /
        // @returns; for properties we don't currently surface tags).
        var dts = @"
export interface Cfg {
    /**
     * Short description.
     * @example { x: 1 }
     * @deprecated use {@link OtherCfg} instead
     */
    x: number;
}
";
        var result = TsDeclarationParser.Parse(dts);
        Assert.Equal("Short description.", result.Interfaces[0].Properties[0].Summary);
    }

    [Fact]
    public void Parse_JSDoc_not_attached_when_separated_by_another_declaration() {
        // A JSDoc block must sit *immediately* above its target (whitespace-only
        // separation). A declaration between the JSDoc and the target disqualifies
        // the attachment, so we don't leak someone else's docs onto a later member.
        var dts = @"
export interface Cfg {
    /** Belongs to a. */
    a: number;
    b: number;
}
";
        var result = TsDeclarationParser.Parse(dts);
        Assert.Equal("Belongs to a.", result.Interfaces[0].Properties[0].Summary);
        Assert.Null(result.Interfaces[0].Properties[1].Summary);
    }

    [Fact]
    public void Parse_ts_function_with_implementation_body() {
        // Preview.7: the parser now treats .ts source (with bodies) the same as .d.ts
        // (without bodies). Function regex accepts either `;` or `{` as the terminator,
        // and the body's contents are simply not matched by any of our patterns.
        var ts = @"
export function greet(name: string, times: number): string {
    return `${name} `.repeat(times);
}
";
        var result = TsDeclarationParser.Parse(ts);

        Assert.Single(result.Functions);
        Assert.Equal("greet", result.Functions[0].JsName);
        Assert.Equal("string", result.Functions[0].ReturnType);
        Assert.Equal("string", result.Functions[0].Params[0].CSharpType);
        Assert.Equal("double", result.Functions[0].Params[1].CSharpType);
    }

    [Fact]
    public void Parse_ts_async_function() {
        // `async` is common in .ts source and was never valid .d.ts syntax. The parser
        // now recognizes it as an optional modifier.
        var ts = @"
export async function fetchData(url: string): Promise<string> {
    const r = await fetch(url);
    return r.text();
}
";
        var result = TsDeclarationParser.Parse(ts);

        Assert.Single(result.Functions);
        // Promise<T> is unwrapped to T for the C# method return type (method is already Task<T>).
        Assert.Equal("string", result.Functions[0].ReturnType);
    }

    [Fact]
    public void Parse_ts_function_body_is_not_mistaken_for_property() {
        // Regression guard: function bodies contain statements that might look like
        // TypeScript property declarations to a careless regex. The body must be
        // effectively invisible to the rest of the parser.
        var ts = @"
export interface Config {
    name: string;
}

export function parse(input: string): Config {
    const cfg: Config = { name: input };
    return cfg;
}
";
        var result = TsDeclarationParser.Parse(ts);

        Assert.Single(result.Interfaces);
        Assert.Single(result.Interfaces[0].Properties);
        Assert.Equal("name", result.Interfaces[0].Properties[0].TsName);
        // The body's `const cfg: Config` must NOT have leaked into the interface as a property.
    }

    [Fact]
    public void Parse_ts_interface_with_same_shape_as_dts() {
        // Interfaces have identical syntax in .ts and .d.ts. Same JSDoc extraction,
        // same property mapping, same enum synthesis — no special casing needed.
        var ts = @"
/** Runtime configuration. */
export interface Config {
    /** The name field. */
    name: string;
    kind: 'a' | 'b';
}

export function configure(cfg: Config): void {
    // ...implementation
}
";
        var result = TsDeclarationParser.Parse(ts);

        Assert.Equal("Runtime configuration.", result.Interfaces[0].Summary);
        Assert.Equal("The name field.", result.Interfaces[0].Properties[0].Summary);
        Assert.Equal("ConfigKind", result.Enums[0].CSharpName);
        Assert.Equal("Config", result.Functions[0].Params[0].CSharpType);
    }

    [Fact]
    public void Parse_ts_JSDoc_flows_same_as_dts() {
        // The .ts JSDoc extraction uses the same code path as .d.ts, so all the
        // JSDoc behaviors (multiline, stop at @tag, boilerplate fallback) apply.
        var ts = @"
/**
 * Create a new thing.
 * @param id - The identifier.
 * @returns The new thing's handle.
 */
export function create(id: string): number {
    return id.length;
}
";
        var result = TsDeclarationParser.Parse(ts);

        Assert.Equal("Create a new thing.", result.Functions[0].Summary);
        Assert.Equal("The identifier.", result.Functions[0].Params[0].Description);
        Assert.Equal("The new thing's handle.", result.Functions[0].ReturnsDoc);
    }

    [Fact]
    public void Parse_interface_with_line_comments() {
        var dts = @"
export interface Config {
    // The container: string; selector
    container: string;
    handle?: string; // don't remove
}";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Single(result.Interfaces);
        Assert.Equal(2, result.Interfaces[0].Properties.Count);
    }

    [Fact]
    public void Parse_jsdoc_apostrophe_does_not_swallow_subsequent_interfaces() {
        // Regression: stray quote in first interface's JSDoc caused body extraction
        // to consume the rest of the file, preventing subsequent interfaces from
        // being parsed correctly
        var dts = @"
export interface First {
    /** It's required */
    name: string;
}

export interface Second {
    value: number;
}

export function doStuff(): void;
";
        var result = TsDeclarationParser.Parse(dts);

        Assert.Equal(2, result.Interfaces.Count);
        Assert.Single(result.Interfaces[0].Properties);
        Assert.Equal("Name", result.Interfaces[0].Properties[0].CSharpName);
        Assert.Single(result.Interfaces[1].Properties);
        Assert.Equal("Value", result.Interfaces[1].Properties[0].CSharpName);
        Assert.Single(result.Functions);
    }

    [Fact]
    public void Parse_full_mw_dnd_scenario() {
        var dts = @"
export interface DragConfig {
    container: string;
    sources: string;
    handle?: string;
    threshold?: number;
    watch: string[];
    ghost?: GhostConfig;
    behaviors?: Record<string, BehaviorConfig>;
}

export interface GhostConfig {
    mode: 'clone' | 'template' | 'label' | 'moveSource' | 'none';
    sourceClass?: string;
    offsetX?: number;
    offsetY?: number;
}

export interface BehaviorConfig {
    insertionLine?: InsertionConfig;
}

export interface InsertionConfig {
    items: string;
    axis?: 'vertical' | 'horizontal';
}

/** Create a new drag-and-drop context. */
export function createDrag(dotNetRef: DotNetObjectReference, config: DragConfig): number;
export function destroyDrag(contextId: number): void;
export function dispose(): void;
export function addClassToMatching(selector: string, className: string): void;
";
        var result = TsDeclarationParser.Parse(dts);

        // Interfaces
        Assert.Equal(4, result.Interfaces.Count);

        // DragConfig
        var dragConfig = result.Interfaces.Find(i => i.TsName == "DragConfig");
        Assert.NotNull(dragConfig);
        Assert.Equal(7, dragConfig!.Properties.Count);
        Assert.True(dragConfig.Properties[0].IsRequired); // container
        Assert.True(dragConfig.Properties[4].IsRequired);  // watch: string[]
        Assert.True(dragConfig.Properties[3].IsOptional);  // threshold?
        Assert.Equal("string[]", dragConfig.Properties[4].CSharpType); // watch

        // GhostConfig with enum
        var ghostConfig = result.Interfaces.Find(i => i.TsName == "GhostConfig");
        Assert.NotNull(ghostConfig);
        var modeEnum = result.Enums.Find(e => e.CSharpName == "GhostConfigMode");
        Assert.NotNull(modeEnum);
        Assert.Equal(5, modeEnum!.Members.Count);

        // Functions
        Assert.Equal(4, result.Functions.Count);
        Assert.Equal("createDrag", result.Functions[0].JsName);
        Assert.Equal("double", result.Functions[0].ReturnType);
        Assert.True(result.Functions[0].Params[0].IsDotNetObjectRef);    // Path C generic
        Assert.Equal("DragConfig", result.Functions[0].Params[1].CSharpType);
        Assert.Equal("Create a new drag-and-drop context.", result.Functions[0].Summary);

        Assert.Null(result.Functions[1].ReturnType); // destroyDrag → void
        Assert.Equal("double", result.Functions[1].Params[0].CSharpType); // contextId: number
    }
}
