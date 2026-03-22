using BrowserApi.Runtime.VirtualDom;

namespace BrowserApi.Runtime.Tests.VirtualDom;

public class VirtualElementTests {
    [Fact]
    public void Id_get_set() {
        var el = new VirtualElement("div");
        el.Id = "main";
        Assert.Equal("main", el.Id);
    }

    [Fact]
    public void ClassName_get_set() {
        var el = new VirtualElement("div");
        el.ClassName = "card active";
        Assert.Equal("card active", el.ClassName);
    }

    [Fact]
    public void TextContent_get_set() {
        var el = new VirtualElement("p");
        el.TextContent = "hello world";
        Assert.Equal("hello world", el.TextContent);
    }

    [Fact]
    public void SetAttribute_GetAttribute() {
        var el = new VirtualElement("input");
        el.SetAttribute("type", "text");
        Assert.Equal("text", el.GetAttribute("type"));
    }

    [Fact]
    public void RemoveAttribute() {
        var el = new VirtualElement("input");
        el.SetAttribute("type", "text");
        el.RemoveAttribute("type");
        Assert.Null(el.GetAttribute("type"));
    }

    [Fact]
    public void HasAttribute() {
        var el = new VirtualElement("input");
        Assert.False(el.HasAttribute("type"));
        el.SetAttribute("type", "text");
        Assert.True(el.HasAttribute("type"));
    }

    [Fact]
    public void Style_property_access() {
        var el = new VirtualElement("div");
        el.Style["display"] = "flex";
        el.Style["gap"] = "1rem";

        Assert.Equal("flex", el.Style["display"]);
        Assert.Equal("1rem", el.Style["gap"]);
    }

    [Fact]
    public void Style_via_js_property() {
        var el = new VirtualElement("div");
        el.Style.SetJsProperty("backgroundColor", "red");

        Assert.Equal("red", el.Style["background-color"]);
    }

    [Fact]
    public void Children_returns_element_children_only() {
        var el = new VirtualElement("div");
        el.AppendChild(new VirtualElement("span"));
        el.AppendChild(new VirtualTextNode("text"));
        el.AppendChild(new VirtualElement("p"));

        Assert.Equal(3, el.ChildNodes.Count);
        Assert.Equal(2, el.Children.Count);
    }

    [Fact]
    public void InnerHtml_serializes_children() {
        var div = new VirtualElement("div");
        div.Id = "main";
        var p = new VirtualElement("p");
        p.AppendChild(new VirtualTextNode("Hello"));
        div.AppendChild(p);

        Assert.Equal("<p>Hello</p>", div.InnerHtml);
    }

    [Fact]
    public void OuterHtml_includes_self() {
        var div = new VirtualElement("div");
        div.Id = "main";
        div.ClassName = "card";
        div.AppendChild(new VirtualTextNode("Hi"));

        Assert.Equal("<div id=\"main\" class=\"card\">Hi</div>", div.OuterHtml);
    }

    [Fact]
    public void AppendChild_removes_from_previous_parent() {
        var parent1 = new VirtualElement("div");
        var parent2 = new VirtualElement("div");
        var child = new VirtualElement("span");

        parent1.AppendChild(child);
        Assert.Single(parent1.ChildNodes);

        parent2.AppendChild(child);
        Assert.Empty(parent1.ChildNodes);
        Assert.Single(parent2.ChildNodes);
        Assert.Same(parent2, child.ParentNode);
    }

    [Fact]
    public void RemoveChild_clears_parent() {
        var parent = new VirtualElement("div");
        var child = new VirtualElement("span");
        parent.AppendChild(child);

        parent.RemoveChild(child);

        Assert.Null(child.ParentNode);
        Assert.Empty(parent.ChildNodes);
    }

    // --- QuerySelector on nested elements ---

    [Fact]
    public void QuerySelector_finds_nested_element_by_tag() {
        var root = new VirtualElement("div");
        var inner = new VirtualElement("section");
        var deep = new VirtualElement("span");
        root.AppendChild(inner);
        inner.AppendChild(deep);

        var found = root.QuerySelector("span");
        Assert.NotNull(found);
        Assert.Same(deep, found);
    }

