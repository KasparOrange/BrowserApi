namespace BrowserApi.Generator.Css;

public sealed class CssPropertyDefinition {
    public string Name { get; set; } = "";
    public string ValueGrammar { get; set; } = "";
    public string? Initial { get; set; }
    public bool IsInherited { get; set; }
    public string? AnimationType { get; set; }
    public List<CssValueDefinition> Values { get; set; } = [];
    public List<string> StyleDeclarationNames { get; set; } = [];
}

public sealed class CssValueDefinition {
    public string Name { get; set; } = "";
    public string? Type { get; set; }
    public string? Value { get; set; }
}
