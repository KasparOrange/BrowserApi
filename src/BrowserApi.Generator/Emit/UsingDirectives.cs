namespace BrowserApi.Generator.Emit;

internal static class UsingDirectives {
    public static void WriteUsings(CSharpCodeWriter w, string currentNamespace, IReadOnlySet<string>? allNamespaces = null) {
        w.AppendLine("using BrowserApi.Common;");

        if (allNamespaces == null)
            return;

        foreach (var ns in allNamespaces.Order()) {
            if (ns != currentNamespace && ns != "BrowserApi.Common")
                w.AppendLine($"using {ns};");
        }
    }
}
