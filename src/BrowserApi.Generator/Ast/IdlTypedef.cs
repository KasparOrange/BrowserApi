namespace BrowserApi.Generator.Ast;

public sealed class IdlTypedef : IdlDefinition {
    public IdlType Type { get; set; } = new();
}
