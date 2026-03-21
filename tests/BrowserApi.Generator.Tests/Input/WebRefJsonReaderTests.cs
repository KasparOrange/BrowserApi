using System.Text.Json;
using BrowserApi.Generator.Ast;
using BrowserApi.Generator.Input;

namespace BrowserApi.Generator.Tests.Input;

public class WebRefJsonReaderTests {
    private readonly WebRefJsonReader _reader = new();

    private IdlSpecFile Parse(string json) {
        using var doc = JsonDocument.Parse(json);
        return _reader.ParseSpecFile(doc.RootElement);
    }

    [Fact]
    public void Parses_spec_metadata() {
        var spec = Parse("""
        {
            "spec": { "title": "Console Standard", "url": "https://console.spec.whatwg.org/" },
            "idlparsed": { "idlNames": {}, "idlExtendedNames": {} }
        }
        """);
        Assert.Equal("Console Standard", spec.SpecTitle);
        Assert.Equal("https://console.spec.whatwg.org/", spec.SpecUrl);
    }

    [Fact]
    public void Parses_namespace() {
        var spec = Parse("""
        {
            "spec": { "title": "Console Standard", "url": "https://console.spec.whatwg.org/" },
            "idlparsed": {
                "idlNames": {
                    "console": {
                        "type": "namespace",
                        "name": "console",
                        "inheritance": null,
                        "members": [
                            {
                                "type": "operation",
                                "name": "log",
                                "idlType": { "type": "return-type", "extAttrs": [], "generic": "", "nullable": false, "union": false, "idlType": "undefined" },
                                "arguments": [
                                    { "type": "argument", "name": "data", "extAttrs": [], "idlType": { "type": "argument-type", "extAttrs": [], "generic": "", "nullable": false, "union": false, "idlType": "any" }, "default": null, "optional": false, "variadic": true }
                                ],
                                "extAttrs": [],
                                "special": ""
                            }
                        ],
                        "extAttrs": [{ "type": "extended-attribute", "name": "Exposed", "rhs": { "type": "identifier", "value": "*" }, "arguments": [] }],
                        "partial": false
                    }
                },
                "idlExtendedNames": {}
            }
        }
        """);

        Assert.Single(spec.Definitions);
        var ns = Assert.IsType<IdlInterface>(spec.Definitions["console"]);
        Assert.Equal("namespace", ns.Kind);
        Assert.Equal("console", ns.Name);
        Assert.False(ns.IsPartial);

        var op = Assert.IsType<IdlOperation>(Assert.Single(ns.Members));
        Assert.Equal("log", op.Name);
        Assert.Equal("undefined", op.ReturnType.TypeName);

        var arg = Assert.Single(op.Arguments);
        Assert.Equal("data", arg.Name);
        Assert.True(arg.IsVariadic);
        Assert.Equal("any", arg.Type.TypeName);

        var exposed = Assert.Single(ns.ExtAttrs);
        Assert.Equal("Exposed", exposed.Name);
        Assert.Equal("*", exposed.Rhs?.Value);
    }

    [Fact]
    public void Parses_interface_with_inheritance() {
        var spec = Parse("""
        {
            "spec": { "title": "Fetch", "url": "https://fetch.spec.whatwg.org/" },
            "idlparsed": {
                "idlNames": {
                    "Request": {
                        "type": "interface",
                        "name": "Request",
                        "inheritance": "EventTarget",
                        "members": [
                            {
                                "type": "attribute",
                                "name": "method",
                                "idlType": { "type": "attribute-type", "extAttrs": [], "generic": "", "nullable": false, "union": false, "idlType": "ByteString" },
                                "extAttrs": [],
                                "special": "",
                                "readonly": true
                            },
                            {
                                "type": "constructor",
                                "arguments": [
                                    { "type": "argument", "name": "input", "extAttrs": [], "idlType": { "type": "argument-type", "extAttrs": [], "generic": "", "nullable": false, "union": false, "idlType": "RequestInfo" }, "default": null, "optional": false, "variadic": false }
                                ],
                                "extAttrs": []
                            }
                        ],
                        "extAttrs": [],
                        "partial": false
                    }
                },
                "idlExtendedNames": {}
            }
        }
        """);

        var iface = Assert.IsType<IdlInterface>(spec.Definitions["Request"]);
        Assert.Equal("interface", iface.Kind);
        Assert.Equal("EventTarget", iface.Inheritance);

        var attr = Assert.IsType<IdlAttribute>(iface.Members[0]);
        Assert.Equal("method", attr.Name);
        Assert.True(attr.IsReadOnly);
        Assert.Equal("ByteString", attr.Type.TypeName);

        var ctor = Assert.IsType<IdlConstructor>(iface.Members[1]);
        Assert.Single(ctor.Arguments);
    }

