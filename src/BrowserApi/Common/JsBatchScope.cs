namespace BrowserApi.Common;

/// <summary>
/// Provides a fluent interface for queuing batched operations against a single
/// <see cref="JsObject"/> target within a <see cref="JsBatch"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="JsBatchScope"/> is a convenience wrapper around <see cref="JsBatch"/> that
/// binds a specific <see cref="JsObject"/> target, allowing you to chain multiple property
/// sets and method calls without repeating the target argument. It is typically created
/// by <see cref="JsObjectBatchExtensions.BatchAsync"/> rather than instantiated directly.
/// </para>
/// <para>
/// All methods return <see langword="this"/> to support fluent chaining:
/// </para>
/// </remarks>
/// <example>
/// <code>
/// await element.BatchAsync(scope => scope
///     .Set("textContent", "Hello")
///     .Set("className", "highlight")
///     .Call("scrollIntoView")
/// );
/// </code>
/// </example>
/// <seealso cref="JsBatch"/>
/// <seealso cref="JsObjectBatchExtensions"/>
/// <seealso cref="JsObject"/>
public sealed class JsBatchScope {
    private readonly JsBatch _batch;
    private readonly JsObject _target;

    /// <summary>
    /// Initializes a new <see cref="JsBatchScope"/> that queues operations on the specified
    /// target within the given batch.
    /// </summary>
    /// <param name="batch">The <see cref="JsBatch"/> to queue commands into.</param>
    /// <param name="target">The <see cref="JsObject"/> that all operations in this scope will target.</param>
    internal JsBatchScope(JsBatch batch, JsObject target) {
        _batch = batch;
        _target = target;
    }

    /// <summary>
    /// Queues a property assignment on the scoped target object.
    /// </summary>
    /// <param name="name">The JavaScript property name (camelCase).</param>
    /// <param name="value">
    /// The value to assign. Automatically converted via <see cref="JsObject.ConvertToJs"/>.
    /// </param>
    /// <returns>This <see cref="JsBatchScope"/> instance, for fluent chaining.</returns>
    public JsBatchScope Set(string name, object? value) {
        _batch.SetProperty(_target, name, value);
        return this;
    }

    /// <summary>
    /// Queues a void method invocation on the scoped target object.
    /// </summary>
    /// <param name="name">The JavaScript method name (camelCase).</param>
    /// <param name="args">
    /// The arguments to pass to the method. Each argument is automatically converted
    /// via <see cref="JsObject.ConvertToJs"/>.
    /// </param>
    /// <returns>This <see cref="JsBatchScope"/> instance, for fluent chaining.</returns>
    public JsBatchScope Call(string name, params object?[] args) {
        _batch.InvokeVoid(_target, name, args);
        return this;
    }
}
