using System.Reflection;

namespace BrowserApi.Common;

public abstract class JsObject : IDisposable, IAsyncDisposable {
    private static IBrowserBackend? _backend;

    public static IBrowserBackend Backend {
        get => _backend ?? throw new InvalidOperationException("BrowserApi backend has not been configured. Call JsObject.Backend = ... during app startup.");
        set => _backend = value;
    }

    public JsHandle Handle { get; internal set; }

    protected JsObject() { }

    protected JsObject(JsHandle handle) {
        Handle = handle;
    }

    protected T GetProperty<T>(string jsName) {
        var raw = Backend.GetProperty<object?>(Handle, jsName);
        return ConvertFromJs<T>(raw);
    }

    protected void SetProperty(string jsName, object? value) {
        Backend.SetProperty(Handle, jsName, ConvertToJs(value));
    }

    protected void InvokeVoid(string jsName, params object?[] args) {
        Backend.InvokeVoid(Handle, jsName, ConvertArgs(args));
    }

    protected T Invoke<T>(string jsName, params object?[] args) {
        var raw = Backend.Invoke<object?>(Handle, jsName, ConvertArgs(args));
        return ConvertFromJs<T>(raw);
    }

    protected Task InvokeVoidAsync(string jsName, params object?[] args) {
        return Backend.InvokeVoidAsync(Handle, jsName, ConvertArgs(args));
    }

    protected async Task<T> InvokeAsync<T>(string jsName, params object?[] args) {
        var raw = await Backend.InvokeAsync<object?>(Handle, jsName, ConvertArgs(args));
        return ConvertFromJs<T>(raw);
    }

    private static object?[] ConvertArgs(object?[] args) {
        var converted = new object?[args.Length];
        for (var i = 0; i < args.Length; i++)
            converted[i] = ConvertToJs(args[i]);
        return converted;
    }

    internal static object? ConvertToJs(object? value) {
        return value switch {
            null => null,
            JsObject obj => obj.Handle,
            ICssValue css => css.ToCss(),
            IWebIdlSerializable s => s.ToJs(),
            Enum e => GetStringValue(e) ?? value,
            _ => value
        };
    }

    internal static T ConvertFromJs<T>(object? raw) {
        if (raw is null)
            return default!;

        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (typeof(JsObject).IsAssignableFrom(targetType)) {
            var handle = raw is JsHandle h ? h : new JsHandle(raw);
            var instance = (JsObject)Activator.CreateInstance(targetType)!;
            instance.Handle = handle;
            return (T)(object)instance;
        }

        if (targetType.IsEnum) {
            if (raw is string s)
                return (T)ParseEnumFromString(targetType, s);
            return (T)Convert.ChangeType(raw, targetType);
        }

        if (raw is IConvertible && typeof(IConvertible).IsAssignableFrom(targetType))
            return (T)Convert.ChangeType(raw, targetType);

        return (T)raw;
    }

    private static string? GetStringValue(Enum e) {
        var member = e.GetType().GetField(e.ToString());
        var attr = member?.GetCustomAttribute<StringValueAttribute>();
        return attr?.Value;
    }

    private static object ParseEnumFromString(Type enumType, string value) {
        foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static)) {
            var attr = field.GetCustomAttribute<StringValueAttribute>();
            if (attr?.Value == value)
                return field.GetValue(null)!;
        }
        return Enum.Parse(enumType, value, ignoreCase: true);
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (disposing && !Handle.IsEmpty)
            Backend.DisposeHandle(Handle).AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync() {
        if (!Handle.IsEmpty)
            await Backend.DisposeHandle(Handle);
        GC.SuppressFinalize(this);
    }
}
