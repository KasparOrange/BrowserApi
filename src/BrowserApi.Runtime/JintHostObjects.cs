using BrowserApi.Runtime.VirtualDom;
using Jint;

namespace BrowserApi.Runtime;

internal static class JintHostObjects {
    public static void Register(Engine engine, VirtualDocument document, VirtualConsole console) {
        engine.SetValue("document", new JsDocumentProxy(document, engine));
        engine.SetValue("console", new JsConsoleProxy(console));
        // window.document and window.console reference the same objects
        engine.Execute("var window = { document: document, console: console };");
    }

    // Jint wraps these C# objects automatically. camelCase names match JS conventions.
    internal class JsDocumentProxy {
        private readonly VirtualDocument _doc;
        private readonly Engine _engine;

        public JsDocumentProxy(VirtualDocument doc, Engine engine) { _doc = doc; _engine = engine; }

        public JsElementProxy? documentElement => new(_doc.DocumentElement, _engine);
        public JsElementProxy? body => new(_doc.Body, _engine);
        public JsElementProxy? head => new(_doc.Head, _engine);
        public int nodeType => _doc.NodeType;

        public JsElementProxy createElement(string tagName) =>
            new(_doc.CreateElement(tagName), _engine);

        public JsTextNodeProxy createTextNode(string data) =>
            new(_doc.CreateTextNode(data));

        public JsElementProxy? getElementById(string id) {
            var el = _doc.GetElementById(id);
            return el is not null ? new(el, _engine) : null;
        }

        public JsElementProxy? querySelector(string selector) {
            var el = _doc.QuerySelector(selector);
            return el is not null ? new(el, _engine) : null;
        }

        public JsElementProxy[] querySelectorAll(string selector) =>
            _doc.QuerySelectorAll(selector).Select(e => new JsElementProxy(e, _engine)).ToArray();
    }

    internal class JsElementProxy {
        internal readonly VirtualElement _element;
        private readonly Engine _engine;

        public JsElementProxy(VirtualElement element, Engine engine) { _element = element; _engine = engine; }

        public string tagName => _element.NodeName;
        public string nodeName => _element.NodeName;
        public int nodeType => _element.NodeType;

        public string id { get => _element.Id; set => _element.Id = value; }
        public string className { get => _element.ClassName; set => _element.ClassName = value; }

        public string textContent {
            get => _element.TextContent;
            set => _element.TextContent = value;
        }

        public string innerHTML => _element.InnerHtml;
        public string outerHTML => _element.OuterHtml;

        public JsStyleProxy style => new(_element.Style);

        public JsElementProxy? parentNode {
            get {
                if (_element.ParentNode is VirtualElement pe) return new(pe, _engine);
                return null;
            }
        }

        public JsElementProxy? firstChild {
            get {
                if (_element.FirstChild is VirtualElement fe) return new(fe, _engine);
                return null;
            }
        }

        public JsElementProxy appendChild(JsElementProxy child) {
            _element.AppendChild(child._element);
            return child;
        }

        public JsElementProxy removeChild(JsElementProxy child) {
            _element.RemoveChild(child._element);
            return child;
        }

        public void setAttribute(string name, string value) =>
            _element.SetAttribute(name, value);

        public string? getAttribute(string name) =>
            _element.GetAttribute(name);

        public void removeAttribute(string name) =>
            _element.RemoveAttribute(name);

        public bool hasAttribute(string name) =>
            _element.HasAttribute(name);

        public JsElementProxy? querySelector(string selector) {
            var el = _element.QuerySelector(selector);
            return el is not null ? new(el, _engine) : null;
        }

        public JsElementProxy[] querySelectorAll(string selector) =>
            _element.QuerySelectorAll(selector).Select(e => new JsElementProxy(e, _engine)).ToArray();
    }

    internal class JsTextNodeProxy {
        private readonly VirtualTextNode _text;

        public JsTextNodeProxy(VirtualTextNode text) { _text = text; }

        public int nodeType => 3;
        public string nodeName => "#text";
        public string data { get => _text.Data; set => _text.Data = value; }
        public string textContent { get => _text.TextContent; set => _text.TextContent = value; }
    }

    internal class JsStyleProxy {
        private readonly VirtualStyle _style;

        public JsStyleProxy(VirtualStyle style) { _style = style; }

        public string cssText { get => _style.CssText; set => _style.CssText = value; }

        // Common CSS properties — Jint accesses these as JS properties
        public string display { get => _style["display"]; set => _style["display"] = value; }
        public string position { get => _style["position"]; set => _style["position"] = value; }
        public string color { get => _style["color"]; set => _style["color"] = value; }
        public string backgroundColor { get => _style["background-color"]; set => _style["background-color"] = value; }
        public string margin { get => _style["margin"]; set => _style["margin"] = value; }
        public string padding { get => _style["padding"]; set => _style["padding"] = value; }
        public string width { get => _style["width"]; set => _style["width"] = value; }
        public string height { get => _style["height"]; set => _style["height"] = value; }
        public string border { get => _style["border"]; set => _style["border"] = value; }
        public string fontSize { get => _style["font-size"]; set => _style["font-size"] = value; }
        public string fontWeight { get => _style["font-weight"]; set => _style["font-weight"] = value; }
        public string fontFamily { get => _style["font-family"]; set => _style["font-family"] = value; }
        public string textAlign { get => _style["text-align"]; set => _style["text-align"] = value; }
        public string opacity { get => _style["opacity"]; set => _style["opacity"] = value; }
        public string overflow { get => _style["overflow"]; set => _style["overflow"] = value; }
        public string gap { get => _style["gap"]; set => _style["gap"] = value; }
        public string flexDirection { get => _style["flex-direction"]; set => _style["flex-direction"] = value; }
        public string justifyContent { get => _style["justify-content"]; set => _style["justify-content"] = value; }
        public string alignItems { get => _style["align-items"]; set => _style["align-items"] = value; }
        public string transform { get => _style["transform"]; set => _style["transform"] = value; }
        public string visibility { get => _style["visibility"]; set => _style["visibility"] = value; }
        public string zIndex { get => _style["z-index"]; set => _style["z-index"] = value; }
    }

    internal class JsConsoleProxy {
        private readonly VirtualConsole _console;

        public JsConsoleProxy(VirtualConsole console) { _console = console; }

        public void log(params object?[] data) => _console.Log(data);
        public void error(params object?[] data) => _console.Error(data);
        public void warn(params object?[] data) => _console.Warn(data);
        public void info(params object?[] data) => _console.Info(data);
        public void clear() => _console.Clear();
    }
}
