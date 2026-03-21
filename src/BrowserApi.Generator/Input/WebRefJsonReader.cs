using System.Text.Json;
using BrowserApi.Generator.Ast;

namespace BrowserApi.Generator.Input;

public sealed class WebRefJsonReader : ISpecReader {
    public IdlSpecFile ReadSpec(string filePath) {
        var json = File.ReadAllText(filePath);
        using var doc = JsonDocument.Parse(json);
        return ParseSpecFile(doc.RootElement);
    }

    public IReadOnlyList<IdlSpecFile> ReadAllSpecs(string directoryPath) {
        var files = Directory.GetFiles(directoryPath, "*.json");
        Array.Sort(files);
        return files.Select(ReadSpec).ToList();
    }

    internal IdlSpecFile ParseSpecFile(JsonElement root) {
        var spec = new IdlSpecFile();

        if (root.TryGetProperty("spec", out var specEl)) {
            spec.SpecTitle = specEl.GetOptionalString("title");
            spec.SpecUrl = specEl.GetOptionalString("url");
        }

        if (!root.TryGetProperty("idlparsed", out var idlparsed))
            return spec;

        if (idlparsed.TryGetProperty("idlNames", out var idlNames)) {
            foreach (var prop in idlNames.EnumerateObject()) {
                var def = ParseDefinition(prop.Value, spec.SpecTitle, spec.SpecUrl);
                if (def != null)
                    spec.Definitions[prop.Name] = def;
            }
        }

        if (idlparsed.TryGetProperty("idlExtendedNames", out var extNames)) {
            foreach (var prop in extNames.EnumerateObject()) {
                foreach (var item in prop.Value.EnumerateArray()) {
                    var type = item.GetOptionalString("type");
                    if (type == "includes") {
                        spec.IncludesStatements.Add(new IdlIncludesStatement {
                            Target = item.GetOptionalString("target") ?? "",
                            Includes = item.GetOptionalString("includes") ?? ""
                        });
                    } else {
                        var def = ParseDefinition(item, spec.SpecTitle, spec.SpecUrl);
                        if (def != null)
                            spec.PartialDefinitions.Add(def);
                    }
                }
            }
        }

        if (idlparsed.TryGetProperty("externalDependencies", out var extDeps) &&
            extDeps.ValueKind == JsonValueKind.Array) {
            foreach (var dep in extDeps.EnumerateArray()) {
                if (dep.GetString() is string s)
                    spec.ExternalDependencies.Add(s);
            }
        }

        return spec;
    }

    internal IdlDefinition? ParseDefinition(JsonElement el, string? specTitle, string? specUrl) {
        var type = el.GetOptionalString("type");
        IdlDefinition? def = type switch {
            "interface" or "interface mixin" or "namespace" or "callback interface" => ParseInterface(el, type),
            "enum" => ParseEnum(el),
            "dictionary" => ParseDictionary(el),
            "typedef" => ParseTypedef(el),
            "callback" => ParseCallback(el),
            _ => null
        };

        if (def != null) {
            def.Name = el.GetOptionalString("name") ?? "";
            def.SpecTitle = specTitle;
            def.SpecUrl = specUrl;
            def.Href = el.GetOptionalString("href");
            def.ExtAttrs = ParseExtAttrs(el);
        }

        return def;
    }

    private IdlInterface ParseInterface(JsonElement el, string kind) {
        var iface = new IdlInterface {
            Kind = kind,
            Inheritance = el.GetOptionalString("inheritance"),
            IsPartial = el.GetOptionalBool("partial")
        };

        if (el.TryGetProperty("members", out var members)) {
            foreach (var m in members.EnumerateArray()) {
                var member = ParseMember(m);
                if (member != null)
                    iface.Members.Add(member);
            }
        }

        return iface;
    }

    private IdlEnum ParseEnum(JsonElement el) {
        var e = new IdlEnum();
        if (el.TryGetProperty("values", out var values)) {
            foreach (var v in values.EnumerateArray()) {
                e.Values.Add(new IdlEnumValue {
                    Value = v.GetOptionalString("value") ?? "",
                    Href = v.GetOptionalString("href")
                });
            }
        }
        return e;
    }

    private IdlDictionary ParseDictionary(JsonElement el) {
        var dict = new IdlDictionary {
            Inheritance = el.GetOptionalString("inheritance"),
            IsPartial = el.GetOptionalBool("partial")
        };

        if (el.TryGetProperty("members", out var members)) {
            foreach (var m in members.EnumerateArray()) {
                dict.Members.Add(ParseField(m));
            }
        }

        return dict;
    }

