using BrowserApi.Generator.Ast;
using BrowserApi.Generator.Resolution;

namespace BrowserApi.Generator.Transform;

public sealed class TypeMapper {
    private static readonly Dictionary<string, string> PrimitiveMap = new() {
        ["DOMString"] = "string",
        ["USVString"] = "string",
        ["ByteString"] = "string",
        ["CSSOMString"] = "string",
        ["DOMTimeStamp"] = "ulong",
        ["boolean"] = "bool",
        ["byte"] = "sbyte",
        ["octet"] = "byte",
        ["short"] = "short",
        ["unsigned short"] = "ushort",
        ["long"] = "int",
        ["unsigned long"] = "uint",
        ["long long"] = "long",
        ["unsigned long long"] = "ulong",
        ["float"] = "float",
        ["unrestricted float"] = "float",
        ["double"] = "double",
        ["unrestricted double"] = "double",
        ["bigint"] = "long",
        ["undefined"] = "void",
        ["any"] = "object",
        ["object"] = "object",
        ["ArrayBuffer"] = "byte[]",
        ["SharedArrayBuffer"] = "byte[]",
        ["DataView"] = "byte[]",
        ["Int8Array"] = "sbyte[]",
        ["Int16Array"] = "short[]",
        ["Int32Array"] = "int[]",
        ["Uint8Array"] = "byte[]",
        ["Uint8ClampedArray"] = "byte[]",
        ["Uint16Array"] = "ushort[]",
        ["Uint32Array"] = "uint[]",
        ["Float32Array"] = "float[]",
        ["Float64Array"] = "double[]",
        ["BigInt64Array"] = "long[]",
        ["BigUint64Array"] = "ulong[]",
        ["WindowProxy"] = "object",
        ["BufferSource"] = "byte[]",
        ["DOMHighResTimeStamp"] = "double",
        ["EpochTimeStamp"] = "long",
        ["VibratePattern"] = "uint[]",
        ["AlgorithmIdentifier"] = "object",
        ["HashAlgorithmIdentifier"] = "object",
        ["BigInteger"] = "byte[]",
        ["CryptoKeyPair"] = "object",
        ["JsonWebKey"] = "object",
        ["ConstrainULong"] = "uint",
        ["ConstrainDouble"] = "double",
        ["ConstrainBoolean"] = "bool",
        ["ConstrainDOMString"] = "string",
        ["ClipboardItemData"] = "object",
        ["ClipboardItems"] = "object[]",
        ["FormDataEntryValue"] = "object",
        ["BlobPart"] = "object",
        ["BodyInit"] = "object",
        ["RequestInfo"] = "object",
        ["TimerHandler"] = "object",
        ["ImageBitmapSource"] = "object",
        ["OnErrorEventHandler"] = "object",
        ["OnBeforeUnloadEventHandler"] = "object",
        ["EventHandler"] = "object",
        ["MessageEventSource"] = "object",
        ["HTMLOrSVGScriptElement"] = "object",
        ["MediaProvider"] = "object",
        ["RenderingContext"] = "object",
        ["OffscreenRenderingContext"] = "object",
        ["CanvasImageSource"] = "object",
        ["CanvasFilterInput"] = "object",
        ["Transferable"] = "object",
        ["StructuredSerializeOptions"] = "object",
        ["ArrayBufferView"] = "byte[]",
    };

    private readonly IdlResolvedModel _model;

    public TypeMapper(IdlResolvedModel model) {
        _model = model;
    }

    public (string type, bool isAsync) MapType(IdlType idlType) {
        if (idlType.IsUnion) {
            // Union return/property → object with doc comment
            return ("object", false);
        }

        if (!string.IsNullOrEmpty(idlType.Generic)) {
            return MapGenericType(idlType);
        }

        var typeName = idlType.TypeName;
        if (typeName == null)
            return ("object", false);

        if (PrimitiveMap.TryGetValue(typeName, out var mapped))
            return (mapped, false);

        // Resolve typedefs to underlying types
        if (_model.Typedefs.TryGetValue(typeName, out var typedef)) {
            return MapType(typedef.Type);
        }

        // Check if it's a known type
        var csName = NamingConventions.ToPascalCase(typeName);

        // Rename types that conflict with System types
        csName = csName switch {
            "File" => "WebFile",
            "Range" => "DomRange",
            _ => csName
        };

        return (csName, false);
    }

    public string MapReturnType(IdlType idlType, out bool isAsync) {
        if (!string.IsNullOrEmpty(idlType.Generic) && idlType.Generic == "Promise") {
            isAsync = true;
            if (idlType.TypeArguments.Count == 1) {
                var inner = idlType.TypeArguments[0];
                if (inner.TypeName == "undefined") {
                    return "Task";
                }
                var (innerType, _) = MapType(inner);
                var nullable = inner.IsNullable ? "?" : "";
                return $"Task<{innerType}{nullable}>";
            }
            return "Task";
        }

        isAsync = false;
        var (result, _) = MapType(idlType);
        if (idlType.IsNullable && result != "object" && result != "void")
            result += "?";
        return result;
    }

    public string MapPropertyType(IdlType idlType) {
        var (result, _) = MapType(idlType);
        if (idlType.IsNullable && result != "object")
            result += "?";
        return result;
    }

    public string MapParameterType(IdlType idlType) {
        if (idlType.IsUnion) {
            return "object";
        }
        var (result, _) = MapType(idlType);
        if (idlType.IsNullable && result != "object")
            result += "?";
        return result;
    }

    private (string type, bool isAsync) MapGenericType(IdlType idlType) {
        switch (idlType.Generic) {
            case "Promise":
                if (idlType.TypeArguments.Count == 1) {
                    var inner = idlType.TypeArguments[0];
                    if (inner.TypeName == "undefined")
                        return ("Task", true);
                    var (innerType, _) = MapType(inner);
                    var nullable = inner.IsNullable ? "?" : "";
                    return ($"Task<{innerType}{nullable}>", true);
                }
                return ("Task", true);

            case "sequence" or "FrozenArray" or "ObservableArray":
                if (idlType.TypeArguments.Count == 1) {
                    var (innerType, _) = MapType(idlType.TypeArguments[0]);
                    return ($"IReadOnlyList<{innerType}>", false);
                }
                return ("IReadOnlyList<object>", false);

            case "record":
                if (idlType.TypeArguments.Count == 2) {
                    var (keyType, _) = MapType(idlType.TypeArguments[0]);
                    var (valType, _2) = MapType(idlType.TypeArguments[1]);
                    return ($"IReadOnlyDictionary<{keyType}, {valType}>", false);
                }
                return ("IReadOnlyDictionary<string, object>", false);

            default:
                if (idlType.TypeArguments.Count == 1) {
                    var (innerType, _) = MapType(idlType.TypeArguments[0]);
                    return ($"{NamingConventions.ToPascalCase(idlType.Generic!)}<{innerType}>", false);
                }
                return (NamingConventions.ToPascalCase(idlType.Generic!), false);
        }
    }
}
