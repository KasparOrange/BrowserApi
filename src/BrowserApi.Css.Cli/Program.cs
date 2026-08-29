using System.Reflection;
using BrowserApi.Css.Authoring;
using BrowserApi.Css.Compiler;

// browserapi-css — mode-3 static emitter and island watcher.
//
//   browserapi-css build [--project DIR] [--app DLL] [--out FILE] [--prefix P]
//                        [--allow-app-references] [--reference DLL]...
//   browserapi-css watch [same options]
//
// Configuration comes from the app: `--app` points at the built DLL, whose
// [assembly: BrowserApiCssConfig] (emitted by BrowserApi.Css.SourceGen from the
// AddBrowserApiCss lambda in Program.cs) supplies prefix, output path, and the
// app-references flag. Flags exist only as overrides for export scenarios.
// Argument parsing is hand-rolled on purpose: six options don't justify a
// parser package, and the MSBuild target depends on a stable shape.

var (command, opts, error) = Cli.Parse(args);
if (error is not null) {
    Console.Error.WriteLine(error);
    Console.Error.WriteLine(Cli.Usage);
    return 2;
}

if (opts.OutputPath is null) {
    // Invoked unconditionally by the MSBuild target; silence is the correct
    // answer when the app never asked for a static file.
    Console.Error.WriteLine("browserapi-css: StaticAssetPath not set in AddBrowserApiCss (and no --out); nothing to emit.");
    return 0;
}

CssRegistry.Configure(new CssOptions { GlobalPrefix = opts.Prefix });
var references = new List<string>(opts.References);
if (opts.AllowAppReferences) {
    if (opts.AppAssemblyPath is null) {
        Console.Error.WriteLine("browserapi-css: --allow-app-references needs --app DLL.");
        return 2;
    }
    references.AddRange(CssIslandCompiler.AppReferencesFrom(opts.AppAssemblyPath));
}
var compiler = new CssIslandCompiler(references);

switch (command) {
    case "build": {
        var result = compiler.Compile(CssIsland.FindFiles(opts.ProjectDirectory));
        return Cli.Write(result, opts) ? 0 : 1;
    }
    case "watch": {
        using var watcher = new CssIslandWatcher(opts.ProjectDirectory, compiler);
        watcher.Compiled += r => Cli.Write(r, opts);
        watcher.Faulted += ex => Console.Error.WriteLine($"browserapi-css: {ex.Message}");
        watcher.CompileNow();
        watcher.Start();
        Console.Error.WriteLine($"browserapi-css: watching {opts.ProjectDirectory} → {opts.OutputPath} (Ctrl+C to stop)");
        var done = new ManualResetEventSlim();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; done.Set(); };
        done.Wait();
        return 0;
    }
    default:
        Console.Error.WriteLine(Cli.Usage);
        return 2;
}

internal sealed record CliOptions(
    string ProjectDirectory,
    string? AppAssemblyPath,
    string? OutputPath,
    string Prefix,
    bool AllowAppReferences,
    List<string> References);

internal static class Cli {
    public const string Usage = """
        usage: browserapi-css <build|watch> [options]
          --project DIR            project directory containing *.css.cs (default: cwd)
          --app DLL                built app assembly; its [BrowserApiCssConfig] supplies the defaults below
          --out FILE               css file to write (default: <project>/wwwroot/<StaticAssetPath>)
          --prefix P               GlobalPrefix override
          --allow-app-references   let stylesheets reference the app assembly (needs --app)
          --reference DLL          extra assembly the island may reference (repeatable)
        """;

