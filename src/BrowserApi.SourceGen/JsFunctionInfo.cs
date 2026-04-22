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

    /// <summary>
    /// True when the .d.ts declared this parameter as `DotNetObjectReference` (with or
    /// without a type argument) at the top level. Signals the generator to promote the
    /// whole method to generic-over-<c>TDotNetRefN</c> and emit the parameter as
    /// <c>Microsoft.JSInterop.DotNetObjectReference&lt;TDotNetRefN&gt;</c>, so consumers
    /// pass their own <c>DotNetObjectReference&lt;T&gt;</c> with full type inference.
    /// </summary>
    public bool IsDotNetObjectRef { get; set; }

    /// <summary>Whether the parameter is nullable (corresponds to `?` in the .d.ts).</summary>
    public bool IsOptional { get; set; }
}
