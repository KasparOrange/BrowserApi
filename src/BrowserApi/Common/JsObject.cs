using System.Reflection;

namespace BrowserApi.Common;

/// <summary>
/// Abstract base class for all generated browser API wrapper types. Provides the bridge
/// between strongly-typed C# properties/methods and their JavaScript counterparts.
/// </summary>
/// <remarks>
/// <para>
/// Every generated class (e.g., <c>Document</c>, <c>Element</c>, <c>HTMLCanvasElement</c>)
/// inherits from <see cref="JsObject"/>. Each instance holds a <see cref="JsHandle"/> that
/// references the corresponding JavaScript object, and delegates all property access and
/// method invocation to the configured <see cref="Backend"/>.
/// </para>
/// <para>
/// <b>Backend configuration:</b> Before using any <see cref="JsObject"/>-derived type,
/// you must assign a concrete <see cref="IBrowserBackend"/> implementation to the static
/// <see cref="Backend"/> property. In a Blazor application, this is typically done by the
/// <c>BrowserApiComponentBase</c> on first render; in tests, assign a mock or fake backend.
/// </para>
/// <para>
/// <b>Automatic type conversion:</b> The <see cref="ConvertToJs"/> and
/// <see cref="ConvertFromJs{T}"/> methods handle seamless conversion between .NET and
/// JavaScript representations:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="JsObject"/> instances are unwrapped to their <see cref="Handle"/>.</description></item>
///   <item><description><see cref="ICssValue"/> instances are serialized via <see cref="ICssValue.ToCss"/>.</description></item>
///   <item><description><see cref="IWebIdlSerializable"/> instances are serialized via <see cref="IWebIdlSerializable.ToJs"/>.</description></item>
///   <item><description>Enum values are mapped to/from their <see cref="StringValueAttribute"/> strings.</description></item>
///   <item><description>Primitives and other types pass through unchanged.</description></item>
/// </list>
/// <para>
/// <b>Disposal:</b> <see cref="JsObject"/> implements both <see cref="IDisposable"/> and
/// <see cref="IAsyncDisposable"/>. Disposing releases the underlying JavaScript object
/// reference so the browser's garbage collector can reclaim it. Prefer
/// <see cref="DisposeAsync"/> in async contexts.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Generated code (simplified):
/// public partial class Document : JsObject {
///     public Element? GetElementById(string id) =>
///         Invoke&lt;Element?&gt;("getElementById", id);
///
///     public string Title {
///         get => GetProperty&lt;string&gt;("title");
///         set => SetProperty("title", value);
///     }
/// }
///
/// // Usage:
/// JsObject.Backend = new JSInteropBackend(jsRuntime);
/// var doc = new Document { Handle = backend.GetGlobal("document") };
/// doc.Title = "Hello, BrowserApi!";
/// </code>
/// </example>
/// <seealso cref="IBrowserBackend"/>
/// <seealso cref="JsHandle"/>
/// <seealso cref="JsBatch"/>
public abstract class JsObject : IDisposable, IAsyncDisposable {
    private static IBrowserBackend? _backend;

    /// <summary>
    /// Gets or sets the global <see cref="IBrowserBackend"/> used by all <see cref="JsObject"/>
    /// instances to communicate with JavaScript.
    /// </summary>
    /// <value>The currently configured backend implementation.</value>
    /// <exception cref="InvalidOperationException">
    /// Thrown when getting this property if no backend has been configured.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is a static property shared across the entire application. It must be set exactly
    /// once during application startup, before any <see cref="JsObject"/> methods are called.
    /// </para>
    /// <para>
    /// In Blazor apps, <c>BrowserApiComponentBase</c> sets this automatically on first render.
    /// In tests, set this to a mock or fake implementation before exercising any API types.
    /// </para>
    /// </remarks>
    public static IBrowserBackend Backend {
        get => _backend ?? throw new InvalidOperationException("BrowserApi backend has not been configured. Call JsObject.Backend = ... during app startup.");
        set => _backend = value;
    }

