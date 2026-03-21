namespace BrowserApi.Generator.Ast;

public sealed class IdlExtendedAttribute {
    public string Name { get; set; } = "";
    public IdlExtAttrRhs? Rhs { get; set; }
    public List<IdlArgument> Arguments { get; set; } = [];
}

public sealed class IdlExtAttrRhs {
    public string Type { get; set; } = "";
    public string? Value { get; set; }
    public List<IdlExtAttrRhsValue> Values { get; set; } = [];
}

public sealed class IdlExtAttrRhsValue {
    public string Value { get; set; } = "";
}
