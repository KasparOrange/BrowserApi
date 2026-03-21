using BrowserApi.Generator.CSharpModel;
using BrowserApi.Generator.Emit;
using BrowserApi.Generator.Transform;

namespace BrowserApi.Generator.Css;

public static class CssEnumEmitter {
    public static CSharpEnum? TryCreate(CssPropertyDefinition prop) {
        if (!CssValueTypeMapper.IsPureKeywordGrammar(prop.ValueGrammar))
            return null;

        var keywords = prop.ValueGrammar
            .Split('|', StringSplitOptions.TrimEntries)
            .Where(k => !string.IsNullOrEmpty(k))
            .ToList();

        if (keywords.Count < 2)
            return null;

        return new CSharpEnum {
            Name = NamingConventions.ToPascalCase(prop.Name),
            Namespace = "BrowserApi.Css",
            Members = keywords.Select(k => new CSharpEnumMember {
                Name = NamingConventions.ToEnumMemberName(k),
                StringValue = k
            }).ToList()
        };
    }

    public static string? TryEmit(CssPropertyDefinition prop) {
        var csEnum = TryCreate(prop);
        return csEnum != null ? EnumEmitter.Emit(csEnum) : null;
    }
}