    [Fact]
    public void QuerySelector_finds_nested_element_by_class() {
        var root = new VirtualElement("div");
        var inner = new VirtualElement("div");
        inner.ClassName = "hidden";
        var deep = new VirtualElement("p");
        deep.ClassName = "target";
        inner.AppendChild(deep);
        root.AppendChild(inner);

        var found = root.QuerySelector(".target");
        Assert.NotNull(found);
        Assert.Same(deep, found);
    }

    [Fact]
    public void QuerySelector_finds_nested_element_by_id() {
        var root = new VirtualElement("div");
        var child = new VirtualElement("div");
        var grandchild = new VirtualElement("span");
        grandchild.Id = "unique";
        child.AppendChild(grandchild);
        root.AppendChild(child);

        var found = root.QuerySelector("#unique");
        Assert.NotNull(found);
        Assert.Same(grandchild, found);
    }

    [Fact]
    public void QuerySelector_does_not_match_self() {
        var el = new VirtualElement("div");
        el.ClassName = "self";

        var found = el.QuerySelector(".self");
        Assert.Null(found);
    }

    [Fact]
    public void QuerySelector_returns_first_match() {
        var root = new VirtualElement("div");
        var first = new VirtualElement("span");
        first.ClassName = "item";
        var second = new VirtualElement("span");
        second.ClassName = "item";
        root.AppendChild(first);
        root.AppendChild(second);

        var found = root.QuerySelector(".item");
        Assert.Same(first, found);
    }

    [Fact]
    public void QuerySelector_returns_null_when_no_match() {
        var root = new VirtualElement("div");
        root.AppendChild(new VirtualElement("span"));

        var found = root.QuerySelector(".nonexistent");
        Assert.Null(found);
    }

    // --- QuerySelectorAll on nested elements ---

    [Fact]
    public void QuerySelectorAll_finds_all_nested_matches() {
        var root = new VirtualElement("ul");
        for (var i = 0; i < 4; i++) {
            var li = new VirtualElement("li");
            li.ClassName = "entry";
            root.AppendChild(li);
        }

        var results = root.QuerySelectorAll(".entry");
        Assert.Equal(4, results.Count);
    }

