namespace BrowserApi.Generator.Ast;

public sealed class IdlCallback : IdlDefinition {
    public IdlType ReturnType { get; set; } = new();
    public List<IdlArgument> Arguments { get; set; } = [];
}
