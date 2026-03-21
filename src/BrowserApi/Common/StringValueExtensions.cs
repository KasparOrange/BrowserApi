using System.Reflection;

namespace BrowserApi.Common;

public static class StringValueExtensions {
    public static string ToStringValue<TEnum>(this TEnum value) where TEnum : struct, Enum {
        var member = typeof(TEnum).GetField(value.ToString()!);
        var attr = member?.GetCustomAttribute<StringValueAttribute>();
        return attr?.Value ?? value.ToString()!;
    }
}
