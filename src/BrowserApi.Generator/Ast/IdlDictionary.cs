namespace BrowserApi.Generator.Ast;

public sealed class IdlDictionary : IdlDefinition {
    public string? Inheritance { get; set; }
    public bool IsPartial { get; set; }
    public List<IdlField> Members { get; set; } = [];
}
