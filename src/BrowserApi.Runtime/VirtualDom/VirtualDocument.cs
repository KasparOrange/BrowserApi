namespace BrowserApi.Runtime.VirtualDom;

public class VirtualDocument : VirtualNode {
    public VirtualElement DocumentElement { get; }
    public VirtualElement Head { get; }
    public VirtualElement Body { get; }

    public override int NodeType => 9;
    public override string NodeName => "#document";

    public VirtualDocument() {
        DocumentElement = new VirtualElement("html");
        Head = new VirtualElement("head");
        Body = new VirtualElement("body");
        DocumentElement.AppendChild(Head);
        DocumentElement.AppendChild(Body);
        AppendChild(DocumentElement);
    }

    public VirtualElement CreateElement(string tagName) => new(tagName);

    public VirtualTextNode CreateTextNode(string data) => new(data);

    public VirtualElement? GetElementById(string id) {
        return DescendantsAndSelf()
            .OfType<VirtualElement>()
            .FirstOrDefault(el => el.Id == id);
    }

    public VirtualElement? QuerySelector(string selector) {
        return DescendantsAndSelf()
            .OfType<VirtualElement>()
            .FirstOrDefault(el => SimpleSelector.Matches(el, selector));
    }

    public List<VirtualElement> QuerySelectorAll(string selector) {
        return DescendantsAndSelf()
            .OfType<VirtualElement>()
            .Where(el => SimpleSelector.Matches(el, selector))
            .ToList();
    }

    public override object? GetJsProperty(string jsName) {
        return jsName switch {
            "documentElement" => DocumentElement,
            "body" => Body,
            "head" => Head,
            _ => base.GetJsProperty(jsName)
        };
    }

    public override object? InvokeJsMethod(string jsName, object?[] args) {
        return jsName switch {
            "createElement" => CreateElement((string)args[0]!),
            "createTextNode" => CreateTextNode((string)args[0]!),
            "getElementById" => GetElementById((string)args[0]!),
            "querySelector" => QuerySelector((string)args[0]!),
            "querySelectorAll" => QuerySelectorAll((string)args[0]!),
            _ => base.InvokeJsMethod(jsName, args)
        };
    }
}
