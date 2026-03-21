using BrowserApi.Generator.Ast;
using BrowserApi.Generator.Resolution;
using BrowserApi.Generator.Transform;

namespace BrowserApi.Generator.Tests.Transform;

public class IdlToCSharpTransformerTests {
    private static IdlToCSharpTransformer CreateTransformer() {
        return new IdlToCSharpTransformer(new IdlResolvedModel());
    }

    [Fact]
    public void Transforms_interface_with_pascalcase() {
        var transformer = CreateTransformer();
        var iface = new IdlInterface {
            Name = "HTMLElement",
            Kind = "interface",
            SpecTitle = "HTML Standard",
            Inheritance = "Element"
        };

        var result = transformer.TransformInterface(iface);

        Assert.Equal("HtmlElement", result.Name);
        Assert.Equal("Element", result.BaseClass);
        Assert.Equal("BrowserApi.Dom", result.Namespace);
    }

    [Fact]
    public void Transforms_readonly_attribute() {
        var transformer = CreateTransformer();
        var iface = new IdlInterface {
            Name = "Request",
            Kind = "interface",
            SpecTitle = "Fetch Standard",
            Members = [
                new IdlAttribute {
                    Name = "method",
                    Type = new IdlType { TypeName = "ByteString" },
                    IsReadOnly = true
                }
            ]
        };

        var result = transformer.TransformInterface(iface);
        var prop = Assert.Single(result.Properties);
        Assert.Equal("Method", prop.Name);
        Assert.Equal("string", prop.CSharpType);
        Assert.True(prop.IsReadOnly);
    }

    [Fact]
    public void Transforms_async_method() {
        var transformer = CreateTransformer();
        var iface = new IdlInterface {
            Name = "Body",
            Kind = "interface",
            Members = [
                new IdlOperation {
                    Name = "text",
                    ReturnType = new IdlType {
                        Generic = "Promise",
                        TypeArguments = [new IdlType { TypeName = "USVString" }]
                    }
                }
            ]
        };

        var result = transformer.TransformInterface(iface);
        var method = Assert.Single(result.Methods);
        Assert.Equal("TextAsync", method.Name);
        Assert.Equal("Task<string>", method.ReturnType);
        Assert.True(method.IsAsync);
    }

    [Fact]
    public void Transforms_static_attribute() {
        var transformer = CreateTransformer();
        var iface = new IdlInterface {
            Name = "Foo",
            Kind = "interface",
            Members = [
                new IdlAttribute {
                    Name = "count",
                    Type = new IdlType { TypeName = "unsigned long" },
                    Special = "static"
                }
            ]
        };

        var result = transformer.TransformInterface(iface);
        var prop = Assert.Single(result.Properties);
        Assert.True(prop.IsStatic);
        Assert.Equal("uint", prop.CSharpType);
    }

    [Fact]
    public void Transforms_optional_param_with_default() {
        var transformer = CreateTransformer();
        var iface = new IdlInterface {
            Name = "console",
            Kind = "namespace",
            SpecTitle = "Console Standard",
            Members = [
                new IdlOperation {
                    Name = "count",
                    ReturnType = new IdlType { TypeName = "undefined" },
                    Arguments = [
                        new IdlArgument {
                            Name = "label",
                            Type = new IdlType { TypeName = "DOMString" },
                            IsOptional = true,
                            Default = new IdlDefaultValue { Type = "string", Value = "default" }
                        }
                    ]
                }
            ]
        };

        var result = transformer.TransformInterface(iface);
        var method = Assert.Single(result.Methods);
        Assert.Equal("Count", method.Name);
        Assert.Equal("void", method.ReturnType);

        var param = Assert.Single(method.Parameters);
        Assert.Equal("label", param.Name);
        Assert.Equal("string", param.CSharpType);
        Assert.Equal("\"default\"", param.DefaultValue);
    }

    [Fact]
    public void Transforms_enum() {
        var transformer = CreateTransformer();
        var e = new IdlEnum {
            Name = "RequestDestination",
            SpecTitle = "Fetch Standard",
            Values = [
                new IdlEnumValue { Value = "" },
                new IdlEnumValue { Value = "audio" },
                new IdlEnumValue { Value = "document" }
            ]
        };

        var result = transformer.TransformEnum(e);
        Assert.Equal("RequestDestination", result.Name);
        Assert.Equal("BrowserApi.Fetch", result.Namespace);
        Assert.Equal(3, result.Members.Count);
        Assert.Equal("Empty", result.Members[0].Name);
        Assert.Equal("", result.Members[0].StringValue);
        Assert.Equal("Audio", result.Members[1].Name);
        Assert.Equal("Document", result.Members[2].Name);
    }

    [Fact]
    public void Transforms_type_mappings() {
        var transformer = CreateTransformer();
        var iface = new IdlInterface {
            Name = "Foo",
            Kind = "interface",
            Members = [
                new IdlAttribute { Name = "a", Type = new IdlType { TypeName = "boolean" } },
                new IdlAttribute { Name = "b", Type = new IdlType { TypeName = "unsigned long" } },
                new IdlAttribute { Name = "c", Type = new IdlType { TypeName = "double" } },
                new IdlAttribute { Name = "d", Type = new IdlType { TypeName = "long long" } },
                new IdlAttribute { Name = "e", Type = new IdlType { TypeName = "DOMString", IsNullable = true } },
            ]
        };

        var result = transformer.TransformInterface(iface);
        Assert.Equal("bool", result.Properties[0].CSharpType);
        Assert.Equal("uint", result.Properties[1].CSharpType);
        Assert.Equal("double", result.Properties[2].CSharpType);
        Assert.Equal("long", result.Properties[3].CSharpType);
        Assert.Equal("string?", result.Properties[4].CSharpType);
    }

    [Fact]
    public void Transforms_promise_undefined_to_task() {
        var transformer = CreateTransformer();
        var iface = new IdlInterface {
            Name = "Foo",
            Kind = "interface",
            Members = [
                new IdlOperation {
                    Name = "doWork",
                    ReturnType = new IdlType {
                        Generic = "Promise",
                        TypeArguments = [new IdlType { TypeName = "undefined" }]
                    }
                }
            ]
        };

        var result = transformer.TransformInterface(iface);
        var method = Assert.Single(result.Methods);
        Assert.Equal("Task", method.ReturnType);
        Assert.True(method.IsAsync);
    }

    [Fact]
    public void Transforms_sequence_type() {
        var transformer = CreateTransformer();
        var iface = new IdlInterface {
            Name = "Foo",
            Kind = "interface",
            Members = [
                new IdlAttribute {
                    Name = "items",
                    Type = new IdlType {
                        Generic = "sequence",
                        TypeArguments = [new IdlType { TypeName = "DOMString" }]
                    }
                }
            ]
        };

        var result = transformer.TransformInterface(iface);
        Assert.Equal("IReadOnlyList<string>", result.Properties[0].CSharpType);
    }
}