    [Fact]
    public void Parses_enum() {
        var spec = Parse("""
        {
            "spec": { "title": "Fetch", "url": "https://fetch.spec.whatwg.org/" },
            "idlparsed": {
                "idlNames": {
                    "RequestDestination": {
                        "type": "enum",
                        "name": "RequestDestination",
                        "values": [
                            { "type": "enum-value", "value": "" },
                            { "type": "enum-value", "value": "audio" },
                            { "type": "enum-value", "value": "document" }
                        ],
                        "extAttrs": []
                    }
                },
                "idlExtendedNames": {}
            }
        }
        """);

        var e = Assert.IsType<IdlEnum>(spec.Definitions["RequestDestination"]);
        Assert.Equal(3, e.Values.Count);
        Assert.Equal("", e.Values[0].Value);
        Assert.Equal("audio", e.Values[1].Value);
        Assert.Equal("document", e.Values[2].Value);
    }

    [Fact]
    public void Parses_dictionary() {
        var spec = Parse("""
        {
            "spec": { "title": "Fetch", "url": "https://fetch.spec.whatwg.org/" },
            "idlparsed": {
                "idlNames": {
                    "RequestInit": {
                        "type": "dictionary",
                        "name": "RequestInit",
                        "inheritance": null,
                        "members": [
                            {
                                "type": "field",
                                "name": "method",
                                "extAttrs": [],
                                "idlType": { "type": "dictionary-type", "extAttrs": [], "generic": "", "nullable": false, "union": false, "idlType": "ByteString" },
                                "default": null,
                                "required": false
                            },
                            {
                                "type": "field",
                                "name": "body",
                                "extAttrs": [],
                                "idlType": { "type": "dictionary-type", "extAttrs": [], "generic": "", "nullable": true, "union": false, "idlType": "BodyInit" },
                                "default": null,
                                "required": true
                            }
                        ],
                        "extAttrs": [],
                        "partial": false
                    }
                },
                "idlExtendedNames": {}
            }
        }
        """);

        var dict = Assert.IsType<IdlDictionary>(spec.Definitions["RequestInit"]);
        Assert.Equal(2, dict.Members.Count);
        Assert.False(dict.Members[0].IsRequired);
        Assert.Equal("ByteString", dict.Members[0].Type.TypeName);
        Assert.True(dict.Members[1].IsRequired);
        Assert.True(dict.Members[1].Type.IsNullable);
    }

    [Fact]
    public void Parses_typedef_with_union() {
        var spec = Parse("""
        {
            "spec": { "title": "Fetch", "url": "https://fetch.spec.whatwg.org/" },
            "idlparsed": {
                "idlNames": {
                    "HeadersInit": {
                        "type": "typedef",
                        "name": "HeadersInit",
                        "idlType": {
                            "type": "typedef-type",
                            "extAttrs": [],
                            "generic": "",
                            "nullable": false,
                            "union": true,
                            "idlType": [
                                { "type": "typedef-type", "extAttrs": [], "generic": "sequence", "nullable": false, "union": false, "idlType": [{ "type": "typedef-type", "extAttrs": [], "generic": "", "nullable": false, "union": false, "idlType": "ByteString" }] },
                                { "type": "typedef-type", "extAttrs": [], "generic": "", "nullable": false, "union": false, "idlType": "ByteString" }
                            ]
                        },
                        "extAttrs": []
                    }
                },
                "idlExtendedNames": {}
            }
        }
        """);

        var td = Assert.IsType<IdlTypedef>(spec.Definitions["HeadersInit"]);
        Assert.True(td.Type.IsUnion);
        Assert.Equal(2, td.Type.UnionMemberTypes.Count);
        Assert.Equal("sequence", td.Type.UnionMemberTypes[0].Generic);
        Assert.Equal("ByteString", td.Type.UnionMemberTypes[1].TypeName);
    }

