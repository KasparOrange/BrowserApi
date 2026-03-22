namespace BrowserApi.Runtime.VirtualDom;

public interface IVirtualNode {
    object? GetJsProperty(string jsName);
    void SetJsProperty(string jsName, object? value);
    object? InvokeJsMethod(string jsName, object?[] args);
}
