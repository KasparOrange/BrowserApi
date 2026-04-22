using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace BrowserApi.SourceGen.Tests;

/// <summary>
/// Integration tests that run the full source generator pipeline via CSharpGeneratorDriver.
/// These catch issues that unit-testing the parser alone cannot (e.g., AddSource hint-name
/// collisions, pipeline wiring, PostInitialization vs SourceOutput interactions).
/// </summary>
public class JsModuleGeneratorDriverTests {
    [Fact]
    public void Generator_produces_all_expected_files_from_dts() {
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
    templateSelector?: string;
    labelAttribute?: string;
    className?: string;
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

        var result = RunGenerator(dts, "wwwroot/js/src/mw-dnd.d.ts");

        Assert.Empty(result.Diagnostics);

        var sourceNames = result.GeneratedTrees
            .Select(t => System.IO.Path.GetFileName(t.FilePath))
            .OrderBy(n => n)
            .ToList();

        // Infrastructure (PostInitialization)
        Assert.Contains("IJsModulePathResolver.g.cs", sourceNames);
        Assert.Contains("JsModuleAttribute.g.cs", sourceNames);

        // Records
        Assert.Contains("DragConfig.g.cs", sourceNames);
        Assert.Contains("GhostConfig.g.cs", sourceNames);
        Assert.Contains("BehaviorConfig.g.cs", sourceNames);
        Assert.Contains("InsertionConfig.g.cs", sourceNames);

        // Enums
        Assert.Contains("GhostConfigMode.g.cs", sourceNames);
        Assert.Contains("InsertionConfigAxis.g.cs", sourceNames);

        // Module class
        Assert.Contains("MwDndModule.g.cs", sourceNames);

        // DI registration
        Assert.Contains("JsModuleServiceCollectionExtensions.g.cs", sourceNames);

        // GhostConfig must have all 7 properties
        var ghostSource = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("GhostConfig.g.cs"))
            .GetText().ToString();
        Assert.Contains("Mode", ghostSource);
        Assert.Contains("SourceClass", ghostSource);
        Assert.Contains("TemplateSelector", ghostSource);
        Assert.Contains("LabelAttribute", ghostSource);
        Assert.Contains("ClassName", ghostSource);
        Assert.Contains("OffsetX", ghostSource);
        Assert.Contains("OffsetY", ghostSource);
    }

    [Fact]
    public void Generator_handles_jsdoc_with_apostrophes_in_comments() {
        var dts = @"
export interface Config {
    /** It's required — don't omit */
    name: string;
    /** The user's preference */
    pref?: number;
}

export function init(config: Config): void;
";

        var result = RunGenerator(dts, "wwwroot/js/widget.d.ts");

        Assert.Empty(result.Diagnostics);

        var sourceNames = result.GeneratedTrees
            .Select(t => System.IO.Path.GetFileName(t.FilePath))
            .ToList();

        Assert.Contains("Config.g.cs", sourceNames);
        Assert.Contains("WidgetModule.g.cs", sourceNames);

        var configSource = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("Config.g.cs"))
            .GetText().ToString();
        Assert.Contains("Name", configSource);
        Assert.Contains("Pref", configSource);
    }

    [Fact]
    public void Generator_handles_jsdoc_link_braces() {
        var dts = @"
export interface Options {
    /** See {@link Defaults} for info. Use {@link validate} to check. */
    timeout: number;
}

export function run(opts: Options): void;
";

        var result = RunGenerator(dts, "wwwroot/js/runner.d.ts");

        Assert.Empty(result.Diagnostics);

        var sourceNames = result.GeneratedTrees
            .Select(t => System.IO.Path.GetFileName(t.FilePath))
            .ToList();

        Assert.Contains("Options.g.cs", sourceNames);
        Assert.Contains("RunnerModule.g.cs", sourceNames);
    }

    [Fact]
    public void Generator_emits_record_for_non_exported_interface() {
        // A private helper interface (no `export`) used in a public signature
        // should still produce a typed C# record — no silent `object` fallback.
        var dts = @"
interface CacheEntry {
    key: string;
    value: number;
}

export function getCache(): CacheEntry;
";

        var result = RunGenerator(dts, "wwwroot/js/cache.d.ts");

        Assert.Empty(result.Diagnostics);

        var sourceNames = result.GeneratedTrees
            .Select(t => System.IO.Path.GetFileName(t.FilePath))
            .ToList();

        Assert.Contains("CacheEntry.g.cs", sourceNames);

        var moduleSource = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("CacheModule.g.cs"))
            .GetText().ToString();
        // Return type should be the strongly-typed record, not object.
        Assert.Contains("Task<CacheEntry>", moduleSource);
    }

    [Fact]
    public void Generator_reports_BAPI002_for_unknown_type_in_param() {
        var dts = @"
export function configure(opts: SomeUnresolvedThing): void;
";

        var result = RunGenerator(dts, "wwwroot/js/thing.d.ts");

        var diag = Assert.Single(result.Diagnostics);
        Assert.Equal("BAPI002", diag.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
        var message = diag.GetMessage();
        Assert.Contains("SomeUnresolvedThing", message);
        Assert.Contains("configure", message);
    }

    [Fact]
    public void Generator_does_not_emit_class_for_DotNetObjectReference_stub() {
        // Regression: a stub `interface DotNetObjectReference {}` in a .d.ts (present only
        // to satisfy TypeScript) must not produce a `DotNetObjectReference.g.cs` class that
        // would collide with `Microsoft.JSInterop.DotNetObjectReference` at consumer call sites.
        var dts = @"
interface DotNetObjectReference {}

export interface DragConfig {
    container: string;
}

export function createDrag(dotNetRef: DotNetObjectReference, config: DragConfig): number;
";

        var result = RunGenerator(dts, "wwwroot/js/mw-dnd.d.ts");

        Assert.Empty(result.Diagnostics);

        var sourceNames = result.GeneratedTrees
            .Select(t => System.IO.Path.GetFileName(t.FilePath))
            .ToList();

        // The stub must NOT produce a C# class.
        Assert.DoesNotContain("DotNetObjectReference.g.cs", sourceNames);
        // DragConfig still emits as normal.
        Assert.Contains("DragConfig.g.cs", sourceNames);

        // The generated module method signature uses the real Blazor type, not `object`.
        var moduleSource = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("MwDndModule.g.cs"))
            .GetText().ToString();
        Assert.Contains("Microsoft.JSInterop.DotNetObjectReference dotNetRef", moduleSource);
        Assert.DoesNotContain("object dotNetRef", moduleSource);
    }

    [Fact]
    public void Generator_does_not_report_BAPI002_for_intentional_any() {
        // `any`, `null`, and DotNetObjectReference intentionally map to `object` —
        // these are not silent degradations and should not produce a warning.
        var dts = @"
export interface Cfg {
    data: any;
    ref: DotNetObjectReference;
}
export function run(dotNet: DotNetObjectReference, cfg: Cfg): void;
";

        var result = RunGenerator(dts, "wwwroot/js/runner.d.ts");

        Assert.Empty(result.Diagnostics);
    }

    private static GeneratorDriverRunResult RunGenerator(string dtsContent, string dtsPath) {
        var compilation = CSharpCompilation.Create("TestAssembly",
            references: new[] {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            });

        var generator = new JsModuleGenerator();

        var driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts(
                System.Collections.Immutable.ImmutableArray.Create<AdditionalText>(
                    new InMemoryAdditionalText(dtsPath, dtsContent)));

        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, out _, out _);

        return driver.GetRunResult();
    }

    private sealed class InMemoryAdditionalText : AdditionalText {
        private readonly string _text;
        public override string Path { get; }

        public InMemoryAdditionalText(string path, string text) {
            Path = path;
            _text = text;
        }

        public override SourceText? GetText(System.Threading.CancellationToken cancellationToken = default) =>
            SourceText.From(_text);
    }
}
