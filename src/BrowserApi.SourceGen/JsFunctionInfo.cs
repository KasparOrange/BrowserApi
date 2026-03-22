using System.Collections.Generic;

namespace BrowserApi.SourceGen;

internal sealed class JsFunctionInfo {
    public string JsName { get; set; } = "";
    public string CSharpName { get; set; } = "";
    public string? ReturnType { get; set; }
    public List<JsParamInfo> Params { get; set; } = new();
    public string? Summary { get; set; }
    public string? ReturnsDoc { get; set; }
}

internal sealed class JsParamInfo {
    public string Name { get; set; } = "";
    public string CSharpName { get; set; } = "";
    public string CSharpType { get; set; } = "object";
    public string? Description { get; set; }
}
