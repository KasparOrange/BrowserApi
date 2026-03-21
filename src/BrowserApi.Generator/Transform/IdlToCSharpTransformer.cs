using BrowserApi.Generator.Ast;
using BrowserApi.Generator.CSharpModel;
using BrowserApi.Generator.Resolution;

namespace BrowserApi.Generator.Transform;

public sealed class IdlToCSharpTransformer {
    private readonly TypeMapper _typeMapper;

    public IdlToCSharpTransformer(IdlResolvedModel model) {
        _typeMapper = new TypeMapper(model);
    }

    public CSharpGeneratedModel Transform(IdlResolvedModel model) {
        var result = new CSharpGeneratedModel();

        foreach (var (_, iface) in model.Interfaces)
            result.Classes.Add(TransformInterface(iface));

        foreach (var (_, e) in model.Enums)
            result.Enums.Add(TransformEnum(e));

        foreach (var (_, dict) in model.Dictionaries)
            result.RecordClasses.Add(TransformDictionary(dict));

        foreach (var (_, cb) in model.Callbacks)
            result.Delegates.Add(TransformCallback(cb));

        return result;
    }

    private static readonly Dictionary<string, string> TypeRenames = new() {
        ["File"] = "WebFile",
        ["Range"] = "DomRange",
    };

    internal CSharpClass TransformInterface(IdlInterface iface) {
        var csName = NamingConventions.ToPascalCase(iface.Name);
        if (TypeRenames.TryGetValue(csName, out var renamed))
            csName = renamed;
        var jsName = csName != iface.Name ? iface.Name : null;
        var ns = NamespaceMapper.MapToNamespace(iface.SpecTitle);

        var csClass = new CSharpClass {
            Name = csName,
            Namespace = ns,
            BaseClass = iface.Inheritance != null ? NamingConventions.ToPascalCase(iface.Inheritance) : null,
            JsName = jsName,
            Kind = iface.Kind
        };

        var seenNames = new HashSet<string>();
        var seenMethods = new HashSet<string>();

        foreach (var member in iface.Members) {
            switch (member) {
                case IdlAttribute attr: {
                    var prop = TransformAttribute(attr);
                    if (prop.Name != csName && seenNames.Add(prop.Name))
                        csClass.Properties.Add(prop);
                    break;
                }
                case IdlOperation op when !string.IsNullOrEmpty(op.Name): {
                    var method = TransformOperation(op);
                    if (method.Name == csName) break;
                    var sig = method.Name + "(" + string.Join(",", method.Parameters.Select(p => p.CSharpType)) + ")";
                    if (seenMethods.Add(sig) && !seenNames.Contains(method.Name))
                        csClass.Methods.Add(method);
                    break;
                }
                case IdlConstructor ctor: {
                    var c = TransformConstructor(ctor);
                    var sig = "ctor(" + string.Join(",", c.Parameters.Select(p => p.CSharpType)) + ")";
                    if (seenMethods.Add(sig))
                        csClass.Constructors.Add(c);
                    break;
                }
                case IdlConst c: {
                    var constant = TransformConst(c);
                    if (seenNames.Add(constant.Name))
                        csClass.Constants.Add(constant);
                    break;
                }
            }
        }

        // Ensure optional params come after required ones (CS1737)
        foreach (var method in csClass.Methods)
            FixParameterOrder(method.Parameters);
        foreach (var ctor in csClass.Constructors)
            FixParameterOrder(ctor.Parameters);

        return csClass;
    }

    internal CSharpEnum TransformEnum(IdlEnum idlEnum) {
        var ns = NamespaceMapper.MapToNamespace(idlEnum.SpecTitle);
        return new CSharpEnum {
            Name = NamingConventions.ToPascalCase(idlEnum.Name),
            Namespace = ns,
            Members = idlEnum.Values.Select(v => new CSharpEnumMember {
                Name = NamingConventions.ToEnumMemberName(v.Value),
                StringValue = v.Value
            }).ToList()
        };
    }

    internal CSharpRecordClass TransformDictionary(IdlDictionary dict) {
        var csName = NamingConventions.ToPascalCase(dict.Name);
        var ns = NamespaceMapper.MapToNamespace(dict.SpecTitle);

        var rec = new CSharpRecordClass {
            Name = csName,
            Namespace = ns,
            BaseRecord = dict.Inheritance != null ? NamingConventions.ToPascalCase(dict.Inheritance) : null,
            JsName = csName != dict.Name ? dict.Name : null
        };

        foreach (var field in dict.Members) {
            var propType = _typeMapper.MapPropertyType(field.Type);
            var propName = NamingConventions.ToPascalCase(field.Name ?? "");
            var jsFieldName = propName != field.Name ? field.Name : null;

            if (!field.IsRequired && !field.Type.IsNullable && !propType.EndsWith("?"))
                propType += "?";

            rec.Properties.Add(new CSharpRecordProperty {
                Name = propName,
                CSharpType = propType,
                IsRequired = field.IsRequired,
                IsNullable = !field.IsRequired || field.Type.IsNullable,
                JsName = jsFieldName
            });
        }

        return rec;
    }

