namespace BrowserApi.Generator.CSharpModel;

public sealed class CSharpEnum {
    public string Name { get; set; } = "";
    public string Namespace { get; set; } = "";
    public List<CSharpEnumMember> Members { get; set; } = [];
}

public sealed class CSharpEnumMember {
    public string Name { get; set; } = "";
    public string StringValue { get; set; } = "";
}
