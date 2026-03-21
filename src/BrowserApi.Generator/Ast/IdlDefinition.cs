namespace BrowserApi.Generator.Ast;

public abstract class IdlDefinition {
    public string Name { get; set; } = "";
    public string? SpecTitle { get; set; }
    public string? SpecUrl { get; set; }
    public string? Href { get; set; }
    public List<IdlExtendedAttribute> ExtAttrs { get; set; } = [];
}
