namespace BrowserApi.Generator.Ast;

public sealed class IdlInterface : IdlDefinition {
    public string Kind { get; set; } = "interface";
    public string? Inheritance { get; set; }
    public bool IsPartial { get; set; }
    public List<IdlMember> Members { get; set; } = [];
}
