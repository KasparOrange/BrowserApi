using System.Collections.Generic;

namespace BrowserApi.SourceGen;

/// <summary>Models a TypeScript interface parsed from a .d.ts file → emitted as a C# record.</summary>
internal sealed class TsInterfaceInfo {
    public string TsName { get; set; } = "";
    public string CSharpName { get; set; } = "";
    public List<TsPropertyInfo> Properties { get; set; } = new();
    public string? Summary { get; set; }
}

/// <summary>Models a property within a TypeScript interface.</summary>
internal sealed class TsPropertyInfo {
    public string TsName { get; set; } = "";
    public string CSharpName { get; set; } = "";
    public string CSharpType { get; set; } = "object";
    public bool IsOptional { get; set; }
    public bool IsRequired { get; set; }
    public string? Summary { get; set; }
}

/// <summary>Models a string literal union type → emitted as a C# enum.</summary>
internal sealed class TsEnumInfo {
    public string CSharpName { get; set; } = "";
    public List<TsEnumMember> Members { get; set; } = new();
}

internal sealed class TsEnumMember {
    public string JsValue { get; set; } = "";
    public string CSharpName { get; set; } = "";
}

/// <summary>A TS type that couldn't be mapped and was silently downgraded to <c>object</c>.</summary>
internal sealed class TsTypeFallback {
    public string TsType { get; set; } = "";
    public string Context { get; set; } = "";
}

/// <summary>Result of parsing a .d.ts file.</summary>
internal sealed class TsParseResult {
    public List<JsFunctionInfo> Functions { get; set; } = new();
    public List<TsInterfaceInfo> Interfaces { get; set; } = new();
    public List<TsEnumInfo> Enums { get; set; } = new();
    /// <summary>Maps TS type names to generated C# type names for parameter resolution.</summary>
    public System.Collections.Generic.Dictionary<string, string> TypeMap { get; set; } = new();
    /// <summary>Unknown TS types that were downgraded to <c>object</c>. Fuel for BAPI002 diagnostics.</summary>
    public List<TsTypeFallback> UnknownTypeFallbacks { get; set; } = new();
}
