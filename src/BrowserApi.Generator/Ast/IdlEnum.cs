namespace BrowserApi.Generator.Ast;

public sealed class IdlEnum : IdlDefinition {
    public List<IdlEnumValue> Values { get; set; } = [];
}

public sealed class IdlEnumValue {
    public string Value { get; set; } = "";
    public string? Href { get; set; }
}
