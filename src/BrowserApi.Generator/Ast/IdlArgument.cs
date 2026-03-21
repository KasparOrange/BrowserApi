namespace BrowserApi.Generator.Ast;

public sealed class IdlArgument {
    public string Name { get; set; } = "";
    public IdlType Type { get; set; } = new();
    public bool IsOptional { get; set; }
    public bool IsVariadic { get; set; }
    public IdlDefaultValue? Default { get; set; }
    public List<IdlExtendedAttribute> ExtAttrs { get; set; } = [];
}
