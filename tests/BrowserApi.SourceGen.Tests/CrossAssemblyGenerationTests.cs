using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BrowserApi.SourceGen.Tests;

/// <summary>
/// A library and an app that both run the generator. Each test compiles real assemblies with
/// warnings as errors, because the original bug only showed up there: every assembly carried
/// public types under one fixed name, so the app failed with CS0436 / CS0121.
/// </summary>
public class CrossAssemblyGenerationTests {
    private const string ProbeModule = "/** Says hello. */\nexport function hello(): string { return 'x'; }";

    private const string AppCode = @"
using Microsoft.Extensions.DependencyInjection;

public sealed class AppResolver : BrowserApi.SourceGen.IJsModulePathResolver {
    public string Resolve(string moduleName) => $""./js/{moduleName}.js"";
}

public static class Startup {
    public static void Configure(IServiceCollection services) {
        services.AddJsModules();
        services.AddLibAJsModules();
    }
}
";

    [Fact]
    public void App_referencing_a_generating_library_compiles_with_warnings_as_errors() {
        var lib = Build("LibA", "wwwroot/js/lib-probe.ts", ProbeModule);
        var app = Build("AppB", "wwwroot/js/app-probe.ts", ProbeModule,
            consumerCode: AppCode, references: new[] { lib.Reference });

        Assert.Empty(lib.Problems);
        Assert.Empty(app.Problems);
    }

    [Fact]
    public void Modules_of_both_assemblies_take_the_one_resolver_contract() {
        var lib = Build("LibA", "wwwroot/js/lib-probe.ts", ProbeModule);
        var app = Build("AppB", "wwwroot/js/app-probe.ts", ProbeModule,
            consumerCode: AppCode, references: new[] { lib.Reference });

        var libResolverParameter = ResolverParameterOf(app.Compilation, "JsModules.LibProbeModule");
        var appResolverParameter = ResolverParameterOf(app.Compilation, "JsModules.AppProbeModule");

        Assert.Equal(libResolverParameter, appResolverParameter, SymbolEqualityComparer.Default);
        Assert.Equal("BrowserApi.JSInterop", appResolverParameter.ContainingAssembly.Name);
    }

    [Fact]
    public async Task One_resolver_registration_serves_the_modules_of_both_assemblies() {
        // Same module name in both assemblies on purpose: that is the case a resolver can only
        // tell apart through the assembly-aware overload.
        var lib = Build("LibA", "wwwroot/js/mw-scroll.ts", ProbeModule,
            buildProperties: new Dictionary<string, string> {
                ["build_property.BrowserApiSourceGenNamespace"] = "LibA.JsModules",
            });
        var app = Build("AppB", "wwwroot/js/mw-scroll.ts", ProbeModule,
            consumerCode: AppCode, references: new[] { lib.Reference });

        var loadContext = new AssemblyLoadContext(nameof(One_resolver_registration_serves_the_modules_of_both_assemblies));
        var libAssembly = loadContext.LoadFromStream(new MemoryStream(lib.Image));
        var appAssembly = loadContext.LoadFromStream(new MemoryStream(app.Image));

        var jsRuntime = new ImportRecordingJsRuntime();
        var services = new ServiceCollection()
            .AddSingleton<IJSRuntime>(jsRuntime)
            .AddSingleton<IJsModulePathResolver, PerAssemblyResolver>();
        InvokeRegistration(libAssembly, "LibAJsModuleServiceCollectionExtensions", "AddLibAJsModules", services);
        InvokeRegistration(appAssembly, "AppBJsModuleServiceCollectionExtensions", "AddAppBJsModules", services);
        await using var provider = services.BuildServiceProvider();

        await CallHelloAsync(provider, libAssembly.GetType("LibA.JsModules.MwScrollModule")!);
        await CallHelloAsync(provider, appAssembly.GetType("JsModules.MwScrollModule")!);

        Assert.Equal(new[] { "/LibA/mw-scroll.js", "/AppB/mw-scroll.js" }, jsRuntime.ImportedPaths);
    }

