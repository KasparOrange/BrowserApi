using BrowserApi.Runtime.VirtualDom;

namespace BrowserApi.Runtime.Tests;

public class BrowserEngineTests : IDisposable {
    private readonly BrowserEngine _engine;

    public BrowserEngineTests() {
        _engine = new BrowserEngine();
    }

    public void Dispose() => _engine.Dispose();

    [Fact]
    public void Execute_creates_element_visible_from_csharp() {
        _engine.Execute("document.body.appendChild(document.createElement('div'));");

        Assert.Single(_engine.VirtualDocument.Body.ChildNodes);
        Assert.Equal("div", ((VirtualElement)_engine.VirtualDocument.Body.ChildNodes[0]).TagName);
    }

    [Fact]
    public void Execute_sets_className() {
        _engine.Execute(@"
            var card = document.createElement('div');
            card.className = 'card';
            document.body.appendChild(card);
        ");

        var card = _engine.VirtualDocument.QuerySelector(".card");
        Assert.NotNull(card);
        Assert.Equal("card", card!.ClassName);
    }

    [Fact]
    public void Execute_sets_style_properties() {
        _engine.Execute(@"
            var el = document.createElement('div');
            el.style.display = 'flex';
            el.style.gap = '1rem';
            document.body.appendChild(el);
        ");

        var el = _engine.VirtualDocument.Body.ChildNodes[0] as VirtualElement;
        Assert.NotNull(el);
        Assert.Equal("flex", el!.Style["display"]);
        Assert.Equal("1rem", el.Style["gap"]);
    }

    [Fact]
    public void Execute_full_scenario_from_runtime_plan() {
        _engine.Execute(@"
            var card = document.createElement('div');
            card.className = 'card';
            card.style.display = 'flex';
            card.style.gap = '1rem';
            document.body.appendChild(card);
        ");

        var card = _engine.VirtualDocument.QuerySelector(".card");
        Assert.NotNull(card);
        Assert.Equal("flex", card!.Style["display"]);
        Assert.Equal("1rem", card.Style["gap"]);
    }

    [Fact]
    public void Execute_sets_id_and_finds_by_id() {
        _engine.Execute(@"
            var el = document.createElement('section');
            el.id = 'hero';
            document.body.appendChild(el);
        ");

        var found = _engine.VirtualDocument.GetElementById("hero");
        Assert.NotNull(found);
        Assert.Equal("section", found!.TagName);
    }

    [Fact]
    public void Execute_sets_textContent() {
        _engine.Execute(@"
            var p = document.createElement('p');
            p.textContent = 'Hello World';
            document.body.appendChild(p);
        ");

        var p = _engine.VirtualDocument.QuerySelector("p");
        Assert.NotNull(p);
        Assert.Equal("Hello World", p!.TextContent);
    }

    [Fact]
    public void Execute_setAttribute_and_getAttribute() {
        _engine.Execute(@"
            var input = document.createElement('input');
            input.setAttribute('type', 'email');
            input.setAttribute('placeholder', 'Enter email');
            document.body.appendChild(input);
        ");

        var input = _engine.VirtualDocument.QuerySelector("input");
        Assert.NotNull(input);
        Assert.Equal("email", input!.GetAttribute("type"));
        Assert.Equal("Enter email", input.GetAttribute("placeholder"));
    }

    [Fact]
    public void Execute_console_log_captures_messages() {
        _engine.Execute("console.log('hello', 'world');");
        _engine.Execute("console.error('oops');");

        Assert.Equal(2, _engine.VirtualConsole.Messages.Count);
        Assert.Equal("log", _engine.VirtualConsole.Messages[0].Level);
        Assert.Contains("hello", _engine.VirtualConsole.Messages[0].Text);
        Assert.Equal("error", _engine.VirtualConsole.Messages[1].Level);
    }

    [Fact]
    public void Execute_nested_elements() {
        _engine.Execute(@"
            var ul = document.createElement('ul');
            for (var i = 0; i < 3; i++) {
                var li = document.createElement('li');
                li.textContent = 'Item ' + (i + 1);
                ul.appendChild(li);
            }
            document.body.appendChild(ul);
        ");

        var items = _engine.VirtualDocument.QuerySelectorAll("li");
        Assert.Equal(3, items.Count);
        Assert.Equal("Item 1", items[0].TextContent);
        Assert.Equal("Item 2", items[1].TextContent);
        Assert.Equal("Item 3", items[2].TextContent);
    }

    [Fact]
    public void Execute_querySelector_from_js() {
        _engine.Execute(@"
            var div = document.createElement('div');
            div.id = 'container';
            var span = document.createElement('span');
            span.className = 'label';
            div.appendChild(span);
            document.body.appendChild(div);

            var found = document.querySelector('.label');
            found.textContent = 'Found!';
        ");

        var label = _engine.VirtualDocument.QuerySelector(".label");
        Assert.NotNull(label);
        Assert.Equal("Found!", label!.TextContent);
    }

    [Fact]
    public void Evaluate_returns_value() {
        var result = _engine.Evaluate<double>("2 + 3");
        Assert.Equal(5.0, result);
    }

    [Fact]
    public void Evaluate_string() {
        var result = _engine.Evaluate<string>("'hello' + ' world'");
        Assert.Equal("hello world", result);
    }

    // --- Evaluate<T> with different types ---

    [Fact]
    public void Evaluate_bool_true() {
        var result = _engine.Evaluate<bool>("1 === 1");
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_bool_false() {
        var result = _engine.Evaluate<bool>("1 === 2");
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_int() {
        var result = _engine.Evaluate<int>("10 + 5");
        Assert.Equal(15, result);
    }

    [Fact]
    public void Evaluate_double_math() {
        var result = _engine.Evaluate<double>("Math.PI");
        Assert.True(result > 3.14 && result < 3.15);
    }

    [Fact]
    public void Evaluate_string_concatenation() {
        var result = _engine.Evaluate<string>("'abc' + 'def'");
        Assert.Equal("abcdef", result);
    }

    [Fact]
    public void Evaluate_nongeneric_returns_object() {
        var result = _engine.Evaluate("42");
        Assert.Equal(42.0, result);
    }

    [Fact]
    public void Evaluate_null_returns_default() {
        var result = _engine.Evaluate<string>("null");
        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_undefined_returns_default() {
        var result = _engine.Evaluate<string>("undefined");
        Assert.Null(result);
    }

    // --- Console error/warn/info captured ---

    [Fact]
    public void Execute_console_error_captures() {
        _engine.Execute("console.error('fail');");

        Assert.Single(_engine.VirtualConsole.Messages);
        Assert.Equal("error", _engine.VirtualConsole.Messages[0].Level);
        Assert.Contains("fail", _engine.VirtualConsole.Messages[0].Text);
    }

    [Fact]
    public void Execute_console_warn_captures() {
        _engine.Execute("console.warn('careful');");

        Assert.Single(_engine.VirtualConsole.Messages);
        Assert.Equal("warn", _engine.VirtualConsole.Messages[0].Level);
        Assert.Contains("careful", _engine.VirtualConsole.Messages[0].Text);
    }

    [Fact]
    public void Execute_console_info_captures() {
        _engine.Execute("console.info('note');");

        Assert.Single(_engine.VirtualConsole.Messages);
        Assert.Equal("info", _engine.VirtualConsole.Messages[0].Level);
        Assert.Contains("note", _engine.VirtualConsole.Messages[0].Text);
    }

    [Fact]
    public void Execute_console_clear_removes_messages() {
        _engine.Execute("console.log('first');");
        _engine.Execute("console.log('second');");
        Assert.Equal(2, _engine.VirtualConsole.Messages.Count);

        _engine.Execute("console.clear();");
        Assert.Empty(_engine.VirtualConsole.Messages);
    }

    [Fact]
    public void Execute_console_log_multiple_args() {
        _engine.Execute("console.log('a', 'b', 'c');");

        Assert.Single(_engine.VirtualConsole.Messages);
        Assert.Contains("a", _engine.VirtualConsole.Messages[0].Text);
        Assert.Contains("b", _engine.VirtualConsole.Messages[0].Text);
        Assert.Contains("c", _engine.VirtualConsole.Messages[0].Text);
    }

    // --- JS querySelector returns correct element ---

    [Fact]
    public void Execute_querySelector_by_id_from_js() {
        _engine.Execute(@"
            var el = document.createElement('div');
            el.id = 'target';
            el.textContent = 'found me';
            document.body.appendChild(el);

            var result = document.querySelector('#target');
            result.textContent = 'updated by querySelector';
        ");

        var found = _engine.VirtualDocument.GetElementById("target");
        Assert.NotNull(found);
        Assert.Equal("updated by querySelector", found!.TextContent);
    }

    [Fact]
    public void Execute_querySelector_by_tag_from_js() {
        _engine.Execute(@"
            var header = document.createElement('h1');
            header.textContent = 'Title';
            document.body.appendChild(header);

            var found = document.querySelector('h1');
            found.className = 'main-title';
        ");

        var h1 = _engine.VirtualDocument.QuerySelector("h1");
        Assert.NotNull(h1);
        Assert.Equal("main-title", h1!.ClassName);
    }

    [Fact]
    public void Execute_querySelectorAll_from_js() {
        _engine.Execute(@"
            for (var i = 0; i < 3; i++) {
                var item = document.createElement('div');
                item.className = 'item';
                item.textContent = 'Item ' + i;
                document.body.appendChild(item);
            }

            var items = document.querySelectorAll('.item');
            for (var j = 0; j < items.length; j++) {
                items[j].className = 'processed';
            }
        ");

        var processed = _engine.VirtualDocument.QuerySelectorAll(".processed");
        Assert.Equal(3, processed.Count);
    }

    // --- Multiple script executions build on same DOM ---

    [Fact]
    public void Execute_multiple_scripts_share_dom() {
        _engine.Execute(@"
            var container = document.createElement('div');
            container.id = 'container';
            document.body.appendChild(container);
        ");

        _engine.Execute(@"
            var container = document.getElementById('container');
            var child = document.createElement('span');
            child.textContent = 'added later';
            container.appendChild(child);
        ");

        var container = _engine.VirtualDocument.GetElementById("container");
        Assert.NotNull(container);
        Assert.Single(container!.ChildNodes);
        Assert.Equal("added later", container.ChildNodes[0].TextContent);
    }

    [Fact]
    public void Execute_multiple_scripts_share_variables() {
        _engine.Execute("var counter = 0;");
        _engine.Execute("counter = counter + 5;");
        _engine.Execute("counter = counter + 3;");

        var result = _engine.Evaluate<double>("counter");
        Assert.Equal(8.0, result);
    }

    [Fact]
    public void Execute_js_and_csharp_see_same_dom() {
        // C# adds an element
        var div = _engine.VirtualDocument.CreateElement("div");
        div.Id = "from-csharp";
        div.TextContent = "C# element";
        _engine.VirtualDocument.Body.AppendChild(div);

        // JS finds and modifies it
        _engine.Execute(@"
            var el = document.getElementById('from-csharp');
            el.textContent = 'Modified by JS';
        ");

        Assert.Equal("Modified by JS", div.TextContent);
    }

    // --- RemoveChild from JS ---

    [Fact]
    public void Execute_removeChild_from_js() {
        _engine.Execute(@"
            var parent = document.createElement('div');
            parent.id = 'parent';
            var child = document.createElement('span');
            child.id = 'child';
            parent.appendChild(child);
            document.body.appendChild(parent);
        ");

        // Verify child exists
        var child = _engine.VirtualDocument.GetElementById("child");
        Assert.NotNull(child);

        _engine.Execute(@"
            var parent = document.getElementById('parent');
            var child = document.getElementById('child');
            parent.removeChild(child);
        ");

        // Verify child is removed
        var parent = _engine.VirtualDocument.GetElementById("parent");
        Assert.NotNull(parent);
        Assert.Empty(parent!.ChildNodes);
    }

    [Fact]
    public void Execute_removeChild_from_body() {
        _engine.Execute(@"
            var el = document.createElement('div');
            el.id = 'to-remove';
            document.body.appendChild(el);
        ");

        Assert.NotNull(_engine.VirtualDocument.GetElementById("to-remove"));

        _engine.Execute(@"
            var el = document.getElementById('to-remove');
            document.body.removeChild(el);
        ");

        Assert.Null(_engine.VirtualDocument.GetElementById("to-remove"));
    }

    // --- Additional integration scenarios ---

    [Fact]
    public void Execute_set_and_read_style_from_js() {
        _engine.Execute(@"
            var el = document.createElement('div');
            el.style.backgroundColor = 'blue';
            el.style.fontSize = '20px';
            document.body.appendChild(el);
        ");

        var el = _engine.VirtualDocument.Body.ChildNodes[0] as VirtualElement;
        Assert.NotNull(el);
        Assert.Equal("blue", el!.Style["background-color"]);
        Assert.Equal("20px", el.Style["font-size"]);
    }

    [Fact]
    public void Execute_getAttribute_from_js() {
        _engine.Execute(@"
            var el = document.createElement('input');
            el.setAttribute('type', 'password');
            document.body.appendChild(el);

            var type = el.getAttribute('type');
            console.log('type is', type);
        ");

        Assert.Contains("type is", _engine.VirtualConsole.Messages[0].Text);
        Assert.Contains("password", _engine.VirtualConsole.Messages[0].Text);
    }

    [Fact]
    public void Execute_hasAttribute_from_js() {
        _engine.Execute(@"
            var el = document.createElement('div');
            el.setAttribute('hidden', '');
            var hasIt = el.hasAttribute('hidden');
            console.log('hasAttribute:', hasIt);
        ");

        Assert.Contains("True", _engine.VirtualConsole.Messages[0].Text);
    }

    [Fact]
    public void Execute_removeAttribute_from_js() {
        _engine.Execute(@"
            var el = document.createElement('div');
            el.setAttribute('data-x', '1');
            el.removeAttribute('data-x');
            var hasIt = el.hasAttribute('data-x');
            console.log('after remove:', hasIt);
        ");

        Assert.Contains("False", _engine.VirtualConsole.Messages[0].Text);
    }

    [Fact]
    public void Document_property_is_wired_to_virtual_document() {
        Assert.NotNull(_engine.Document);
    }

    [Fact]
    public void Backend_property_is_accessible() {
        Assert.NotNull(_engine.Backend);
    }
}
