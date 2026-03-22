namespace BrowserApi.Common;

/// <summary>
/// Provides extension methods for performing batched interop operations on a single
/// <see cref="JsObject"/> target.
/// </summary>
/// <remarks>
/// These extensions create a <see cref="JsBatch"/> and <see cref="JsBatchScope"/>
/// behind the scenes, providing a concise API for the common case of batching
/// multiple operations against one object.
/// </remarks>
/// <seealso cref="JsBatch"/>
/// <seealso cref="JsBatchScope"/>
/// <seealso cref="JsObjectBulkExtensions"/>
public static class JsObjectBatchExtensions {
    /// <summary>
    /// Batches multiple void operations (property sets and method calls) on this
    /// <see cref="JsObject"/> and executes them in a single interop round-trip.
    /// </summary>
    /// <param name="target">The JavaScript object to operate on.</param>
    /// <param name="action">
    /// A delegate that receives a <see cref="JsBatchScope"/> bound to <paramref name="target"/>.
    /// Use <see cref="JsBatchScope.Set"/> and <see cref="JsBatchScope.Call"/> to queue operations.
    /// </param>
    /// <returns>A task that completes when all batched operations have been executed in JavaScript.</returns>
    /// <example>
    /// <code>
    /// await element.BatchAsync(scope => scope
    ///     .Set("textContent", "Hello, world!")
    ///     .Set("className", "active")
    ///     .Call("focus")
    /// );
    /// // All three operations are sent to JavaScript in a single interop call.
    /// </code>
    /// </example>
    public static async Task BatchAsync(this JsObject target, System.Action<JsBatchScope> action) {
        var batch = new JsBatch();
        var scope = new JsBatchScope(batch, target);
        action(scope);
        await batch.ExecuteAsync();
    }
}