    [Fact]
    public void Same_module_name_in_library_and_app_needs_only_a_namespace_setting() {
        var lib = Build("LibA", "wwwroot/js/mw-scroll.ts", ProbeModule,
            buildProperties: new Dictionary<string, string> {
                ["build_property.BrowserApiSourceGenNamespace"] = "LibA.JsModules",
            });
        var app = Build("AppB", "wwwroot/js/mw-scroll.ts", ProbeModule,
            consumerCode: AppCode, references: new[] { lib.Reference });

        Assert.Empty(lib.Problems);
        Assert.Empty(app.Problems);
        Assert.NotNull(app.Compilation.GetTypeByMetadataName("LibA.JsModules.MwScrollModule"));
    }

    [Fact]
    public void Module_tells_the_resolver_its_assembly_and_module_name() {
        var lib = Build("LibA", "wwwroot/js/lib-probe.ts", ProbeModule);

        Assert.Contains(@"pathResolver?.Resolve(""lib-probe"", ""LibA"") ?? ""./js/lib-probe.js""",
            GeneratedSource(lib, "LibProbeModule.g.cs"));
    }

    [Fact]
    public void Module_base_path_replaces_the_default_import_path() {
        var lib = Build("LibA", "Interop/lib-probe.ts", ProbeModule,
            buildProperties: new Dictionary<string, string> {
                ["build_property.BrowserApiSourceGenModuleBasePath"] = "./_content/LibA/js/",
            });

        Assert.Contains(@"?? ""./_content/LibA/js/lib-probe.js""", GeneratedSource(lib, "LibProbeModule.g.cs"));
    }

    [Fact]
    public void Registration_class_is_named_after_the_assembly_and_short_name_stays_internal() {
        var lib = Build("Acme.Ui-Kit.Interop", "wwwroot/js/lib-probe.ts", ProbeModule);

        var registration = GeneratedSource(lib, "JsModuleServiceCollectionExtensions.g.cs");

        Assert.Contains("public static class AcmeUiKitInteropJsModuleServiceCollectionExtensions", registration);
        Assert.Contains("public static IServiceCollection AddAcmeUiKitInteropJsModules(", registration);
        Assert.Contains("internal static class JsModuleServiceCollectionExtensions", registration);
    }

    [Fact]
    public void Without_BrowserApi_JSInterop_the_module_compiles_and_has_no_resolver_parameter() {
        // The documented quick start references only the generator package. It must keep
        // building, and no copy of the interface may come back to make that happen.
        var app = Build("AppB", "wwwroot/js/app-probe.ts", ProbeModule,
            consumerCode: @"
public static class Startup {
    public static void Configure(Microsoft.Extensions.DependencyInjection.IServiceCollection services) =>
        Microsoft.Extensions.DependencyInjection.JsModuleServiceCollectionExtensions.AddJsModules(services);
}",
            withResolverContract: false);

        Assert.Empty(app.Problems);
        Assert.Contains("public AppProbeModule(IJSRuntime js) {", GeneratedSource(app, "AppProbeModule.g.cs"));
    }

    [Fact]
    public void Dom_element_parameter_maps_to_ElementReference_only_when_the_type_is_visible() {
        const string module = "export function focus(el: HTMLElement): void;";
        const string elementReference =
            "namespace Microsoft.AspNetCore.Components { public readonly struct ElementReference { } }";

        var withBlazor = Build("AppB", "wwwroot/js/probe.d.ts", module, consumerCode: elementReference);
        var withoutBlazor = Build("AppB", "wwwroot/js/probe.d.ts", module);

        Assert.Contains("FocusAsync(Microsoft.AspNetCore.Components.ElementReference el)",
            GeneratedSource(withBlazor, "ProbeModule.g.cs"));
        Assert.Empty(withBlazor.GeneratorDiagnostics);
        Assert.Contains("FocusAsync(object el)", GeneratedSource(withoutBlazor, "ProbeModule.g.cs"));
        Assert.Equal("BAPI002", Assert.Single(withoutBlazor.GeneratorDiagnostics).Id);
    }

    private static void InvokeRegistration(
        Assembly assembly, string className, string methodName, IServiceCollection services) {
        var registrationClass = assembly.GetType("Microsoft.Extensions.DependencyInjection." + className)!;
        registrationClass.GetMethod(methodName)!.Invoke(null, new object[] { services });
    }

    private static async Task CallHelloAsync(System.IServiceProvider provider, System.Type moduleType) {
        var module = provider.GetRequiredService(moduleType);
        await Assert.ThrowsAsync<ImportReached>(
            () => (Task)moduleType.GetMethod("HelloAsync")!.Invoke(module, null)!);
    }

