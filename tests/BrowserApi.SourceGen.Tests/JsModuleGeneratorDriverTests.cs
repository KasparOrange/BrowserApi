using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
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
    public void Generator_emits_generic_method_for_DotNetObjectReference_param() {
        // Path C: a stub `interface DotNetObjectReference {}` is skipped (no colliding
        // class), and the top-level DotNetObjectReference parameter promotes the whole
        // method to generic-over-TDotNetRef so callers pass their real
        // DotNetObjectReference<T> with type inference intact.
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

        var moduleSource = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("MwDndModule.g.cs"))
            .GetText().ToString();

        // Method is generic over TDotNetRef, decorated with DAM for AOT/trim safety.
        Assert.Contains("CreateDragAsync<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers", moduleSource);
        Assert.Contains("DynamicallyAccessedMemberTypes.PublicMethods)] TDotNetRef>", moduleSource);

        // Param uses the real Blazor type, fully qualified.
        Assert.Contains("Microsoft.JSInterop.DotNetObjectReference<TDotNetRef> dotNetRef", moduleSource);

        // `where TDotNetRef : class` is required because DotNetObjectReference<T> has that constraint.
        Assert.Contains("where TDotNetRef : class", moduleSource);

        // No `object dotNetRef` — we're past the preview.5 fallback.
        Assert.DoesNotContain("object dotNetRef", moduleSource);
    }

    [Fact]
    public void Consumer_can_call_generated_DotNetObjectReference_method_with_inference() {
        // The critical Path C test: compile the generated code *together with* realistic
        // consumer code that calls the module's method by passing a DotNetObjectReference<MyPage>
        // without an explicit type argument. If C# type inference can't resolve TDotNetRef
        // from the argument, this fails with CS0411. Ensures the generic method we emit
        // is actually callable at the shape consumers write.
        var dts = @"
interface DotNetObjectReference {}
export function createDrag(dotNetRef: DotNetObjectReference, name: string): number;
";

        var consumerCode = @"
using System.Threading.Tasks;
using Microsoft.JSInterop;
using JsModules;

namespace TestConsumer {
    public class MyPage {
        [JSInvokable] public void OnCallback() {}
    }

    public class Caller {
        public async Task<double> Run(MwDndModule module) {
            var page = new MyPage();
            var objRef = DotNetObjectReference.Create(page);    // DotNetObjectReference<MyPage>
            return await module.CreateDragAsync(objRef, ""hi""); // TDotNetRef inferred as MyPage
        }
    }
}";

        var errors = CompileGeneratorOutputWithConsumerCode(dts, "wwwroot/js/mw-dnd.d.ts", consumerCode)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.True(errors.Count == 0,
            "Consumer code + generated code has compile errors:\n" +
            string.Join("\n", errors.Select(e => $"  {e.Id} at {e.Location.GetLineSpan()}: {e.GetMessage()}")));
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

    [Fact]
    public void Generator_output_compiles_cleanly_against_real_Microsoft_JSInterop() {
        // Regression guard against preview.4-style silent breakage:
        // String-based "does the output contain X?" tests cannot detect emitted code
        // that references types incorrectly — CS0721 (static type as parameter),
        // CS0246 (type not found), namespace typos, or wrong generic arity.
        // This test compiles the generator's output against the same real
        // Microsoft.JSInterop that a consumer project would see, and asserts zero
        // compile errors. Any change to emitted code that doesn't compile in a real
        // consumer will fail here before it can ship to NuGet.
        var dts = @"
interface DotNetObjectReference {}

export interface DragConfig {
    container: string;
    behaviors: Record<string, Behavior>;
    mode: 'a' | 'b';
}

export interface Behavior {
    name: string;
}

/** Create a new drag context. */
export function createDrag(dotNetRef: DotNetObjectReference, config: DragConfig): number;
export function destroyDrag(contextId: number): void;
export function fetchThing(id: string): Promise<DragConfig>;
";

        var errors = CompileGeneratorOutput(dts, "wwwroot/js/mw-dnd.d.ts")
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.True(errors.Count == 0,
            "Generated code has compile errors:\n" +
            string.Join("\n", errors.Select(e => $"  {e.Id} at {e.Location.GetLineSpan()}: {e.GetMessage()}")));
    }

    private static ImmutableArray<Diagnostic> CompileGeneratorOutput(string dts, string dtsPath) {
        // Step 1: run the generator against a seed compilation so it emits trees.
        var seed = CSharpCompilation.Create("SeedAssembly",
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var driver = CSharpGeneratorDriver.Create(new JsModuleGenerator())
            .AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(
                new InMemoryAdditionalText(dtsPath, dts)));
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(seed, out _, out _);

        // Step 2: compile the generated trees standalone with a full reference set,
        // simulating what a consumer assembly sees.
        var compilation = CSharpCompilation.Create(
            "ConsumerAssembly",
            syntaxTrees: driver.GetRunResult().GeneratedTrees,
            references: GetConsumerReferences(),
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        return compilation.GetDiagnostics();
    }

    private static ImmutableArray<Diagnostic> CompileGeneratorOutputWithConsumerCode(
        string dts, string dtsPath, string consumerCode) {

        var seed = CSharpCompilation.Create("SeedAssembly",
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var driver = CSharpGeneratorDriver.Create(new JsModuleGenerator())
            .AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(
                new InMemoryAdditionalText(dtsPath, dts)));
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(seed, out _, out _);

        var consumerTree = CSharpSyntaxTree.ParseText(consumerCode, path: "Consumer.cs");

        var compilation = CSharpCompilation.Create(
            "ConsumerAssembly",
            syntaxTrees: driver.GetRunResult().GeneratedTrees.Concat(new[] { consumerTree }),
            references: GetConsumerReferences(),
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        return compilation.GetDiagnostics();
    }

    private static IEnumerable<MetadataReference> GetConsumerReferences() {
        // All trusted-platform-assemblies (the runtime's BCL: System.Runtime, netstandard,
        // System.Collections, System.Text.Json, Microsoft.JSInterop when referenced, etc.)
        var tpa = (string?)System.AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "";
        foreach (var path in tpa.Split(Path.PathSeparator).Where(p => !string.IsNullOrEmpty(p)))
            yield return MetadataReference.CreateFromFile(path);

        // Belt-and-braces: touch types from each package that the generated code references,
        // forcing assembly resolution if the host hasn't loaded them yet.
        yield return MetadataReference.CreateFromFile(
            typeof(Microsoft.JSInterop.IJSRuntime).Assembly.Location);
        yield return MetadataReference.CreateFromFile(
            typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location);
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
