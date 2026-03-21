namespace BrowserApi.Common;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event)]
public sealed class JsNameAttribute : Attribute {
    public string Name { get; }

    public JsNameAttribute(string name) {
        Name = name;
    }
}
