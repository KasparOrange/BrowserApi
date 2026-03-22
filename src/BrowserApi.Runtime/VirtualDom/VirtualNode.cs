namespace BrowserApi.Runtime.VirtualDom;

public abstract class VirtualNode : IVirtualNode {
    public VirtualNode? ParentNode { get; internal set; }
    public List<VirtualNode> ChildNodes { get; } = [];
    public abstract int NodeType { get; }
    public abstract string NodeName { get; }

    public VirtualNode? FirstChild => ChildNodes.Count > 0 ? ChildNodes[0] : null;
    public VirtualNode? LastChild => ChildNodes.Count > 0 ? ChildNodes[^1] : null;

    public VirtualNode? NextSibling {
        get {
            if (ParentNode is null) return null;
            var idx = ParentNode.ChildNodes.IndexOf(this);
            return idx >= 0 && idx + 1 < ParentNode.ChildNodes.Count ? ParentNode.ChildNodes[idx + 1] : null;
        }
    }

    public VirtualNode? PreviousSibling {
        get {
            if (ParentNode is null) return null;
            var idx = ParentNode.ChildNodes.IndexOf(this);
            return idx > 0 ? ParentNode.ChildNodes[idx - 1] : null;
        }
    }

    public virtual string TextContent {
        get => string.Concat(ChildNodes.Select(c => c.TextContent));
        set {
            ChildNodes.Clear();
            if (!string.IsNullOrEmpty(value))
                AppendChild(new VirtualTextNode(value));
        }
    }

    public VirtualNode AppendChild(VirtualNode child) {
        child.ParentNode?.RemoveChild(child);
        child.ParentNode = this;
        ChildNodes.Add(child);
        return child;
    }

    public VirtualNode RemoveChild(VirtualNode child) {
        ChildNodes.Remove(child);
        child.ParentNode = null;
        return child;
    }

    public VirtualNode InsertBefore(VirtualNode newChild, VirtualNode? refChild) {
        newChild.ParentNode?.RemoveChild(newChild);
        newChild.ParentNode = this;
        if (refChild is null) {
            ChildNodes.Add(newChild);
        } else {
            var idx = ChildNodes.IndexOf(refChild);
            if (idx < 0) throw new InvalidOperationException("Reference node is not a child.");
            ChildNodes.Insert(idx, newChild);
        }
        return newChild;
    }

    public virtual object? GetJsProperty(string jsName) {
        return jsName switch {
            "parentNode" => ParentNode,
            "childNodes" => ChildNodes,
            "firstChild" => FirstChild,
            "lastChild" => LastChild,
            "nextSibling" => NextSibling,
            "previousSibling" => PreviousSibling,
            "nodeType" => NodeType,
            "nodeName" => NodeName,
            "textContent" => TextContent,
            _ => null
        };
    }

    public virtual void SetJsProperty(string jsName, object? value) {
        switch (jsName) {
            case "textContent":
                TextContent = value?.ToString() ?? "";
                break;
        }
    }

    public virtual object? InvokeJsMethod(string jsName, object?[] args) {
        return jsName switch {
            "appendChild" => AppendChild((VirtualNode)args[0]!),
            "removeChild" => RemoveChild((VirtualNode)args[0]!),
            "insertBefore" => InsertBefore((VirtualNode)args[0]!, args[1] as VirtualNode),
            _ => null
        };
    }

    protected IEnumerable<VirtualNode> DescendantsAndSelf() {
        yield return this;
        foreach (var child in ChildNodes)
            foreach (var desc in child.DescendantsAndSelf())
                yield return desc;
    }
}
