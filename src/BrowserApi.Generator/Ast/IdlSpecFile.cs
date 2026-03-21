namespace BrowserApi.Generator.Ast;

public sealed class IdlSpecFile {
    public string? SpecTitle { get; set; }
    public string? SpecUrl { get; set; }
    public Dictionary<string, IdlDefinition> Definitions { get; set; } = [];
    public List<IdlDefinition> PartialDefinitions { get; set; } = [];
    public List<IdlIncludesStatement> IncludesStatements { get; set; } = [];
    public List<string> ExternalDependencies { get; set; } = [];
}