    /// <summary>
    /// Gets or sets the <see cref="JsHandle"/> that references the underlying JavaScript object.
    /// </summary>
    /// <value>
    /// The opaque handle used by the <see cref="IBrowserBackend"/> to identify the
    /// corresponding JavaScript object.
    /// </value>
    /// <remarks>
    /// This property is settable internally so that factory methods, deserialization logic,
    /// and the <see cref="ConvertFromJs{T}"/> method can assign the handle after construction.
    /// </remarks>
    public JsHandle Handle { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsObject"/> class with an empty handle.
    /// </summary>
    /// <remarks>
    /// The <see cref="Handle"/> must be assigned before calling any interop methods.
    /// This constructor is used by the <see cref="ConvertFromJs{T}"/> method and by
    /// generated code that defers handle assignment.
    /// </remarks>
    protected JsObject() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsObject"/> class with the specified handle.
    /// </summary>
    /// <param name="handle">
    /// The <see cref="JsHandle"/> referencing the JavaScript object this instance wraps.
    /// </param>
    protected JsObject(JsHandle handle) {
        Handle = handle;
    }

    /// <summary>
    /// Synchronously reads a property from the underlying JavaScript object and converts
    /// the result to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected .NET type of the property value.</typeparam>
    /// <param name="jsName">The JavaScript property name (camelCase).</param>
    /// <returns>The property value, converted to <typeparamref name="T"/>.</returns>
    /// <remarks>
    /// Generated property getters call this method, passing the original JavaScript name
    /// (typically stored in a <see cref="JsNameAttribute"/>).
    /// </remarks>
    protected T GetProperty<T>(string jsName) {
        var raw = Backend.GetProperty<object?>(Handle, jsName);
        return ConvertFromJs<T>(raw);
    }

    /// <summary>
    /// Synchronously sets a property on the underlying JavaScript object, converting
    /// the value to its JavaScript representation first.
    /// </summary>
    /// <param name="jsName">The JavaScript property name (camelCase).</param>
    /// <param name="value">
    /// The value to assign. <see cref="JsObject"/>, <see cref="ICssValue"/>,
    /// <see cref="IWebIdlSerializable"/>, and enum values are automatically converted.
    /// </param>
    /// <remarks>
    /// Generated property setters call this method, passing the original JavaScript name.
    /// </remarks>
    protected void SetProperty(string jsName, object? value) {
        Backend.SetProperty(Handle, jsName, ConvertToJs(value));
    }

    /// <summary>
    /// Synchronously invokes a void method on the underlying JavaScript object.
    /// </summary>
    /// <param name="jsName">The JavaScript method name (camelCase).</param>
    /// <param name="args">
    /// The arguments to pass. Each argument is converted via <see cref="ConvertToJs"/> before dispatch.
    /// </param>
    protected void InvokeVoid(string jsName, params object?[] args) {
        Backend.InvokeVoid(Handle, jsName, ConvertArgs(args));
    }

    /// <summary>
    /// Synchronously invokes a method on the underlying JavaScript object and returns
    /// the result, converted to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected .NET type of the return value.</typeparam>
    /// <param name="jsName">The JavaScript method name (camelCase).</param>
    /// <param name="args">
    /// The arguments to pass. Each argument is converted via <see cref="ConvertToJs"/> before dispatch.
    /// </param>
    /// <returns>The method's return value, converted to <typeparamref name="T"/>.</returns>
    protected T Invoke<T>(string jsName, params object?[] args) {
        var raw = Backend.Invoke<object?>(Handle, jsName, ConvertArgs(args));
        return ConvertFromJs<T>(raw);
    }

    /// <summary>
    /// Asynchronously invokes a void method on the underlying JavaScript object.
    /// </summary>
    /// <param name="jsName">The JavaScript method name (camelCase).</param>
    /// <param name="args">
    /// The arguments to pass. Each argument is converted via <see cref="ConvertToJs"/> before dispatch.
    /// </param>
    /// <returns>A task that completes when the JavaScript method has finished executing.</returns>
    protected Task InvokeVoidAsync(string jsName, params object?[] args) {
        return Backend.InvokeVoidAsync(Handle, jsName, ConvertArgs(args));
    }

    /// <summary>
    /// Asynchronously invokes a method on the underlying JavaScript object and returns
    /// the result, converted to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected .NET type of the return value.</typeparam>
    /// <param name="jsName">The JavaScript method name (camelCase).</param>
    /// <param name="args">
    /// The arguments to pass. Each argument is converted via <see cref="ConvertToJs"/> before dispatch.
    /// </param>
    /// <returns>
    /// A task whose result is the method's return value, converted to <typeparamref name="T"/>.
    /// </returns>
    protected async Task<T> InvokeAsync<T>(string jsName, params object?[] args) {
        var raw = await Backend.InvokeAsync<object?>(Handle, jsName, ConvertArgs(args));
        return ConvertFromJs<T>(raw);
    }

    /// <summary>
    /// Converts an array of .NET arguments to their JavaScript representations.
    /// </summary>
    /// <param name="args">The arguments to convert.</param>
    /// <returns>A new array with each element converted via <see cref="ConvertToJs"/>.</returns>
    private static object?[] ConvertArgs(object?[] args) {
        var converted = new object?[args.Length];
        for (var i = 0; i < args.Length; i++)
            converted[i] = ConvertToJs(args[i]);
        return converted;
    }

    /// <summary>
    /// Converts a single .NET value to its JavaScript-compatible representation.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>
    /// The JavaScript-compatible form of the value:
    /// <list type="bullet">
    ///   <item><description><see langword="null"/> passes through as <see langword="null"/>.</description></item>
    ///   <item><description><see cref="JsObject"/> is unwrapped to its <see cref="Handle"/>.</description></item>
    ///   <item><description><see cref="ICssValue"/> is serialized to its CSS string via <see cref="ICssValue.ToCss"/>.</description></item>
    ///   <item><description><see cref="IWebIdlSerializable"/> is serialized via <see cref="IWebIdlSerializable.ToJs"/>.</description></item>
    ///   <item><description>Enum values are mapped to their <see cref="StringValueAttribute"/> string, if present.</description></item>
    ///   <item><description>All other values pass through unchanged.</description></item>
    /// </list>
    /// </returns>
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

    /// <summary>
    /// Converts a raw JavaScript value to the specified .NET type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target .NET type.</typeparam>
    /// <param name="raw">The raw value received from JavaScript.</param>
    /// <returns>
    /// The value converted to <typeparamref name="T"/>. Returns <see langword="default"/>
    /// if <paramref name="raw"/> is <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method handles several conversion scenarios:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <b>JsObject subclasses:</b> Creates a new instance of <typeparamref name="T"/>
    ///       and assigns the handle from the raw value.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Enums:</b> If the raw value is a string, matches it against
    ///       <see cref="StringValueAttribute"/> values; otherwise, uses numeric conversion.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>IConvertible types:</b> Uses <see cref="Convert.ChangeType"/> for numeric
    ///       and other primitive conversions.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Everything else:</b> Direct cast to <typeparamref name="T"/>.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
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

    /// <summary>
    /// Retrieves the <see cref="StringValueAttribute.Value"/> for the specified enum value,
    /// or <see langword="null"/> if the field is not decorated with <see cref="StringValueAttribute"/>.
    /// </summary>
    /// <param name="e">The enum value to inspect.</param>
    /// <returns>The string value, or <see langword="null"/>.</returns>
    private static string? GetStringValue(Enum e) {
        var member = e.GetType().GetField(e.ToString());
        var attr = member?.GetCustomAttribute<StringValueAttribute>();
        return attr?.Value;
    }

    /// <summary>
    /// Parses a JavaScript string value back to the corresponding enum member by matching
    /// against <see cref="StringValueAttribute"/> values, falling back to case-insensitive
    /// name parsing.
    /// </summary>
    /// <param name="enumType">The target enum type.</param>
    /// <param name="value">The string value from JavaScript.</param>
    /// <returns>The matching enum value.</returns>
    private static object ParseEnumFromString(Type enumType, string value) {
        foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static)) {
            var attr = field.GetCustomAttribute<StringValueAttribute>();
            if (attr?.Value == value)
                return field.GetValue(null)!;
        }
        return Enum.Parse(enumType, value, ignoreCase: true);
    }

    /// <summary>
    /// Releases the JavaScript object reference held by this instance.
    /// </summary>
    /// <remarks>
    /// This calls <see cref="Dispose(bool)"/> and suppresses finalization.
    /// Prefer <see cref="DisposeAsync"/> in async contexts to avoid blocking.
    /// </remarks>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the JavaScript object reference if <paramref name="disposing"/> is
    /// <see langword="true"/> and the handle is not empty.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> if called from <see cref="Dispose()"/>;
    /// <see langword="false"/> if called from a finalizer.
    /// </param>
    /// <remarks>
    /// <para>
    /// This method synchronously waits for the async handle disposal to complete.
    /// Override this method in derived classes to add custom cleanup logic, but
    /// always call the base implementation.
    /// </para>
    /// </remarks>
    protected virtual void Dispose(bool disposing) {
        if (disposing && !Handle.IsEmpty)
            Backend.DisposeHandle(Handle).AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously releases the JavaScript object reference held by this instance.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that completes when the handle has been released.</returns>
    /// <remarks>
    /// This is the preferred disposal method in async contexts (e.g., inside
    /// <c>await using</c> blocks) because it avoids blocking the thread.
    /// </remarks>
    public async ValueTask DisposeAsync() {
        if (!Handle.IsEmpty)
            await Backend.DisposeHandle(Handle);
        GC.SuppressFinalize(this);
    }
}
