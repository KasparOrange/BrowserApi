using BrowserApi.Generator.Css;
using BrowserApi.Generator.Emit;
using BrowserApi.Generator.Input;
using BrowserApi.Generator.Resolution;
using BrowserApi.Generator.Transform;

var idlparsedDir = "";
var cssDataDir = "";
var outputDir = "";
var dryRun = false;
var specsFilter = new HashSet<string>();

for (var i = 0; i < args.Length; i++) {
    switch (args[i]) {
        case "--idlparsed" when i + 1 < args.Length:
            idlparsedDir = args[++i];
            break;
        case "--css-data" when i + 1 < args.Length:
            cssDataDir = args[++i];
            break;
        case "--output" when i + 1 < args.Length:
            outputDir = args[++i];
            break;
        case "--specs" when i + 1 < args.Length:
            foreach (var s in args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries))
                specsFilter.Add(s.Trim());
            break;
        case "--dry-run":
            dryRun = true;
            break;
        case "--help" or "-h":
            PrintUsage();
            return;
    }
}

if (string.IsNullOrEmpty(idlparsedDir) || string.IsNullOrEmpty(outputDir)) {
    Console.Error.WriteLine("Error: --idlparsed and --output are required.");
    PrintUsage();
    Environment.Exit(1);
}

// WebIDL pipeline
Console.WriteLine($"Reading specs from {idlparsedDir}...");
var reader = new WebRefJsonReader();
var specFiles = reader.ReadAllSpecs(idlparsedDir);
Console.WriteLine($"  Parsed {specFiles.Count} spec files");

Console.WriteLine("Resolving cross-references...");
var resolver = new IdlResolver();
var model = resolver.Resolve(specFiles);
Console.WriteLine($"  {model.Interfaces.Count} interfaces, {model.Enums.Count} enums, {model.Dictionaries.Count} dictionaries, {model.Typedefs.Count} typedefs, {model.Callbacks.Count} callbacks");

if (model.Warnings.Count > 0) {
    Console.WriteLine($"  {model.Warnings.Count} warnings (use --verbose to see)");
}

Console.WriteLine("Transforming to C# model...");
var transformer = new IdlToCSharpTransformer(model);
var csModel = transformer.Transform(model);
Console.WriteLine($"  {csModel.Classes.Count} classes, {csModel.Enums.Count} enums, {csModel.RecordClasses.Count} records, {csModel.Delegates.Count} delegates");

Console.WriteLine($"Emitting C# files to {outputDir}...");
var fileEmitter = new FileEmitter();
var count = fileEmitter.EmitAll(csModel, outputDir, dryRun);
Console.WriteLine($"  {(dryRun ? "Would write" : "Wrote")} {count} files");

// CSS pipeline (if css-data provided)
if (!string.IsNullOrEmpty(cssDataDir)) {
    Console.WriteLine($"Reading CSS data from {cssDataDir}...");
    var cssReader = new CssDataReader();
    var cssProperties = cssReader.ReadAllFiles(cssDataDir);
    Console.WriteLine($"  Parsed {cssProperties.Count} CSS properties");

    // Emit CssStyleDeclaration
    var cssOutputDir = Path.Combine(outputDir, "Css");
    var styleDeclCode = CssStyleDeclarationEmitter.Emit(cssProperties);
    if (!dryRun) {
        Directory.CreateDirectory(cssOutputDir);
        File.WriteAllText(Path.Combine(cssOutputDir, "CssStyleDeclaration.g.cs"), styleDeclCode);
    }
    Console.WriteLine($"  {(dryRun ? "Would write" : "Wrote")} CssStyleDeclaration.g.cs");

    // Emit CSS enums for keyword-only properties
    var cssEnumCount = 0;
    foreach (var prop in cssProperties) {
        var enumCode = CssEnumEmitter.TryEmit(prop);
        if (enumCode != null) {
            var enumName = NamingConventions.ToPascalCase(prop.Name);
            if (!dryRun) {
                Directory.CreateDirectory(cssOutputDir);
                File.WriteAllText(Path.Combine(cssOutputDir, $"{enumName}.g.cs"), enumCode);
            }
            cssEnumCount++;
        }
    }
    Console.WriteLine($"  {(dryRun ? "Would write" : "Wrote")} {cssEnumCount} CSS enum files");

    // Emit CSS value type stubs
    var stubs = CssValueTypeStubEmitter.EmitAll();
    foreach (var (typeName, code) in stubs) {
        if (!dryRun) {
            Directory.CreateDirectory(cssOutputDir);
            File.WriteAllText(Path.Combine(cssOutputDir, $"{typeName}.g.cs"), code);
        }
    }
    Console.WriteLine($"  {(dryRun ? "Would write" : "Wrote")} {stubs.Count} CSS value type stubs");
}

Console.WriteLine("Done.");

static void PrintUsage() {
    Console.WriteLine("""
    BrowserApi.Generator — WebIDL + CSS spec → C# code generator

    Usage:
      dotnet run -- --idlparsed <dir> --output <dir> [options]

    Options:
      --idlparsed <dir>   Directory with pre-parsed WebIDL JSON files (required)
      --css-data <dir>    Directory with CSS property JSON files
      --output <dir>      Output directory for generated C# files (required)
      --specs <names>     Comma-separated list of spec names to process
      --dry-run           Print file paths without writing
      --help, -h          Show this help
    """);
}
