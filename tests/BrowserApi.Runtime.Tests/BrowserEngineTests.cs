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
}
