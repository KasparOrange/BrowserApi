namespace BrowserApi.Generator.Ast;

public sealed class IdlType {
    public string? TypeName { get; set; }
    public string? Generic { get; set; }
    public bool IsNullable { get; set; }
    public bool IsUnion { get; set; }
    public List<IdlType> TypeArguments { get; set; } = [];
    public List<IdlType> UnionMemberTypes { get; set; } = [];
    public List<IdlExtendedAttribute> ExtAttrs { get; set; } = [];
}
