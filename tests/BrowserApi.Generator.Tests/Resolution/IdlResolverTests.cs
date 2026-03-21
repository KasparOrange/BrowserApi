using BrowserApi.Generator.Ast;
using BrowserApi.Generator.Resolution;

namespace BrowserApi.Generator.Tests.Resolution;

public class IdlResolverTests {
    private readonly IdlResolver _resolver = new();

    [Fact]
    public void Collects_interfaces_enums_dictionaries() {
        var spec = new IdlSpecFile {
            Definitions = {
                ["Foo"] = new IdlInterface { Name = "Foo", Kind = "interface" },
                ["Bar"] = new IdlEnum { Name = "Bar", Values = [new() { Value = "a" }] },
                ["Baz"] = new IdlDictionary { Name = "Baz" }
            }
        };

        var model = _resolver.Resolve([spec]);

        Assert.Single(model.Interfaces);
        Assert.Single(model.Enums);
        Assert.Single(model.Dictionaries);
    }

    [Fact]
    public void Merges_partial_interface_members() {
        var spec1 = new IdlSpecFile {
            Definitions = {
                ["Foo"] = new IdlInterface {
                    Name = "Foo",
                    Kind = "interface",
                    Members = [new IdlAttribute { Name = "a", Type = new IdlType { TypeName = "string" } }]
                }
            }
        };

        var spec2 = new IdlSpecFile {
            PartialDefinitions = [
                new IdlInterface {
                    Name = "Foo",
                    Kind = "interface",
                    IsPartial = true,
                    Members = [new IdlAttribute { Name = "b", Type = new IdlType { TypeName = "long" } }]
                }
            ]
        };

        var model = _resolver.Resolve([spec1, spec2]);

        var foo = model.Interfaces["Foo"];
        Assert.Equal(2, foo.Members.Count);
        Assert.Equal("a", foo.Members[0].Name);
        Assert.Equal("b", foo.Members[1].Name);
    }

    [Fact]
    public void Resolves_includes_copies_mixin_members_to_target() {
        var spec = new IdlSpecFile {
            Definitions = {
                ["Request"] = new IdlInterface {
                    Name = "Request",
                    Kind = "interface",
                    Members = [new IdlAttribute { Name = "url", Type = new IdlType { TypeName = "string" } }]
                },
                ["Body"] = new IdlInterface {
                    Name = "Body",
                    Kind = "interface mixin",
                    Members = [
                        new IdlAttribute { Name = "bodyUsed", Type = new IdlType { TypeName = "boolean" } },
                        new IdlOperation { Name = "text", ReturnType = new IdlType { Generic = "Promise", TypeArguments = [new IdlType { TypeName = "string" }] } }
                    ]
                }
            },
            IncludesStatements = [new() { Target = "Request", Includes = "Body" }]
        };

        var model = _resolver.Resolve([spec]);

        var request = model.Interfaces["Request"];
        Assert.Equal(3, request.Members.Count);
        Assert.Equal("url", request.Members[0].Name);
        Assert.Equal("bodyUsed", request.Members[1].Name);
        Assert.Equal("text", request.Members[2].Name);
    }

    [Fact]
    public void Mixin_removed_from_output_after_includes() {
        var spec = new IdlSpecFile {
            Definitions = {
                ["Request"] = new IdlInterface { Name = "Request", Kind = "interface" },
                ["Body"] = new IdlInterface { Name = "Body", Kind = "interface mixin" }
            },
            IncludesStatements = [new() { Target = "Request", Includes = "Body" }]
        };

        var model = _resolver.Resolve([spec]);

        Assert.True(model.Interfaces.ContainsKey("Request"));
        Assert.False(model.Interfaces.ContainsKey("Body"));
    }

    [Fact]
    public void Builds_inheritance_chain() {
        var spec = new IdlSpecFile {
            Definitions = {
                ["Node"] = new IdlInterface { Name = "Node", Kind = "interface" },
                ["Element"] = new IdlInterface { Name = "Element", Kind = "interface", Inheritance = "Node" },
                ["HTMLElement"] = new IdlInterface { Name = "HTMLElement", Kind = "interface", Inheritance = "Element" }
            }
        };

        var model = _resolver.Resolve([spec]);

        Assert.Equal(["Element", "Node"], model.InheritanceChains["HTMLElement"]);
        Assert.Equal(["Node"], model.InheritanceChains["Element"]);
        Assert.False(model.InheritanceChains.ContainsKey("Node"));
    }

    [Fact]
    public void Missing_includes_target_produces_warning() {
        var spec = new IdlSpecFile {
            Definitions = {
                ["Body"] = new IdlInterface { Name = "Body", Kind = "interface mixin" }
            },
            IncludesStatements = [new() { Target = "NonExistent", Includes = "Body" }]
        };

        var model = _resolver.Resolve([spec]);

        Assert.Contains(model.Warnings, w => w.Contains("NonExistent"));
    }

    [Fact]
    public void Missing_includes_mixin_produces_warning() {
        var spec = new IdlSpecFile {
            Definitions = {
                ["Request"] = new IdlInterface { Name = "Request", Kind = "interface" }
            },
            IncludesStatements = [new() { Target = "Request", Includes = "NonExistent" }]
        };

        var model = _resolver.Resolve([spec]);

        Assert.Contains(model.Warnings, w => w.Contains("NonExistent"));
    }

    [Fact]
    public void Cross_spec_merge() {
        var spec1 = new IdlSpecFile {
            Definitions = {
                ["EventTarget"] = new IdlInterface {
                    Name = "EventTarget",
                    Kind = "interface",
                    Members = [new IdlOperation { Name = "addEventListener" }]
                }
            }
        };

        var spec2 = new IdlSpecFile {
            Definitions = {
                ["Request"] = new IdlInterface {
                    Name = "Request",
                    Kind = "interface",
                    Inheritance = "EventTarget"
                }
            }
        };

        var model = _resolver.Resolve([spec1, spec2]);

        Assert.True(model.Interfaces.ContainsKey("EventTarget"));
        Assert.True(model.Interfaces.ContainsKey("Request"));
        Assert.Equal(["EventTarget"], model.InheritanceChains["Request"]);
    }

    [Fact]
    public void Collects_typedefs_and_callbacks() {
        var spec = new IdlSpecFile {
            Definitions = {
                ["HeadersInit"] = new IdlTypedef { Name = "HeadersInit", Type = new IdlType { IsUnion = true } },
                ["BlobCallback"] = new IdlCallback { Name = "BlobCallback", ReturnType = new IdlType { TypeName = "undefined" } }
            }
        };

        var model = _resolver.Resolve([spec]);

        Assert.Single(model.Typedefs);
        Assert.Single(model.Callbacks);
    }

    [Fact]
    public void Partial_dictionary_members_merged() {
        var spec1 = new IdlSpecFile {
            Definitions = {
                ["Opts"] = new IdlDictionary {
                    Name = "Opts",
                    Members = [new IdlField { Name = "a" }]
                }
            }
        };

        var spec2 = new IdlSpecFile {
            PartialDefinitions = [
                new IdlDictionary {
                    Name = "Opts",
                    IsPartial = true,
                    Members = [new IdlField { Name = "b" }]
                }
            ]
        };

        var model = _resolver.Resolve([spec1, spec2]);

        Assert.Equal(2, model.Dictionaries["Opts"].Members.Count);
    }
}
