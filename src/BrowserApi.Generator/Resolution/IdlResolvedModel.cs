using BrowserApi.Generator.Ast;

namespace BrowserApi.Generator.Resolution;

public sealed class IdlResolvedModel {
    public Dictionary<string, IdlInterface> Interfaces { get; set; } = [];
    public Dictionary<string, IdlDictionary> Dictionaries { get; set; } = [];
    public Dictionary<string, IdlEnum> Enums { get; set; } = [];
    public Dictionary<string, IdlTypedef> Typedefs { get; set; } = [];
    public Dictionary<string, IdlCallback> Callbacks { get; set; } = [];
    public Dictionary<string, List<string>> InheritanceChains { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
