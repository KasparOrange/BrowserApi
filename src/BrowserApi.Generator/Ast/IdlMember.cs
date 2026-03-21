namespace BrowserApi.Generator.Ast;

public abstract class IdlMember {
    public string? Name { get; set; }
    public string? Href { get; set; }
    public List<IdlExtendedAttribute> ExtAttrs { get; set; } = [];
}

public sealed class IdlOperation : IdlMember {
    public IdlType ReturnType { get; set; } = new();
    public List<IdlArgument> Arguments { get; set; } = [];
    public string? Special { get; set; }
}

public sealed class IdlAttribute : IdlMember {
    public IdlType Type { get; set; } = new();
    public bool IsReadOnly { get; set; }
    public string? Special { get; set; }
}

public sealed class IdlConstructor : IdlMember {
    public List<IdlArgument> Arguments { get; set; } = [];
}

public sealed class IdlConst : IdlMember {
    public IdlType Type { get; set; } = new();
    public IdlConstValue? Value { get; set; }
}

public sealed class IdlConstValue {
    public string Type { get; set; } = "";
    public object? Value { get; set; }
}

public sealed class IdlField : IdlMember {
    public IdlType Type { get; set; } = new();
    public IdlDefaultValue? Default { get; set; }
    public bool IsRequired { get; set; }
}

public sealed class IdlIterable : IdlMember {
    public List<IdlType> TypeParams { get; set; } = [];
    public bool IsReadOnly { get; set; }
}
