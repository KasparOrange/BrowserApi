using BrowserApi.Generator.Ast;
using BrowserApi.Generator.Resolution;
using BrowserApi.Generator.Transform;

namespace BrowserApi.Generator.Tests.Transform;

public class UnionTypeTests {
    private static IdlToCSharpTransformer CreateTransformer() {
        return new IdlToCSharpTransformer(new IdlResolvedModel());
    }

    [Fact]
    public void Union_return_type_maps_to_object() {
        var transformer = CreateTransformer();
        var iface = new IdlInterface {
            Name = "Foo",
            Kind = "interface",
            Members = [
                new IdlOperation {
                    Name = "getValue",
                    ReturnType = new IdlType {
                        IsUnion = true,
                        UnionMemberTypes = [
                            new IdlType { TypeName = "DOMString" },
                            new IdlType { TypeName = "long" }
                        ]
                    }
                }
            ]
        };

        var result = transformer.TransformInterface(iface);
        var method = Assert.Single(result.Methods);
        Assert.Equal("object", method.ReturnType);
    }

    [Fact]
    public void Union_property_type_maps_to_object() {
        var transformer = CreateTransformer();
        var iface = new IdlInterface {
            Name = "Foo",
            Kind = "interface",
            Members = [
                new IdlAttribute {
                    Name = "value",
                    Type = new IdlType {
                        IsUnion = true,
                        UnionMemberTypes = [
                            new IdlType { TypeName = "DOMString" },
                            new IdlType { TypeName = "boolean" }
                        ]
                    }
                }
            ]
        };

        var result = transformer.TransformInterface(iface);
        var prop = Assert.Single(result.Properties);
        Assert.Equal("object", prop.CSharpType);
    }

    [Fact]
    public void Union_parameter_maps_to_object() {
        var transformer = CreateTransformer();
        var iface = new IdlInterface {
            Name = "Foo",
            Kind = "interface",
            Members = [
                new IdlOperation {
                    Name = "doStuff",
                    ReturnType = new IdlType { TypeName = "undefined" },
                    Arguments = [
                        new IdlArgument {
                            Name = "input",
                            Type = new IdlType {
                                IsUnion = true,
                                UnionMemberTypes = [
                                    new IdlType { TypeName = "DOMString" },
                                    new IdlType { TypeName = "Request" }
                                ]
                            }
                        }
                    ]
                }
            ]
        };

        var result = transformer.TransformInterface(iface);
        var method = Assert.Single(result.Methods);
        Assert.Equal("object", method.Parameters[0].CSharpType);
    }
}