    private IdlTypedef ParseTypedef(JsonElement el) {
        var td = new IdlTypedef();
        if (el.TryGetProperty("idlType", out var idlType))
            td.Type = ParseIdlType(idlType);
        return td;
    }

    private IdlCallback ParseCallback(JsonElement el) {
        var cb = new IdlCallback();
        if (el.TryGetProperty("idlType", out var idlType))
            cb.ReturnType = ParseIdlType(idlType);
        if (el.TryGetProperty("arguments", out var args))
            cb.Arguments = ParseArguments(args);
        return cb;
    }

    internal IdlMember? ParseMember(JsonElement el) {
        var type = el.GetOptionalString("type");
        return type switch {
            "operation" => ParseOperation(el),
            "attribute" => ParseAttribute(el),
            "constructor" => ParseConstructor(el),
            "const" => ParseConst(el),
            "field" => ParseField(el),
            "iterable" or "maplike" or "setlike" => ParseIterable(el),
            _ => null
        };
    }

    private IdlOperation ParseOperation(JsonElement el) {
        var op = new IdlOperation {
            Name = el.GetOptionalString("name"),
            Href = el.GetOptionalString("href"),
            ExtAttrs = ParseExtAttrs(el),
            Special = el.GetOptionalString("special")
        };
        if (el.TryGetProperty("idlType", out var idlType))
            op.ReturnType = ParseIdlType(idlType);
        if (el.TryGetProperty("arguments", out var args))
            op.Arguments = ParseArguments(args);
        return op;
    }

    private IdlAttribute ParseAttribute(JsonElement el) {
        var attr = new IdlAttribute {
            Name = el.GetOptionalString("name"),
            Href = el.GetOptionalString("href"),
            ExtAttrs = ParseExtAttrs(el),
            IsReadOnly = el.GetOptionalBool("readonly"),
            Special = el.GetOptionalString("special")
        };
        if (el.TryGetProperty("idlType", out var idlType))
            attr.Type = ParseIdlType(idlType);
        return attr;
    }

    private IdlConstructor ParseConstructor(JsonElement el) {
        var ctor = new IdlConstructor {
            Href = el.GetOptionalString("href"),
            ExtAttrs = ParseExtAttrs(el)
        };
        if (el.TryGetProperty("arguments", out var args))
            ctor.Arguments = ParseArguments(args);
        return ctor;
    }

    private IdlConst ParseConst(JsonElement el) {
        var c = new IdlConst {
            Name = el.GetOptionalString("name"),
            Href = el.GetOptionalString("href"),
            ExtAttrs = ParseExtAttrs(el)
        };
        if (el.TryGetProperty("idlType", out var idlType))
            c.Type = ParseIdlType(idlType);
        if (el.TryGetProperty("value", out var value))
            c.Value = ParseConstValue(value);
        return c;
    }

    private IdlField ParseField(JsonElement el) {
        var f = new IdlField {
            Name = el.GetOptionalString("name"),
            Href = el.GetOptionalString("href"),
            ExtAttrs = ParseExtAttrs(el),
            IsRequired = el.GetOptionalBool("required")
        };
        if (el.TryGetProperty("idlType", out var idlType))
            f.Type = ParseIdlType(idlType);
        if (el.TryGetProperty("default", out var def) && def.ValueKind != JsonValueKind.Null)
            f.Default = ParseDefaultValue(def);
        return f;
    }

    private IdlIterable ParseIterable(JsonElement el) {
        var it = new IdlIterable {
            Name = el.GetOptionalString("name"),
            Href = el.GetOptionalString("href"),
            ExtAttrs = ParseExtAttrs(el),
            IsReadOnly = el.GetOptionalBool("readonly")
        };
        if (el.TryGetProperty("idlType", out var idlType)) {
            if (idlType.ValueKind == JsonValueKind.Array) {
                foreach (var t in idlType.EnumerateArray())
                    it.TypeParams.Add(ParseIdlType(t));
            } else {
                it.TypeParams.Add(ParseIdlType(idlType));
            }
        }
        return it;
    }

