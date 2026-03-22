namespace BrowserApi.Runtime.VirtualDom;

public class VirtualTextNode : VirtualNode {
    public string Data { get; set; }

    public override int NodeType => 3;
    public override string NodeName => "#text";
    public override string TextContent {
        get => Data;
        set => Data = value;
    }

    public VirtualTextNode(string data) {
        Data = data;
    }

    public override object? GetJsProperty(string jsName) {
        return jsName switch {
            "data" => Data,
            "length" => Data.Length,
            _ => base.GetJsProperty(jsName)
        };
    }

    public override void SetJsProperty(string jsName, object? value) {
        switch (jsName) {
            case "data": Data = value?.ToString() ?? ""; break;
            default: base.SetJsProperty(jsName, value); break;
        }
    }
}
