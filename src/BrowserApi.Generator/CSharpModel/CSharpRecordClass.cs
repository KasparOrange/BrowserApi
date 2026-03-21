namespace BrowserApi.Generator.CSharpModel;

public sealed class CSharpRecordClass {
    public string Name { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string? BaseRecord { get; set; }
    public string? JsName { get; set; }
    public List<CSharpRecordProperty> Properties { get; set; } = [];
}

public sealed class CSharpRecordProperty {
    public string Name { get; set; } = "";
    public string CSharpType { get; set; } = "";
    public bool IsRequired { get; set; }
    public bool IsNullable { get; set; }
    public string? JsName { get; set; }
}