    [Fact]
    public void Parses_generic_types() {
        var spec = Parse("""
        {
            "spec": { "title": "Test", "url": "" },
            "idlparsed": {
                "idlNames": {
                    "Foo": {
                        "type": "interface",
                        "name": "Foo",
                        "inheritance": null,
                        "members": [
                            {
                                "type": "operation",
                                "name": "doStuff",
                                "idlType": { "type": "return-type", "extAttrs": [], "generic": "Promise", "nullable": false, "union": false, "idlType": [{ "type": "return-type", "extAttrs": [], "generic": "", "nullable": false, "union": false, "idlType": "ArrayBuffer" }] },
                                "arguments": [],
                                "extAttrs": [],
                                "special": ""
                            }
                        ],
                        "extAttrs": [],
                        "partial": false
                    }
                },
                "idlExtendedNames": {}
            }
        }
        """);

        var iface = Assert.IsType<IdlInterface>(spec.Definitions["Foo"]);
        var op = Assert.IsType<IdlOperation>(iface.Members[0]);
        Assert.Equal("Promise", op.ReturnType.Generic);
        Assert.Single(op.ReturnType.TypeArguments);
        Assert.Equal("ArrayBuffer", op.ReturnType.TypeArguments[0].TypeName);
    }

    [Fact]
    public void Parses_includes_statement() {
        var spec = Parse("""
        {
            "spec": { "title": "Fetch", "url": "https://fetch.spec.whatwg.org/" },
            "idlparsed": {
                "idlNames": {},
                "idlExtendedNames": {
                    "Request": [
                        { "fragment": "Request includes Body;", "type": "includes", "extAttrs": [], "target": "Request", "includes": "Body" }
                    ]
                }
            }
        }
        """);

        var inc = Assert.Single(spec.IncludesStatements);
        Assert.Equal("Request", inc.Target);
        Assert.Equal("Body", inc.Includes);
    }

    [Fact]
    public void Parses_mixin() {
        var spec = Parse("""
        {
            "spec": { "title": "Fetch", "url": "https://fetch.spec.whatwg.org/" },
            "idlparsed": {
                "idlNames": {
                    "Body": {
                        "type": "interface mixin",
                        "name": "Body",
                        "inheritance": null,
                        "members": [
                            {
                                "type": "attribute",
                                "name": "bodyUsed",
                                "idlType": { "type": "attribute-type", "extAttrs": [], "generic": "", "nullable": false, "union": false, "idlType": "boolean" },
                                "extAttrs": [],
                                "special": "",
                                "readonly": true
                            }
                        ],
                        "extAttrs": [],
                        "partial": false
                    }
                },
                "idlExtendedNames": {}
            }
        }
        """);

        var mixin = Assert.IsType<IdlInterface>(spec.Definitions["Body"]);
        Assert.Equal("interface mixin", mixin.Kind);
        Assert.Single(mixin.Members);
    }

    [Fact]
    public void Parses_callback() {
        var spec = Parse("""
        {
            "spec": { "title": "HTML", "url": "" },
            "idlparsed": {
                "idlNames": {
                    "BlobCallback": {
                        "type": "callback",
                        "name": "BlobCallback",
                        "idlType": { "type": "return-type", "extAttrs": [], "generic": "", "nullable": false, "union": false, "idlType": "undefined" },
                        "arguments": [
                            { "type": "argument", "name": "blob", "extAttrs": [], "idlType": { "type": "argument-type", "extAttrs": [], "generic": "", "nullable": true, "union": false, "idlType": "Blob" }, "default": null, "optional": false, "variadic": false }
                        ],
                        "extAttrs": []
                    }
                },
                "idlExtendedNames": {}
            }
        }
        """);

        var cb = Assert.IsType<IdlCallback>(spec.Definitions["BlobCallback"]);
        Assert.Equal("undefined", cb.ReturnType.TypeName);
        Assert.Single(cb.Arguments);
        Assert.Equal("Blob", cb.Arguments[0].Type.TypeName);
        Assert.True(cb.Arguments[0].Type.IsNullable);
    }

    [Fact]
    public void Parses_optional_with_default() {
        var spec = Parse("""
        {
            "spec": { "title": "Test", "url": "" },
            "idlparsed": {
                "idlNames": {
                    "Foo": {
                        "type": "interface",
                        "name": "Foo",
                        "inheritance": null,
                        "members": [
                            {
                                "type": "operation",
                                "name": "count",
                                "idlType": { "type": "return-type", "extAttrs": [], "generic": "", "nullable": false, "union": false, "idlType": "undefined" },
                                "arguments": [
                                    { "type": "argument", "name": "label", "extAttrs": [], "idlType": { "type": "argument-type", "extAttrs": [], "generic": "", "nullable": false, "union": false, "idlType": "DOMString" }, "default": { "type": "string", "value": "default" }, "optional": true, "variadic": false }
                                ],
                                "extAttrs": [],
                                "special": ""
                            }
                        ],
                        "extAttrs": [],
                        "partial": false
                    }
                },
                "idlExtendedNames": {}
            }
        }
        """);

        var iface = Assert.IsType<IdlInterface>(spec.Definitions["Foo"]);
        var op = Assert.IsType<IdlOperation>(iface.Members[0]);
        var arg = op.Arguments[0];
        Assert.True(arg.IsOptional);
        Assert.NotNull(arg.Default);
        Assert.Equal("string", arg.Default.Type);
        Assert.Equal("default", arg.Default.Value);
    }