    [Fact]
    public void QuerySelectorAll_finds_deeply_nested() {
        var root = new VirtualElement("div");
        var level1 = new VirtualElement("div");
        var level2 = new VirtualElement("div");
        var target1 = new VirtualElement("span");
        target1.ClassName = "highlight";
        var target2 = new VirtualElement("span");
        target2.ClassName = "highlight";

        level2.AppendChild(target2);
        level1.AppendChild(target1);
        level1.AppendChild(level2);
        root.AppendChild(level1);

        var results = root.QuerySelectorAll(".highlight");
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void QuerySelectorAll_excludes_self() {
        var el = new VirtualElement("div");
        el.ClassName = "match";
        var child = new VirtualElement("div");
        child.ClassName = "match";
        el.AppendChild(child);

        var results = el.QuerySelectorAll(".match");
        Assert.Single(results);
        Assert.Same(child, results[0]);
    }

    [Fact]
    public void QuerySelectorAll_returns_empty_when_no_match() {
        var root = new VirtualElement("div");
        root.AppendChild(new VirtualElement("span"));

        var results = root.QuerySelectorAll(".nonexistent");
        Assert.Empty(results);
    }

    // --- Comma-separated selectors ---

    [Fact]
    public void QuerySelector_comma_separated_matches_first_alternative() {
        var root = new VirtualElement("div");
        var span = new VirtualElement("span");
        var p = new VirtualElement("p");
        root.AppendChild(span);
        root.AppendChild(p);

        var found = root.QuerySelector("p, span");
        // Should find span first (document order)
        Assert.Same(span, found);
    }

    [Fact]
    public void QuerySelectorAll_comma_separated_matches_all_alternatives() {
        var root = new VirtualElement("div");
        var span = new VirtualElement("span");
        var p = new VirtualElement("p");
        var section = new VirtualElement("section");
        root.AppendChild(span);
        root.AppendChild(p);
        root.AppendChild(section);

        var results = root.QuerySelectorAll("span, p");
        Assert.Equal(2, results.Count);
        Assert.Contains(span, results);
        Assert.Contains(p, results);
    }

    [Fact]
    public void QuerySelector_comma_separated_with_class_and_tag() {
        var root = new VirtualElement("div");
        var el1 = new VirtualElement("span");
        el1.ClassName = "active";
        var el2 = new VirtualElement("p");
        root.AppendChild(el1);
        root.AppendChild(el2);

        var found = root.QuerySelector("p, .active");
        // .active (span) comes first in document order
        Assert.Same(el1, found);
    }

    // --- Compound selectors ---

    [Fact]
    public void QuerySelector_compound_tag_and_class() {
        var root = new VirtualElement("div");
        var target = new VirtualElement("div");
        target.ClassName = "active";
        var other = new VirtualElement("span");
        other.ClassName = "active";
        root.AppendChild(other);
        root.AppendChild(target);

        var found = root.QuerySelector("div.active");
        Assert.Same(target, found);
    }

    [Fact]
    public void QuerySelector_compound_tag_and_id() {
        var root = new VirtualElement("div");
        var target = new VirtualElement("div");
        target.Id = "main";
        root.AppendChild(target);

        var found = root.QuerySelector("div#main");
        Assert.Same(target, found);
    }

    [Fact]
    public void QuerySelector_compound_tag_class_and_id() {
        var root = new VirtualElement("div");
        var target = new VirtualElement("div");
        target.Id = "main";
        target.ClassName = "active";
        var decoy = new VirtualElement("div");
        decoy.ClassName = "active";
        root.AppendChild(decoy);
        root.AppendChild(target);

        var found = root.QuerySelector("div.active#main");
        Assert.Same(target, found);
    }

    [Fact]
    public void QuerySelector_compound_multiple_classes() {
        var root = new VirtualElement("div");
        var target = new VirtualElement("div");
        target.ClassName = "card active featured";
        var partial = new VirtualElement("div");
        partial.ClassName = "card";
        root.AppendChild(partial);
        root.AppendChild(target);

        var found = root.QuerySelector(".card.active");
        Assert.Same(target, found);
    }

    // --- NextSibling / PreviousSibling ---

    [Fact]
    public void NextSibling_returns_next_child() {
        var parent = new VirtualElement("div");
        var first = new VirtualElement("span");
        var second = new VirtualElement("p");
        var third = new VirtualElement("a");
        parent.AppendChild(first);
        parent.AppendChild(second);
        parent.AppendChild(third);

        Assert.Same(second, first.NextSibling);
        Assert.Same(third, second.NextSibling);
        Assert.Null(third.NextSibling);
    }

    [Fact]
    public void PreviousSibling_returns_previous_child() {
        var parent = new VirtualElement("div");
        var first = new VirtualElement("span");
        var second = new VirtualElement("p");
        var third = new VirtualElement("a");
        parent.AppendChild(first);
        parent.AppendChild(second);
        parent.AppendChild(third);

        Assert.Null(first.PreviousSibling);
        Assert.Same(first, second.PreviousSibling);
        Assert.Same(second, third.PreviousSibling);
    }

    [Fact]
    public void NextSibling_returns_null_when_no_parent() {
        var el = new VirtualElement("div");
        Assert.Null(el.NextSibling);
    }

    [Fact]
    public void PreviousSibling_returns_null_when_no_parent() {
        var el = new VirtualElement("div");
        Assert.Null(el.PreviousSibling);
    }

    [Fact]
    public void Siblings_include_text_nodes() {
        var parent = new VirtualElement("div");
        var span = new VirtualElement("span");
        var text = new VirtualTextNode("hello");
        parent.AppendChild(span);
        parent.AppendChild(text);

        Assert.Same(text, span.NextSibling);
        Assert.Same(span, text.PreviousSibling);
    }

    // --- InsertBefore ---

    [Fact]
    public void InsertBefore_inserts_before_reference() {
        var parent = new VirtualElement("div");
        var existing = new VirtualElement("span");
        parent.AppendChild(existing);

        var newChild = new VirtualElement("p");
        parent.InsertBefore(newChild, existing);

        Assert.Equal(2, parent.ChildNodes.Count);
        Assert.Same(newChild, parent.ChildNodes[0]);
        Assert.Same(existing, parent.ChildNodes[1]);
        Assert.Same(parent, newChild.ParentNode);
    }

    [Fact]
    public void InsertBefore_null_ref_appends_to_end() {
        var parent = new VirtualElement("div");
        var existing = new VirtualElement("span");
        parent.AppendChild(existing);

        var newChild = new VirtualElement("p");
        parent.InsertBefore(newChild, null);

        Assert.Equal(2, parent.ChildNodes.Count);
        Assert.Same(existing, parent.ChildNodes[0]);
        Assert.Same(newChild, parent.ChildNodes[1]);
    }

    [Fact]
    public void InsertBefore_removes_from_old_parent() {
        var parent1 = new VirtualElement("div");
        var parent2 = new VirtualElement("div");
        var child = new VirtualElement("span");
        var ref_ = new VirtualElement("p");

        parent1.AppendChild(child);
        parent2.AppendChild(ref_);

        parent2.InsertBefore(child, ref_);

        Assert.Empty(parent1.ChildNodes);
        Assert.Equal(2, parent2.ChildNodes.Count);
        Assert.Same(child, parent2.ChildNodes[0]);
        Assert.Same(parent2, child.ParentNode);
    }

    [Fact]
    public void InsertBefore_throws_when_ref_not_child() {
        var parent = new VirtualElement("div");
        var newChild = new VirtualElement("span");
        var notChild = new VirtualElement("p");

        Assert.Throws<InvalidOperationException>(() =>
            parent.InsertBefore(newChild, notChild));
    }

    [Fact]
    public void InsertBefore_updates_siblings() {
        var parent = new VirtualElement("div");
        var first = new VirtualElement("a");
        var third = new VirtualElement("c");
        parent.AppendChild(first);
        parent.AppendChild(third);

        var second = new VirtualElement("b");
        parent.InsertBefore(second, third);

        Assert.Same(second, first.NextSibling);
        Assert.Same(first, second.PreviousSibling);
        Assert.Same(third, second.NextSibling);
        Assert.Same(second, third.PreviousSibling);
    }

    // --- TextContent get (recursive from children) ---

    [Fact]
    public void TextContent_get_concatenates_all_descendant_text() {
        var div = new VirtualElement("div");
        var span1 = new VirtualElement("span");
        span1.AppendChild(new VirtualTextNode("Hello "));
        var span2 = new VirtualElement("span");
        span2.AppendChild(new VirtualTextNode("World"));
        div.AppendChild(span1);
        div.AppendChild(span2);

        Assert.Equal("Hello World", div.TextContent);
    }

    [Fact]
    public void TextContent_get_deeply_nested() {
        var div = new VirtualElement("div");
        var p = new VirtualElement("p");
        var span = new VirtualElement("span");
        span.AppendChild(new VirtualTextNode("deep"));
        p.AppendChild(span);
        div.AppendChild(p);

        Assert.Equal("deep", div.TextContent);
    }

    [Fact]
    public void TextContent_get_empty_when_no_children() {
        var div = new VirtualElement("div");
        Assert.Equal("", div.TextContent);
    }

    [Fact]
    public void TextContent_get_mixed_elements_and_text() {
        var div = new VirtualElement("div");
        div.AppendChild(new VirtualTextNode("before "));
        var span = new VirtualElement("span");
        span.AppendChild(new VirtualTextNode("inside"));
        div.AppendChild(span);
        div.AppendChild(new VirtualTextNode(" after"));

        Assert.Equal("before inside after", div.TextContent);
    }

    // --- TextContent set (replaces children) ---

    [Fact]
    public void TextContent_set_replaces_all_children_with_text_node() {
        var div = new VirtualElement("div");
        div.AppendChild(new VirtualElement("span"));
        div.AppendChild(new VirtualElement("p"));
        div.AppendChild(new VirtualTextNode("old text"));

        div.TextContent = "new content";

        Assert.Single(div.ChildNodes);
        Assert.IsType<VirtualTextNode>(div.ChildNodes[0]);
        Assert.Equal("new content", div.TextContent);
    }

    [Fact]
    public void TextContent_set_empty_string_clears_children() {
        var div = new VirtualElement("div");
        div.AppendChild(new VirtualElement("span"));

        div.TextContent = "";

        Assert.Empty(div.ChildNodes);
    }

    [Fact]
    public void TextContent_set_null_clears_children() {
        var div = new VirtualElement("div");
        div.AppendChild(new VirtualElement("span"));

        div.TextContent = null!;

        Assert.Empty(div.ChildNodes);
    }

    // --- OuterHtml with style attributes ---

    [Fact]
    public void OuterHtml_includes_style_attribute() {
        var div = new VirtualElement("div");
        div.Style["color"] = "red";

        Assert.Contains("style=\"color: red\"", div.OuterHtml);
    }

    [Fact]
    public void OuterHtml_includes_multiple_style_properties() {
        var div = new VirtualElement("div");
        div.Style["color"] = "red";
        div.Style["font-size"] = "14px";

        var html = div.OuterHtml;
        Assert.Contains("style=\"", html);
        Assert.Contains("color: red", html);
        Assert.Contains("font-size: 14px", html);
    }

    [Fact]
    public void OuterHtml_no_style_when_empty() {
        var div = new VirtualElement("div");
        Assert.DoesNotContain("style", div.OuterHtml);
    }

    [Fact]
    public void OuterHtml_with_id_class_and_style() {
        var div = new VirtualElement("div");
        div.Id = "main";
        div.ClassName = "card";
        div.Style["display"] = "flex";

        var html = div.OuterHtml;
        Assert.Contains("id=\"main\"", html);
        Assert.Contains("class=\"card\"", html);
        Assert.Contains("style=\"display: flex\"", html);
    }

    [Fact]
    public void OuterHtml_includes_custom_attributes() {
        var div = new VirtualElement("div");
        div.SetAttribute("data-value", "42");

        Assert.Contains("data-value=\"42\"", div.OuterHtml);
    }

    // --- GetJsProperty / SetJsProperty / InvokeJsMethod ---

    [Fact]
    public void GetJsProperty_tagName_returns_uppercase() {
        var el = new VirtualElement("div");
        Assert.Equal("DIV", el.GetJsProperty("tagName"));
    }

    [Fact]
    public void GetJsProperty_innerHTML() {
        var el = new VirtualElement("div");
        el.AppendChild(new VirtualTextNode("text"));
        Assert.Equal("text", el.GetJsProperty("innerHTML"));
    }

    [Fact]
    public void GetJsProperty_outerHTML() {
        var el = new VirtualElement("div");
        el.AppendChild(new VirtualTextNode("text"));
        Assert.Equal("<div>text</div>", el.GetJsProperty("outerHTML"));
    }

    [Fact]
    public void GetJsProperty_children_returns_element_children() {
        var el = new VirtualElement("div");
        el.AppendChild(new VirtualElement("span"));
        el.AppendChild(new VirtualTextNode("text"));
        el.AppendChild(new VirtualElement("p"));

        var children = el.GetJsProperty("children") as IReadOnlyList<VirtualElement>;
        Assert.NotNull(children);
        Assert.Equal(2, children!.Count);
    }

    [Fact]
    public void GetJsProperty_attributes_returns_dictionary() {
        var el = new VirtualElement("div");
        el.SetAttribute("data-x", "1");
        var attrs = el.GetJsProperty("attributes") as Dictionary<string, string>;
        Assert.NotNull(attrs);
        Assert.Equal("1", attrs!["data-x"]);
    }

    [Fact]
    public void SetJsProperty_id_sets_id() {
        var el = new VirtualElement("div");
        el.SetJsProperty("id", "test");
        Assert.Equal("test", el.Id);
    }

    [Fact]
    public void SetJsProperty_className_sets_classname() {
        var el = new VirtualElement("div");
        el.SetJsProperty("className", "card active");
        Assert.Equal("card active", el.ClassName);
    }

    [Fact]
    public void SetJsProperty_innerHTML_replaces_children() {
        var el = new VirtualElement("div");
        el.AppendChild(new VirtualElement("span"));
        el.SetJsProperty("innerHTML", "new content");

        Assert.Single(el.ChildNodes);
        Assert.IsType<VirtualTextNode>(el.ChildNodes[0]);
        Assert.Equal("new content", el.TextContent);
    }

    [Fact]
    public void SetJsProperty_innerHTML_empty_clears_children() {
        var el = new VirtualElement("div");
        el.AppendChild(new VirtualElement("span"));
        el.SetJsProperty("innerHTML", "");

        Assert.Empty(el.ChildNodes);
    }

    [Fact]
    public void InvokeJsMethod_getAttribute() {
        var el = new VirtualElement("div");
        el.SetAttribute("role", "button");
        var result = el.InvokeJsMethod("getAttribute", ["role"]);
        Assert.Equal("button", result);
    }

    [Fact]
    public void InvokeJsMethod_setAttribute() {
        var el = new VirtualElement("div");
        el.InvokeJsMethod("setAttribute", ["data-x", "123"]);
        Assert.Equal("123", el.GetAttribute("data-x"));
    }

    [Fact]
    public void InvokeJsMethod_removeAttribute() {
        var el = new VirtualElement("div");
        el.SetAttribute("data-x", "123");
        el.InvokeJsMethod("removeAttribute", ["data-x"]);
        Assert.Null(el.GetAttribute("data-x"));
    }

    [Fact]
    public void InvokeJsMethod_hasAttribute() {
        var el = new VirtualElement("div");
        el.SetAttribute("data-x", "123");
        Assert.Equal(true, el.InvokeJsMethod("hasAttribute", ["data-x"]));
        Assert.Equal(false, el.InvokeJsMethod("hasAttribute", ["data-y"]));
    }

    [Fact]
    public void InvokeJsMethod_querySelector() {
        var el = new VirtualElement("div");
        var child = new VirtualElement("span");
        child.ClassName = "target";
        el.AppendChild(child);

        var result = el.InvokeJsMethod("querySelector", [".target"]);
        Assert.Same(child, result);
    }

    [Fact]
    public void InvokeJsMethod_querySelectorAll() {
        var el = new VirtualElement("div");
        el.AppendChild(new VirtualElement("span"));
        el.AppendChild(new VirtualElement("span"));

        var result = el.InvokeJsMethod("querySelectorAll", ["span"]) as List<VirtualElement>;
        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
    }

    [Fact]
    public void InvokeJsMethod_appendChild() {
        var parent = new VirtualElement("div");
        var child = new VirtualElement("span");
        parent.InvokeJsMethod("appendChild", [child]);

        Assert.Single(parent.ChildNodes);
        Assert.Same(child, parent.ChildNodes[0]);
    }

    [Fact]
    public void InvokeJsMethod_removeChild() {
        var parent = new VirtualElement("div");
        var child = new VirtualElement("span");
        parent.AppendChild(child);

        parent.InvokeJsMethod("removeChild", [child]);
        Assert.Empty(parent.ChildNodes);
    }

    [Fact]
    public void InvokeJsMethod_insertBefore() {
        var parent = new VirtualElement("div");
        var existing = new VirtualElement("span");
        parent.AppendChild(existing);

        var newChild = new VirtualElement("p");
        parent.InvokeJsMethod("insertBefore", [newChild, existing]);

        Assert.Equal(2, parent.ChildNodes.Count);
        Assert.Same(newChild, parent.ChildNodes[0]);
    }

    [Fact]
    public void InvokeJsMethod_unknown_returns_null() {
        var el = new VirtualElement("div");
        var result = el.InvokeJsMethod("unknownMethod", []);
        Assert.Null(result);
    }

    // --- FirstChild / LastChild ---

    [Fact]
    public void FirstChild_returns_first_child() {
        var parent = new VirtualElement("div");
        var first = new VirtualElement("span");
        var second = new VirtualElement("p");
        parent.AppendChild(first);
        parent.AppendChild(second);

        Assert.Same(first, parent.FirstChild);
    }

    [Fact]
    public void LastChild_returns_last_child() {
        var parent = new VirtualElement("div");
        var first = new VirtualElement("span");
        var second = new VirtualElement("p");
        parent.AppendChild(first);
        parent.AppendChild(second);

        Assert.Same(second, parent.LastChild);
    }

    [Fact]
    public void FirstChild_null_when_no_children() {
        var el = new VirtualElement("div");
        Assert.Null(el.FirstChild);
    }

    [Fact]
    public void LastChild_null_when_no_children() {
        var el = new VirtualElement("div");
        Assert.Null(el.LastChild);
    }

    // --- NodeType and NodeName ---

    [Fact]
    public void NodeType_is_1_for_element() {
        var el = new VirtualElement("div");
        Assert.Equal(1, el.NodeType);
    }

    [Fact]
    public void NodeName_is_uppercase_tag() {
        var el = new VirtualElement("div");
        Assert.Equal("DIV", el.NodeName);
    }

    [Fact]
    public void TagName_is_lowercase() {
        var el = new VirtualElement("DIV");
        Assert.Equal("div", el.TagName);
    }
}
