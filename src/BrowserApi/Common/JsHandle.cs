namespace BrowserApi.Common;

/// <summary>
/// An opaque, lightweight handle that represents a reference to a JavaScript object
/// on the other side of the interop boundary.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="JsHandle"/> is the core currency of the BrowserApi interop system. Every
/// <see cref="JsObject"/> holds a <see cref="JsHandle"/> that the <see cref="IBrowserBackend"/>
/// uses to locate the corresponding JavaScript object when getting/setting properties
/// or invoking methods.
/// </para>
/// <para>
/// The <see cref="Value"/> is intentionally typed as <see cref="object"/>. Its concrete type
/// depends on the backend implementation. For example, the Blazor WebAssembly backend
/// stores an <c>IJSObjectReference</c> here, while a test backend might store a simple
/// integer identifier. Consumer code should treat <see cref="JsHandle"/> as opaque and
/// never inspect or cast <see cref="Value"/> directly.
/// </para>
/// <para>
/// Equality is based on reference identity (<see cref="object.ReferenceEquals"/>), not
/// structural equality, because two different .NET references to the same JS object
/// should be considered the same handle.
/// </para>
/// </remarks>
/// <seealso cref="JsObject"/>
/// <seealso cref="IBrowserBackend"/>
public readonly struct JsHandle : IEquatable<JsHandle> {
    /// <summary>
    /// Gets the underlying backend-specific reference to the JavaScript object.
    /// </summary>
    /// <remarks>
    /// This property is internal because consumer code should never need to access
    /// the raw value. It is used by <see cref="IBrowserBackend"/> implementations
    /// to resolve the JavaScript object reference.
    /// </remarks>
    internal object? Value { get; }

    /// <summary>
    /// Initializes a new <see cref="JsHandle"/> wrapping the specified backend-specific reference.
    /// </summary>
    /// <param name="value">
    /// The backend-specific object reference (e.g., an <c>IJSObjectReference</c> in Blazor).
    /// Pass <see langword="null"/> to create an empty handle.
    /// </param>
    public JsHandle(object? value) => Value = value;

    /// <summary>
    /// Gets a value indicating whether this handle has no associated JavaScript object.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="Value"/> is <see langword="null"/>;
    /// otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// An empty handle typically indicates that the <see cref="JsObject"/> has not yet been
    /// bound to a JavaScript object, or that it has been disposed.
    /// </remarks>
    public bool IsEmpty => Value is null;

    /// <summary>
    /// Determines whether the specified <see cref="JsHandle"/> refers to the same
    /// JavaScript object as this handle, using reference equality.
    /// </summary>
    /// <param name="other">The other handle to compare against.</param>
    /// <returns>
    /// <see langword="true"/> if both handles wrap the same object reference;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(JsHandle other) => ReferenceEquals(Value, other.Value);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is JsHandle other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value?.GetHashCode() ?? 0;

    /// <summary>
    /// Determines whether two <see cref="JsHandle"/> instances refer to the same JavaScript object.
    /// </summary>
    /// <param name="left">The first handle.</param>
    /// <param name="right">The second handle.</param>
    /// <returns>
    /// <see langword="true"/> if both handles refer to the same object; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator ==(JsHandle left, JsHandle right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="JsHandle"/> instances refer to different JavaScript objects.
    /// </summary>
    /// <param name="left">The first handle.</param>
    /// <param name="right">The second handle.</param>
    /// <returns>
    /// <see langword="true"/> if the handles refer to different objects; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator !=(JsHandle left, JsHandle right) => !left.Equals(right);
}
