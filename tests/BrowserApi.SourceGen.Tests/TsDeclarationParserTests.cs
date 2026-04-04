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
        Assert.Equal("DragConfig", result.Functions[0].Params[1].CSharpType);
        Assert.Equal("Create a new drag-and-drop context.", result.Functions[0].Summary);

        Assert.Null(result.Functions[1].ReturnType); // destroyDrag → void
        Assert.Equal("double", result.Functions[1].Params[0].CSharpType); // contextId: number
    }
}
