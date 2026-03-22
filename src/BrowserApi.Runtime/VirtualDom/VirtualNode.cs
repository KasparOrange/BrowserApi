namespace BrowserApi.Runtime.VirtualDom;

/// <summary>
/// Abstract base class for all virtual DOM nodes, providing tree structure (parent, children,
/// siblings) and standard DOM node operations.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="VirtualNode"/> implements the core DOM tree behaviors: <see cref="AppendChild"/>,
/// <see cref="RemoveChild"/>, <see cref="InsertBefore"/>, and traversal properties
/// (<see cref="FirstChild"/>, <see cref="LastChild"/>, <see cref="NextSibling"/>,
/// <see cref="PreviousSibling"/>). It also implements <see cref="IVirtualNode"/> so the
/// <see cref="JintBackend"/> can access these operations through JavaScript-style names.
/// </para>
/// <para>
/// Concrete subclasses include <see cref="VirtualElement"/> (element nodes) and
/// <see cref="VirtualTextNode"/> (text nodes). The <see cref="VirtualDocument"/> class
/// extends this to represent the document root.
/// </para>
/// </remarks>
/// <seealso cref="VirtualElement"/>
/// <seealso cref="VirtualTextNode"/>
/// <seealso cref="VirtualDocument"/>
/// <seealso cref="IVirtualNode"/>
public abstract class VirtualNode : IVirtualNode {
    /// <summary>
    /// Gets or sets the parent node of this node, or <see langword="null"/> if this node is
    /// not attached to a tree.
    /// </summary>
    public VirtualNode? ParentNode { get; internal set; }

    /// <summary>
    /// Gets the ordered list of child nodes belonging to this node.
    /// </summary>
    public List<VirtualNode> ChildNodes { get; } = [];

    /// <summary>
    /// Gets the numeric node type constant, following the DOM specification
    /// (1 = Element, 3 = Text, 9 = Document).
    /// </summary>
    public abstract int NodeType { get; }

    /// <summary>
    /// Gets the node name (e.g., the uppercase tag name for elements, <c>"#text"</c> for text nodes,
    /// <c>"#document"</c> for documents).
    /// </summary>
    public abstract string NodeName { get; }

    /// <summary>
    /// Gets the first child node, or <see langword="null"/> if this node has no children.
    /// </summary>
    public VirtualNode? FirstChild => ChildNodes.Count > 0 ? ChildNodes[0] : null;

    /// <summary>
    /// Gets the last child node, or <see langword="null"/> if this node has no children.
    /// </summary>
    public VirtualNode? LastChild => ChildNodes.Count > 0 ? ChildNodes[^1] : null;

    /// <summary>
    /// Gets the next sibling node in the parent's child list, or <see langword="null"/> if
    /// this is the last child or has no parent.
    /// </summary>
    public VirtualNode? NextSibling {
        get {
            if (ParentNode is null) return null;
            var idx = ParentNode.ChildNodes.IndexOf(this);
            return idx >= 0 && idx + 1 < ParentNode.ChildNodes.Count ? ParentNode.ChildNodes[idx + 1] : null;
        }
    }

    /// <summary>
    /// Gets the previous sibling node in the parent's child list, or <see langword="null"/> if
    /// this is the first child or has no parent.
    /// </summary>
    public VirtualNode? PreviousSibling {
        get {
            if (ParentNode is null) return null;
            var idx = ParentNode.ChildNodes.IndexOf(this);
            return idx > 0 ? ParentNode.ChildNodes[idx - 1] : null;
        }
    }

    /// <summary>
    /// Gets or sets the concatenated text content of this node and all its descendants.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When read, returns the concatenation of all descendant text nodes' content.
    /// When set, replaces all child nodes with a single text node containing the given value.
    /// </para>
    /// </remarks>
    public virtual string TextContent {
        get => string.Concat(ChildNodes.Select(c => c.TextContent));
        set {
            ChildNodes.Clear();
            if (!string.IsNullOrEmpty(value))
                AppendChild(new VirtualTextNode(value));
        }
    }

    /// <summary>
    /// Appends a child node to the end of this node's child list.
    /// </summary>
    /// <param name="child">
    /// The node to append. If it already has a parent, it is removed from its current parent first.
    /// </param>
    /// <returns>The appended child node.</returns>
    public VirtualNode AppendChild(VirtualNode child) {
        child.ParentNode?.RemoveChild(child);
        child.ParentNode = this;
        ChildNodes.Add(child);
        return child;
    }

    /// <summary>
    /// Removes the specified child node from this node's child list.
    /// </summary>
    /// <param name="child">The child node to remove.</param>
    /// <returns>The removed child node.</returns>
    public VirtualNode RemoveChild(VirtualNode child) {
        ChildNodes.Remove(child);
        child.ParentNode = null;
        return child;
    }

    /// <summary>
    /// Inserts a new child node before an existing reference child node.
    /// </summary>
    /// <param name="newChild">
    /// The node to insert. If it already has a parent, it is removed from its current parent first.
    /// </param>
    /// <param name="refChild">
    /// The reference child before which <paramref name="newChild"/> is inserted.
    /// If <see langword="null"/>, <paramref name="newChild"/> is appended to the end.
    /// </param>
    /// <returns>The inserted child node.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="refChild"/> is not a child of this node.
    /// </exception>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public virtual void SetJsProperty(string jsName, object? value) {
        switch (jsName) {
            case "textContent":
                TextContent = value?.ToString() ?? "";
                break;
        }
    }

    /// <inheritdoc/>
    public virtual object? InvokeJsMethod(string jsName, object?[] args) {
        return jsName switch {
            "appendChild" => AppendChild((VirtualNode)args[0]!),
            "removeChild" => RemoveChild((VirtualNode)args[0]!),
            "insertBefore" => InsertBefore((VirtualNode)args[0]!, args[1] as VirtualNode),
            _ => null
        };
    }

    /// <summary>
    /// Enumerates this node and all its descendants in depth-first order.
    /// </summary>
    /// <returns>An enumerable of this node followed by all descendant nodes.</returns>
    protected IEnumerable<VirtualNode> DescendantsAndSelf() {
        yield return this;
        foreach (var child in ChildNodes)
            foreach (var desc in child.DescendantsAndSelf())
                yield return desc;
    }
}
