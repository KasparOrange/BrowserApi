namespace BrowserApi.Generator.CSharpModel;

public sealed class CSharpDelegate {
    public string Name { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string ReturnType { get; set; } = "void";
    public List<CSharpParameter> Parameters { get; set; } = [];
}
