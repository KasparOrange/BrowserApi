using BrowserApi.Generator.CSharpModel;

namespace BrowserApi.Generator.Emit;

public sealed class FileEmitter {
    public int EmitAll(CSharpGeneratedModel model, string outputDir, bool dryRun = false) {
        // Collect all namespaces used in the model
        var allNamespaces = new HashSet<string>();
        foreach (var c in model.Classes) allNamespaces.Add(c.Namespace);
        foreach (var e in model.Enums) allNamespaces.Add(e.Namespace);
        foreach (var r in model.RecordClasses) allNamespaces.Add(r.Namespace);
        foreach (var d in model.Delegates) allNamespaces.Add(d.Namespace);

        var count = 0;

        foreach (var csClass in model.Classes) {
            var code = ClassEmitter.Emit(csClass, allNamespaces);
            WriteFile(outputDir, csClass.Namespace, csClass.Name, code, dryRun);
            count++;
        }

        foreach (var csEnum in model.Enums) {
            var code = EnumEmitter.Emit(csEnum);
            WriteFile(outputDir, csEnum.Namespace, csEnum.Name, code, dryRun);
            count++;
        }

        foreach (var rec in model.RecordClasses) {
            var code = DictionaryEmitter.Emit(rec, allNamespaces);
            WriteFile(outputDir, rec.Namespace, rec.Name, code, dryRun);
            count++;
        }

        foreach (var del in model.Delegates) {
            var code = CallbackEmitter.Emit(del, allNamespaces);
            WriteFile(outputDir, del.Namespace, del.Name, code, dryRun);
            count++;
        }

        return count;
    }

    private void WriteFile(string outputDir, string ns, string typeName, string code, bool dryRun) {
        var subDir = ns.Replace("BrowserApi.", "").Replace("BrowserApi", "").Replace('.', '/');
        var dir = string.IsNullOrEmpty(subDir)
            ? outputDir
            : Path.Combine(outputDir, subDir);

        var filePath = Path.Combine(dir, $"{typeName}.g.cs");

        if (dryRun) {
            Console.WriteLine($"  [dry-run] {filePath}");
            return;
        }

        Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, code);
    }
}
