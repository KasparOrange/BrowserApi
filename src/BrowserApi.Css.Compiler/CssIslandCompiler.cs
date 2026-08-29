using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using BrowserApi.Css.Authoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace BrowserApi.Css.Compiler;

// Our namespace sits under BrowserApi.Css, where the CSSOM `StyleSheet` lives.
// Enclosing-namespace lookup beats file-level aliases, so the alias must be
// declared here, inside the namespace, to win.
using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;

/// <summary>
/// Compiles a set of <c>*.css.cs</c> files into a throwaway assembly, loads it
/// into a collectible <see cref="AssemblyLoadContext"/>, and renders every
/// stylesheet in it through <see cref="StyleSheet.RenderAll"/> — the same
/// renderer the runtime <c>&lt;BrowserApiCss /&gt;</c> path uses. Output is
/// therefore identical to mode 1 by construction.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why this works in-process.</strong> The island assembly references
/// <c>BrowserApi</c>, which the load context resolves from the default context
/// (the one already holding the app's copy). Island types thus derive from the
/// <em>same</em> <see cref="StyleSheet"/> type the app uses, so the renderer,
/// prefix options, and the late-binding <c>CssVar</c> registry all apply unchanged.
/// </para>
/// <para>
/// <strong>Why the app's own stylesheet fields stay untouched.</strong> They
/// are <c>static readonly</c>, initialized once by the type initializer; the
/// CLR offers no way to re-run that. Instead of mutating them, the island is a
/// <em>second copy</em> of the stylesheets whose CSS replaces the registry
/// output. Class names are a pure function of field name + prefix, so the
/// app's <c>AppStyles.Btn</c> references keep resolving to the same selectors.
/// </para>
/// <para>
/// Known dev-only leak: <c>CssVar.Or()</c> and unnamed-variable references
/// register late-bound closures in a process-wide table, which pins each island
/// assembly after unload. A few KB per save — acceptable for a dev loop, not
/// something to run in a long-lived production process.
/// </para>
/// </remarks>
public sealed class CssIslandCompiler {
    private static int _islandCounter;

    private readonly ImmutableArray<MetadataReference> _references;
    private readonly CSharpParseOptions _parseOptions;
    private readonly SyntaxTree _globalUsings;

    /// <summary>Creates a compiler whose reference set is resolved once and reused across compiles.</summary>
    /// <param name="additionalReferencePaths">
    /// Extra assemblies the island may reference. The framework and
    /// <c>BrowserApi</c> are always included. For <see cref="CssOptions.AllowAppReferences"/>
    /// pass <see cref="AppReferences"/>.
    /// </param>
    public CssIslandCompiler(IEnumerable<string>? additionalReferencePaths = null) {
        _references = ResolveReferences(additionalReferencePaths);
        _parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        // Mirror the SDK's ImplicitUsings so island files compile exactly as
        // they do inside the app project.
        _globalUsings = CSharpSyntaxTree.ParseText(
            "global using System;\n" +
            "global using System.Collections.Generic;\n" +
            "global using System.IO;\n" +
            "global using System.Linq;\n" +
            "global using System.Net.Http;\n" +
            "global using System.Threading;\n" +
            "global using System.Threading.Tasks;\n",
            _parseOptions, path: "BrowserApi.CssIsland.GlobalUsings.g.cs");
    }

    /// <summary>Compiles and renders the given island files read from disk.</summary>
    public CssIslandResult Compile(IEnumerable<string> files) {
        var sources = new List<(string Path, string Text)>();
        foreach (var file in files) {
            sources.Add((file, File.ReadAllText(file)));
        }
        return CompileSources(sources);
    }

    /// <summary>
    /// Compiles and renders in-memory sources. <c>Path</c> is used only for
    /// diagnostics and (later) source maps.
    /// </summary>
    public CssIslandResult CompileSources(IEnumerable<(string Path, string Text)> sources) {
        var trees = new List<SyntaxTree> { _globalUsings };
        foreach (var (path, text) in sources) {
            trees.Add(CSharpSyntaxTree.ParseText(SourceText.From(text), _parseOptions, path: path));
        }

        var assemblyName = CssRegistry.IslandAssemblyPrefix + Interlocked.Increment(ref _islandCounter);
        var compilation = CSharpCompilation.Create(
            assemblyName,
            trees,
            _references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable,
                optimizationLevel: OptimizationLevel.Debug,
                // With AllowAppReferences the app assembly is a reference AND the
                // island re-declares the same stylesheet types. Roslyn resolves
                // source over metadata (what we want) and warns CS0436 about it;
                // that warning is noise here, not a user mistake.
                specificDiagnosticOptions: new[] {
                    new KeyValuePair<string, ReportDiagnostic>("CS0436", ReportDiagnostic.Suppress),
                }));

