namespace BrowserApi.Generator.CSharpModel;

public sealed class CSharpClass {
    public string Name { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string? BaseClass { get; set; }
    public string? JsName { get; set; }
    public string Kind { get; set; } = "interface";
    public List<CSharpProperty> Properties { get; set; } = [];
    public List<CSharpMethod> Methods { get; set; } = [];
    public List<CSharpConstructor> Constructors { get; set; } = [];
    public List<CSharpConst> Constants { get; set; } = [];
}

public sealed class CSharpProperty {
    public string Name { get; set; } = "";
    public string CSharpType { get; set; } = "";
    public bool IsReadOnly { get; set; }
    public bool IsNullable { get; set; }
    public bool IsStatic { get; set; }
    public string? JsName { get; set; }
}

public sealed class CSharpMethod {
    public string Name { get; set; } = "";
    public string ReturnType { get; set; } = "void";
    public bool IsAsync { get; set; }
    public bool IsStatic { get; set; }
    public string? JsName { get; set; }
    public List<CSharpParameter> Parameters { get; set; } = [];
}

public sealed class CSharpConstructor {
    public List<CSharpParameter> Parameters { get; set; } = [];
}

public sealed class CSharpParameter {
    public string Name { get; set; } = "";
    public string CSharpType { get; set; } = "";
    public bool IsOptional { get; set; }
    public bool IsParams { get; set; }
    public string? DefaultValue { get; set; }
}

public sealed class CSharpConst {
    public string Name { get; set; } = "";
    public string CSharpType { get; set; } = "";
    public string Value { get; set; } = "";
}