    private static ITypeSymbol ResolverParameterOf(Compilation compilation, string moduleTypeName) {
        var module = compilation.GetTypeByMetadataName(moduleTypeName);
        Assert.NotNull(module);
        return module!.InstanceConstructors.Single().Parameters[1].Type.WithNullableAnnotation(NullableAnnotation.None);
    }

    private static string GeneratedSource(BuiltAssembly assembly, string fileName) =>
        assembly.GeneratedTrees.Single(t => Path.GetFileName(t.FilePath) == fileName).GetText().ToString();

    private static BuiltAssembly Build(
        string assemblyName, string modulePath, string moduleSource,
        string? consumerCode = null,
        IEnumerable<MetadataReference>? references = null,
        IReadOnlyDictionary<string, string>? buildProperties = null,
        bool withResolverContract = true) {

        var allReferences = GetFrameworkReferences(withResolverContract).Concat(references ?? []).ToList();
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var trees = consumerCode is null
            ? []
            : new[] { CSharpSyntaxTree.ParseText(consumerCode, parseOptions, path: "Consumer.cs") };

        // Warnings as errors, like every consumer of ours. CS1701 is the benign assembly-version
        // unification between this test host's framework and the packages it references.
        var compilation = CSharpCompilation.Create(assemblyName, trees, allReferences,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable,
                generalDiagnosticOption: ReportDiagnostic.Error,
                specificDiagnosticOptions: new Dictionary<string, ReportDiagnostic> {
                    ["CS1701"] = ReportDiagnostic.Suppress,
                }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { new JsModuleGenerator().AsSourceGenerator() },
            additionalTexts: ImmutableArray.Create<AdditionalText>(new InMemoryAdditionalText(modulePath, moduleSource)),
            parseOptions: parseOptions,
            optionsProvider: new BuildPropertiesProvider(buildProperties ?? new Dictionary<string, string>()));

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);

        using var image = new MemoryStream();
        var emitted = output.Emit(image);

        return new BuiltAssembly(
            output,
            driver.GetRunResult().GeneratedTrees,
            generatorDiagnostics,
            emitted.Diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning).Select(d => d.ToString()).ToList(),
            image.ToArray());
    }

    private static IEnumerable<MetadataReference> GetFrameworkReferences(bool withResolverContract) {
        var contractLocation = typeof(IJsModulePathResolver).Assembly.Location;
        var platformAssemblies = (string?)System.AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "";

        return platformAssemblies.Split(Path.PathSeparator)
            .Where(path => path.Length > 0)
            .Where(path => withResolverContract || path != contractLocation)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path));
    }

    private sealed record BuiltAssembly(
        Compilation Compilation,
        ImmutableArray<SyntaxTree> GeneratedTrees,
        ImmutableArray<Diagnostic> GeneratorDiagnostics,
        List<string> Problems,
        byte[] Image) {
        public MetadataReference Reference => MetadataReference.CreateFromImage(Image);
    }

    private sealed class PerAssemblyResolver : IJsModulePathResolver {
        public string? Resolve(string moduleName) =>
            throw new System.InvalidOperationException("Generated modules must call the assembly-aware overload.");

        public string? Resolve(string moduleName, string assemblyName) => $"/{assemblyName}/{moduleName}.js";
    }

    /// <summary>Thrown at the dynamic import: the path is all these tests need from a module call.</summary>
    private sealed class ImportReached : System.Exception;

    private sealed class ImportRecordingJsRuntime : IJSRuntime {
        public List<string> ImportedPaths { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) {
            ImportedPaths.Add((string)args![0]!);
            throw new ImportReached();
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }

    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText {
        public override string Path { get; } = path;

        public override SourceText? GetText(System.Threading.CancellationToken cancellationToken = default) =>
            SourceText.From(text);
    }

    private sealed class BuildPropertiesProvider(IReadOnlyDictionary<string, string> properties)
        : AnalyzerConfigOptionsProvider {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Options(properties);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        private sealed class Options(IReadOnlyDictionary<string, string> properties) : AnalyzerConfigOptions {
            public override bool TryGetValue(string key, out string value) {
                var found = properties.TryGetValue(key, out var stored);
                value = stored ?? "";
                return found;
            }
        }
    }
}
