using System.Text.Json;
using BrowserApi.Generator.Input;

namespace BrowserApi.Generator.Css;

public sealed class CssDataReader {
    public List<CssPropertyDefinition> ReadFile(string filePath) {
        var json = File.ReadAllText(filePath);
        using var doc = JsonDocument.Parse(json);
        return ParseProperties(doc.RootElement);
    }

    public List<CssPropertyDefinition> ReadAllFiles(string directoryPath) {
        var files = Directory.GetFiles(directoryPath, "*.json");
        Array.Sort(files);
        var seen = new HashSet<string>();
        var all = new List<CssPropertyDefinition>();
        foreach (var file in files) {
            foreach (var prop in ReadFile(file)) {
                if (seen.Add(prop.Name))
                    all.Add(prop);
            }
        }
        return all;
    }

    internal List<CssPropertyDefinition> ParseProperties(JsonElement root) {
        var result = new List<CssPropertyDefinition>();
        if (!root.TryGetProperty("properties", out var properties))
            return result;

        foreach (var prop in properties.EnumerateArray()) {
            var def = new CssPropertyDefinition {
                Name = prop.GetOptionalString("name") ?? "",
                ValueGrammar = prop.GetOptionalString("value") ?? "",
                Initial = prop.GetOptionalString("initial"),
                IsInherited = prop.GetOptionalString("inherited") == "yes",
                AnimationType = prop.GetOptionalString("animationType")
            };

            if (prop.TryGetProperty("values", out var values)) {
                foreach (var v in values.EnumerateArray()) {
                    def.Values.Add(new CssValueDefinition {
                        Name = v.GetOptionalString("name") ?? "",
                        Type = v.GetOptionalString("type"),
                        Value = v.GetOptionalString("value")
                    });
                }
            }

            if (prop.TryGetProperty("styleDeclaration", out var styleDecl)) {
                foreach (var s in styleDecl.EnumerateArray()) {
                    if (s.GetString() is string name)
                        def.StyleDeclarationNames.Add(name);
                }
            }

            result.Add(def);
        }

        return result;
    }
}
