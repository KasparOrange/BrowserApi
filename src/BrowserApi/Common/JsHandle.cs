namespace BrowserApi.Common;

public readonly struct JsHandle : IEquatable<JsHandle> {
    internal object? Value { get; }

    public JsHandle(object? value) => Value = value;

    public bool IsEmpty => Value is null;

    public bool Equals(JsHandle other) => ReferenceEquals(Value, other.Value);

    public override bool Equals(object? obj) => obj is JsHandle other && Equals(other);

    public override int GetHashCode() => Value?.GetHashCode() ?? 0;

    public static bool operator ==(JsHandle left, JsHandle right) => left.Equals(right);

    public static bool operator !=(JsHandle left, JsHandle right) => !left.Equals(right);
}