    public static (string? Command, CliOptions Options, string? Error) Parse(string[] args) {
        string? command = null;
        var project = Directory.GetCurrentDirectory();
        string? app = null, output = null, prefix = null;
        bool? allowApp = null;
        var references = new List<string>();

        for (var i = 0; i < args.Length; i++) {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{a} needs a value");
            try {
                switch (a) {
                    case "build" or "watch" when command is null: command = a; break;
                    case "--project": project = Path.GetFullPath(Next()); break;
                    case "--app": app = Path.GetFullPath(Next()); break;
                    case "--out": output = Path.GetFullPath(Next()); break;
                    case "--prefix": prefix = Next(); break;
                    case "--allow-app-references": allowApp = true; break;
                    case "--reference": references.Add(Path.GetFullPath(Next())); break;
                    case "-h" or "--help": return (null, Empty(), Usage);
                    default: return (null, Empty(), $"unknown argument: {a}");
                }
            } catch (ArgumentException ex) {
                return (null, Empty(), ex.Message);
            }
        }
        if (command is null) return (null, Empty(), "missing command");

        // Layer: attribute from the app DLL, then explicit flags on top.
        var fromApp = app is not null ? ReadConfig(app) : null;
        prefix ??= fromApp?.GlobalPrefix ?? string.Empty;
        allowApp ??= fromApp?.AllowAppReferences ?? false;
        if (output is null && !string.IsNullOrEmpty(fromApp?.StaticAssetPath)) {
            output = Path.Combine(project, "wwwroot", fromApp!.StaticAssetPath!.Replace('/', Path.DirectorySeparatorChar));
        }

        return (command, new CliOptions(project, app, output, prefix, allowApp.Value, references), null);

        CliOptions Empty() => new(project, app, output, prefix ?? "", allowApp ?? false, references);
    }

    /// <summary>
    /// Reads <c>[assembly: BrowserApiCssConfig]</c> from a DLL by metadata only —
    /// the app is never loaded for execution, so no startup side effects and no
    /// framework-version coupling.
    /// </summary>
    public static BrowserApiCssConfigAttribute? ReadConfig(string appAssemblyPath) {
        if (!File.Exists(appAssemblyPath)) {
            Console.Error.WriteLine($"browserapi-css: --app '{appAssemblyPath}' not found; using defaults.");
            return null;
        }
        var core = typeof(object).Assembly.Location;
        var paths = new List<string> { core, appAssemblyPath, typeof(BrowserApiCssConfigAttribute).Assembly.Location };
        paths.AddRange(Directory.EnumerateFiles(Path.GetDirectoryName(core)!, "*.dll"));
        using var mlc = new MetadataLoadContext(new PathAssemblyResolver(paths.Distinct()));
        var asm = mlc.LoadFromAssemblyPath(appAssemblyPath);
        foreach (var attr in asm.GetCustomAttributesData()) {
            if (attr.AttributeType.FullName != typeof(BrowserApiCssConfigAttribute).FullName) continue;
            var result = new BrowserApiCssConfigAttribute();
            foreach (var named in attr.NamedArguments) {
                switch (named.MemberName) {
                    case nameof(BrowserApiCssConfigAttribute.GlobalPrefix): result.GlobalPrefix = (string?)named.TypedValue.Value ?? ""; break;
                    case nameof(BrowserApiCssConfigAttribute.StaticAssetPath): result.StaticAssetPath = (string?)named.TypedValue.Value; break;
                    case nameof(BrowserApiCssConfigAttribute.AllowAppReferences): result.AllowAppReferences = named.TypedValue.Value is true; break;
                }
            }
            return result;
        }
        return null;
    }

    /// <summary>Writes the result to disk; returns false (and prints diagnostics) on compile failure.</summary>
    public static bool Write(CssIslandResult result, CliOptions opts) {
        foreach (var d in result.Diagnostics) {
            Console.Error.WriteLine(d.ToString());
        }
        if (!result.Success) {
            Console.Error.WriteLine("browserapi-css: island did not compile; output left unchanged.");
            return false;
        }
        var path = opts.OutputPath!;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // Only touch the file when content changed — keeps file watchers
        // (browser live reload, dotnet watch) quiet on no-op saves.
        var css = result.CombinedCss;
        if (!File.Exists(path) || File.ReadAllText(path) != css) {
            File.WriteAllText(path, css);
            Console.Error.WriteLine($"browserapi-css: wrote {result.CssByType.Count} stylesheet(s) → {path}");
        }
        return true;
    }
}