    internal IdlType ParseIdlType(JsonElement el) {
        var t = new IdlType {
            IsNullable = el.GetOptionalBool("nullable"),
            ExtAttrs = ParseExtAttrs(el)
        };

        var generic = el.GetOptionalString("generic");
        if (!string.IsNullOrEmpty(generic))
            t.Generic = generic;

        var isUnion = el.GetOptionalBool("union");
        t.IsUnion = isUnion;

        if (isUnion) {
            if (el.TryGetProperty("idlType", out var unionMembers) &&
                unionMembers.ValueKind == JsonValueKind.Array) {
                foreach (var member in unionMembers.EnumerateArray())
                    t.UnionMemberTypes.Add(ParseIdlType(member));
            }
        } else if (!string.IsNullOrEmpty(generic)) {
            if (el.TryGetProperty("idlType", out var typeArgs) &&
                typeArgs.ValueKind == JsonValueKind.Array) {
                foreach (var arg in typeArgs.EnumerateArray())
                    t.TypeArguments.Add(ParseIdlType(arg));
            }
        } else {
            if (el.TryGetProperty("idlType", out var idlType)) {
                if (idlType.ValueKind == JsonValueKind.String) {
                    t.TypeName = idlType.GetString();
                } else if (idlType.ValueKind == JsonValueKind.Array) {
                    // Unwrap single-element array (some specs produce this)
                    foreach (var inner in idlType.EnumerateArray()) {
                        var parsed = ParseIdlType(inner);
                        t.TypeName = parsed.TypeName;
                        t.Generic = parsed.Generic;
                        t.IsUnion = parsed.IsUnion;
                        t.TypeArguments = parsed.TypeArguments;
                        t.UnionMemberTypes = parsed.UnionMemberTypes;
                        break;
                    }
                }
            }
        }

        return t;
    }

    private List<IdlArgument> ParseArguments(JsonElement el) {
        var args = new List<IdlArgument>();
        foreach (var a in el.EnumerateArray()) {
            var arg = new IdlArgument {
                Name = a.GetOptionalString("name") ?? "",
                IsOptional = a.GetOptionalBool("optional"),
                IsVariadic = a.GetOptionalBool("variadic"),
                ExtAttrs = ParseExtAttrs(a)
            };
            if (a.TryGetProperty("idlType", out var idlType))
                arg.Type = ParseIdlType(idlType);
            if (a.TryGetProperty("default", out var def) && def.ValueKind != JsonValueKind.Null)
                arg.Default = ParseDefaultValue(def);
            args.Add(arg);
        }
        return args;
    }

    private List<IdlExtendedAttribute> ParseExtAttrs(JsonElement el) {
        var attrs = new List<IdlExtendedAttribute>();
        if (!el.TryGetProperty("extAttrs", out var extAttrs))
            return attrs;
        foreach (var a in extAttrs.EnumerateArray()) {
            var attr = new IdlExtendedAttribute {
                Name = a.GetOptionalString("name") ?? ""
            };
            if (a.TryGetProperty("rhs", out var rhs) && rhs.ValueKind != JsonValueKind.Null)
                attr.Rhs = ParseExtAttrRhs(rhs);
            if (a.TryGetProperty("arguments", out var args))
                attr.Arguments = ParseArguments(args);
            attrs.Add(attr);
        }
        return attrs;
    }

    private IdlExtAttrRhs ParseExtAttrRhs(JsonElement el) {
        var rhs = new IdlExtAttrRhs {
            Type = el.GetOptionalString("type") ?? ""
        };

        if (el.TryGetProperty("value", out var value)) {
            if (value.ValueKind == JsonValueKind.String) {
                rhs.Value = value.GetString();
            } else if (value.ValueKind == JsonValueKind.Array) {
                foreach (var v in value.EnumerateArray()) {
                    var str = v.GetOptionalString("value") ?? "";
                    rhs.Values.Add(new IdlExtAttrRhsValue { Value = str });
                }
            }
        }

        return rhs;
    }

    private IdlDefaultValue ParseDefaultValue(JsonElement el) {
        var dv = new IdlDefaultValue {
            Type = el.GetOptionalString("type") ?? ""
        };
        if (el.TryGetProperty("value", out var value)) {
            dv.Value = value.ValueKind switch {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.TryGetInt64(out var l) ? l : value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => null
            };
        }
        return dv;
    }

    private IdlConstValue ParseConstValue(JsonElement el) {
        var cv = new IdlConstValue {
            Type = el.GetOptionalString("type") ?? ""
        };
        if (el.TryGetProperty("value", out var value)) {
            cv.Value = value.ValueKind switch {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.TryGetInt64(out var l) ? l : value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null
            };
        }
        return cv;
    }
}

internal static class JsonElementExtensions {
    public static string? GetOptionalString(this JsonElement el, string propertyName) {
        return el.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    public static bool GetOptionalBool(this JsonElement el, string propertyName) {
        return el.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.True;
    }
}