    internal CSharpDelegate TransformCallback(IdlCallback cb) {
        var ns = NamespaceMapper.MapToNamespace(cb.SpecTitle);
        var returnType = _typeMapper.MapReturnType(cb.ReturnType, out _);
        if (returnType == "void") returnType = "void";

        return new CSharpDelegate {
            Name = NamingConventions.ToPascalCase(cb.Name),
            Namespace = ns,
            ReturnType = returnType,
            Parameters = cb.Arguments.Select(TransformArgument).ToList()
        };
    }

    private CSharpProperty TransformAttribute(IdlAttribute attr) {
        var propType = _typeMapper.MapPropertyType(attr.Type);
        var propName = NamingConventions.ToPascalCase(attr.Name ?? "");
        var jsName = propName != attr.Name ? attr.Name : null;

        return new CSharpProperty {
            Name = propName,
            CSharpType = propType,
            IsReadOnly = attr.IsReadOnly,
            IsNullable = attr.Type.IsNullable,
            IsStatic = attr.Special == "static",
            JsName = jsName
        };
    }

    private CSharpMethod TransformOperation(IdlOperation op) {
        var returnType = _typeMapper.MapReturnType(op.ReturnType, out var isAsync);
        var methodName = NamingConventions.ToPascalCase(op.Name ?? "");
        if (isAsync && !methodName.EndsWith("Async"))
            methodName += "Async";

        var jsName = methodName != op.Name && !isAsync ? op.Name : null;
        if (isAsync) {
            // For async methods the JS name is the original name (without Async suffix)
            var originalPascal = NamingConventions.ToPascalCase(op.Name ?? "");
            jsName = originalPascal != op.Name ? op.Name : null;
        }

        return new CSharpMethod {
            Name = methodName,
            ReturnType = returnType,
            IsAsync = isAsync,
            IsStatic = op.Special == "static",
            JsName = jsName,
            Parameters = op.Arguments.Select(TransformArgument).ToList()
        };
    }

    private CSharpConstructor TransformConstructor(IdlConstructor ctor) {
        return new CSharpConstructor {
            Parameters = ctor.Arguments.Select(TransformArgument).ToList()
        };
    }

    private CSharpConst TransformConst(IdlConst c) {
        var (csType, _) = _typeMapper.MapType(c.Type);
        return new CSharpConst {
            Name = NamingConventions.ToPascalCase(c.Name ?? ""),
            CSharpType = csType,
            Value = c.Value?.Value?.ToString() ?? "default"
        };
    }

    private static readonly HashSet<string> CSharpValueTypes = [
        "bool", "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong",
        "float", "double", "decimal", "char"
    ];

    private CSharpParameter TransformArgument(IdlArgument arg) {
        var paramType = _typeMapper.MapParameterType(arg.Type);
        var paramName = NamingConventions.ToParameterName(arg.Name);

        string? defaultValue = null;
        if (arg.Default != null) {
            defaultValue = MapDefaultValue(arg.Default, ref paramType);
        } else if (arg.IsOptional && !arg.IsVariadic) {
            if (!paramType.EndsWith("?"))
                paramType += "?";
            defaultValue = "null";
        }

        if (arg.IsVariadic) {
            paramType = "params object[]";
            paramName = NamingConventions.ToParameterName(arg.Name);
        }

        return new CSharpParameter {
            Name = paramName,
            CSharpType = paramType,
            IsOptional = arg.IsOptional,
            IsParams = arg.IsVariadic,
            DefaultValue = defaultValue
        };
    }

    private string MapDefaultValue(IdlDefaultValue def, ref string csType) {
        switch (def.Type) {
            case "string":
                if (csType == "string" || csType == "string?")
                    return $"\"{def.Value}\"";
                // String default for non-string type → use default
                if (!csType.EndsWith("?")) csType += "?";
                return "null";

            case "boolean":
                if (csType == "bool" || csType == "bool?")
                    return def.Value is true ? "true" : "false";
                if (!csType.EndsWith("?")) csType += "?";
                return "null";

            case "number":
                if (CSharpValueTypes.Contains(csType.TrimEnd('?'))) {
                    var numVal = def.Value?.ToString() ?? "0";
                    // Ensure float literals have 'f' suffix
                    if (csType.TrimEnd('?') == "float" && !numVal.EndsWith("f"))
                        numVal = numVal.Contains('.') ? numVal + "f" : numVal + ".0f";
                    return numVal;
                }
                if (!csType.EndsWith("?")) csType += "?";
                return "null";

            case "null":
            case "sequence":
            case "dictionary":
                if (!csType.EndsWith("?") && !csType.EndsWith("[]"))
                    csType += "?";
                return "null";

            default:
                return "default";
        }
    }

    private static void FixParameterOrder(List<CSharpParameter> parameters) {
        // Ensure all required params come before optional/default params, and params[] is last
        var required = parameters.Where(p => p.DefaultValue == null && !p.IsParams).ToList();
        var optional = parameters.Where(p => p.DefaultValue != null && !p.IsParams).ToList();
        var paramsP = parameters.Where(p => p.IsParams).ToList();

        parameters.Clear();
        parameters.AddRange(required);
        parameters.AddRange(optional);
        parameters.AddRange(paramsP);
    }
}