        using var stream = new MemoryStream();
        var emit = compilation.Emit(stream);
        var diagnostics = emit.Diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning).ToImmutableArray();
        if (!emit.Success) {
            return CssIslandResult.Failed(diagnostics);
        }

        stream.Position = 0;
        var context = new CssIslandLoadContext(assemblyName);
        try {
            var assembly = context.LoadFromStream(stream);
            var types = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && typeof(StyleSheet).IsAssignableFrom(t))
                .OrderBy(t => t.FullName, StringComparer.Ordinal)
                .ToList();
            var cssByType = StyleSheet.RenderAll(types);
            return CssIslandResult.Succeeded(cssByType, diagnostics);
        } finally {
            // Collectible; actually reclaimed once nothing references its types.
            context.Unload();
        }
    }

    /// <summary>
    /// Reference set for <see cref="CssOptions.AllowAppReferences"/>: the app
    /// assembly plus everything it may transitively need that is already loaded
    /// in this process. Only what exists on disk; dynamic/in-memory assemblies
    /// are skipped. In-process (hot reload) this is the live app; the CLI
    /// instead passes the built DLL's directory via <see cref="AppReferencesFrom"/>.
    /// </summary>
    public static IEnumerable<string> AppReferences() =>
        AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Where(a => !(a.GetName().Name ?? "").StartsWith(CssRegistry.IslandAssemblyPrefix, StringComparison.Ordinal))
            .Select(a => a.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    /// <summary>Every managed DLL next to <paramref name="appAssemblyPath"/> — the app's build output.</summary>
    public static IEnumerable<string> AppReferencesFrom(string appAssemblyPath) {
        var dir = Path.GetDirectoryName(Path.GetFullPath(appAssemblyPath))!;
        foreach (var dll in Directory.EnumerateFiles(dir, "*.dll")) {
            if (Path.GetFileName(dll).Equals("BrowserApi.dll", StringComparison.OrdinalIgnoreCase)) continue; // always the host's copy
            if (IsManagedAssembly(dll)) yield return dll;
        }
    }

    private static bool IsManagedAssembly(string path) {
        try { System.Reflection.AssemblyName.GetAssemblyName(path); return true; }
        catch { return false; }
    }

    private static ImmutableArray<MetadataReference> ResolveReferences(IEnumerable<string>? additional) {
        var refs = ImmutableArray.CreateBuilder<MetadataReference>();

        // The framework the current process runs on. Implementation assemblies
        // are fine as compile references here — the island is loaded straight
        // back into this same process, never shipped.
        var tpa = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string) ?? string.Empty;
        foreach (var path in tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)) {
            var name = Path.GetFileNameWithoutExtension(path);
            if (name.StartsWith("System", StringComparison.Ordinal) ||
                name == "mscorlib" || name == "netstandard" ||
                name.StartsWith("Microsoft.CSharp", StringComparison.Ordinal)) {
                refs.Add(MetadataReference.CreateFromFile(path));
            }
        }

        var browserApi = typeof(StyleSheet).Assembly;
        if (string.IsNullOrEmpty(browserApi.Location)) {
            throw new InvalidOperationException("BrowserApi must be loaded from disk to serve as an island reference.");
        }
        refs.Add(MetadataReference.CreateFromFile(browserApi.Location));

        var seen = new HashSet<string>(refs.Select(r => ((PortableExecutableReference)r).FilePath ?? ""), StringComparer.OrdinalIgnoreCase);
        foreach (var path in additional ?? Array.Empty<string>()) {
            if (!seen.Add(path)) continue;
            // Same-named assembly already referenced (e.g. BrowserApi from bin/ vs the
            // host's copy) → keep the first; two identities would be ambiguous.
            if (seen.Any(p => Path.GetFileName(p).Equals(Path.GetFileName(path), StringComparison.OrdinalIgnoreCase) && p != path)) continue;
            refs.Add(MetadataReference.CreateFromFile(path));
        }
        return refs.ToImmutable();
    }

    /// <summary>
    /// Collectible context that owns nothing but the island assembly: every
    /// other name (BrowserApi, the framework) falls through to the default
    /// context so type identity is shared with the host app.
    /// </summary>
    private sealed class CssIslandLoadContext : AssemblyLoadContext {
        public CssIslandLoadContext(string name) : base(name, isCollectible: true) { }
        protected override Assembly? Load(AssemblyName assemblyName) => null;
    }
}

/// <summary>Outcome of one island compile + render.</summary>
public sealed class CssIslandResult {
    private CssIslandResult(bool success, IReadOnlyDictionary<string, string> cssByType, ImmutableArray<Diagnostic> diagnostics) {
        Success = success;
        CssByType = cssByType;
        Diagnostics = diagnostics;
    }

    /// <summary>False when the island did not compile; <see cref="Diagnostics"/> says why.</summary>
    public bool Success { get; }

    /// <summary>Rendered CSS keyed by stylesheet type full name, in deterministic (ordinal full-name) order. Empty on failure.</summary>
    public IReadOnlyDictionary<string, string> CssByType { get; }

    /// <summary>Warnings and errors from the compile.</summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>All stylesheets concatenated — the mode-3 file body and the mode-1 <c>&lt;style&gt;</c> body.</summary>
    public string CombinedCss => CssRegistry.Combine(CssByType.Values);

    internal static CssIslandResult Succeeded(IReadOnlyDictionary<string, string> cssByType, ImmutableArray<Diagnostic> diagnostics) =>
        new(true, cssByType, diagnostics);

    internal static CssIslandResult Failed(ImmutableArray<Diagnostic> diagnostics) =>
        new(false, new Dictionary<string, string>(), diagnostics);
}