    [Fact]
    public void Parses_extended_attributes_with_identifier_list() {
        var spec = Parse("""
        {
            "spec": { "title": "Test", "url": "" },
            "idlparsed": {
                "idlNames": {
                    "Foo": {
                        "type": "interface",
                        "name": "Foo",
                        "inheritance": null,
                        "members": [],
                        "extAttrs": [
                            { "type": "extended-attribute", "name": "Exposed", "rhs": { "type": "identifier-list", "value": [{ "value": "Window" }, { "value": "Worker" }] }, "arguments": [] },
                            { "type": "extended-attribute", "name": "SameObject", "rhs": null, "arguments": [] }
                        ],
                        "partial": false
                    }
                },
                "idlExtendedNames": {}
            }
        }
        """);

        var iface = Assert.IsType<IdlInterface>(spec.Definitions["Foo"]);
        Assert.Equal(2, iface.ExtAttrs.Count);

        Assert.Equal("Exposed", iface.ExtAttrs[0].Name);
        Assert.Equal("identifier-list", iface.ExtAttrs[0].Rhs?.Type);
        Assert.Equal(2, iface.ExtAttrs[0].Rhs?.Values.Count);
        Assert.Equal("Window", iface.ExtAttrs[0].Rhs?.Values[0].Value);
        Assert.Equal("Worker", iface.ExtAttrs[0].Rhs?.Values[1].Value);

        Assert.Equal("SameObject", iface.ExtAttrs[1].Name);
    }

    [Fact]
    public void Parses_partial_definitions_from_extended_names() {
        var spec = Parse("""
        {
            "spec": { "title": "Test", "url": "" },
            "idlparsed": {
                "idlNames": {},
                "idlExtendedNames": {
                    "Window": [
                        {
                            "type": "interface",
                            "name": "Window",
                            "inheritance": null,
                            "members": [
                                { "type": "attribute", "name": "customProp", "idlType": { "type": "attribute-type", "extAttrs": [], "generic": "", "nullable": false, "union": false, "idlType": "DOMString" }, "extAttrs": [], "special": "", "readonly": false }
                            ],
                            "extAttrs": [],
                            "partial": true
                        }
                    ]
                }
            }
        }
        """);

        var partial = Assert.Single(spec.PartialDefinitions);
        var iface = Assert.IsType<IdlInterface>(partial);
        Assert.Equal("Window", iface.Name);
        Assert.True(iface.IsPartial);
        Assert.Single(iface.Members);
    }

    private static string SpecPath(string relativePath) {
        // Walk up from bin output to repo root
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "BrowserApi.sln")))
            dir = Path.GetDirectoryName(dir);
        return Path.Combine(dir!, relativePath);
    }

    [Fact]
    public void Reads_real_spec_file() {
        var reader = new WebRefJsonReader();
        var spec = reader.ReadSpec(SpecPath("specs/idlparsed/console.json"));

        Assert.Equal("Console Standard", spec.SpecTitle);
        Assert.True(spec.Definitions.ContainsKey("console"));

        var console = Assert.IsType<IdlInterface>(spec.Definitions["console"]);
        Assert.Equal("namespace", console.Kind);
        Assert.True(console.Members.Count > 5);
    }

    [Fact]
    public void Reads_real_fetch_spec() {
        var reader = new WebRefJsonReader();
        var spec = reader.ReadSpec(SpecPath("specs/idlparsed/fetch.json"));

        Assert.True(spec.Definitions.ContainsKey("Request"));
        Assert.True(spec.Definitions.ContainsKey("Response"));
        Assert.True(spec.Definitions.ContainsKey("Headers"));
        Assert.True(spec.Definitions.ContainsKey("RequestInit"));
        Assert.True(spec.Definitions.ContainsKey("RequestDestination"));
        Assert.True(spec.Definitions.ContainsKey("Body"));

        var request = Assert.IsType<IdlInterface>(spec.Definitions["Request"]);
        Assert.Equal("interface", request.Kind);
        Assert.True(request.Members.Count > 0);

        var body = Assert.IsType<IdlInterface>(spec.Definitions["Body"]);
        Assert.Equal("interface mixin", body.Kind);

        Assert.True(spec.IncludesStatements.Count > 0);
    }
}
