namespace BrowserApi.Common;

/// <summary>
/// Collects multiple void interop operations (property sets and method calls) and
/// executes them in a single round-trip to JavaScript for improved performance.
/// </summary>
/// <remarks>
/// <para>
/// Each individual <see cref="JsObject"/> property set or void method call normally
/// requires a separate interop call. When you need to perform many operations at once
/// (e.g., setting multiple CSS properties on an element, or calling several void methods
/// in sequence), <see cref="JsBatch"/> dramatically reduces overhead by bundling all
/// operations into one call to the JavaScript <c>browserApi.batch</c> function.
/// </para>
/// <para>
/// <b>Limitations:</b> Only void operations are supported. Operations that return a
/// value cannot be batched because the batch executes as a single async call and
/// does not relay individual return values.
/// </para>
/// <para>
/// The batch deduplicates target objects internally: if multiple operations target the
/// same <see cref="JsObject"/>, the handle is sent to JavaScript only once.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Using the static RunAsync helper:
/// await JsBatch.RunAsync(batch => {
///     batch.SetProperty(element, "textContent", "Hello");
///     batch.SetProperty(element, "className", "active");
///     batch.InvokeVoid(element, "focus");
/// });
///
/// // Using an instance directly:
/// var batch = new JsBatch();
/// batch.SetProperty(elementA, "hidden", true);
/// batch.SetProperty(elementB, "hidden", false);
/// await batch.ExecuteAsync();
/// </code>
/// </example>
/// <seealso cref="JsBatchScope"/>
/// <seealso cref="JsObjectBatchExtensions"/>
/// <seealso cref="JsObject"/>
public sealed class JsBatch {
    private readonly Dictionary<JsHandle, int> _targetIndices = new();
    private readonly List<object?> _targetHandles = [];
    private readonly List<object> _commands = [];

    /// <summary>
    /// Gets the number of commands currently queued in this batch.
    /// </summary>
    /// <value>
    /// The count of pending operations (property sets and void method calls) that will
    /// be sent to JavaScript when <see cref="ExecuteAsync"/> is called.
    /// </value>
    public int Count => _commands.Count;

    /// <summary>
    /// Queues a property assignment on the specified JavaScript object.
    /// </summary>
    /// <param name="target">
    /// The <see cref="JsObject"/> whose JavaScript property should be set.
    /// </param>
    /// <param name="name">The JavaScript property name (camelCase).</param>
    /// <param name="value">
    /// The value to assign. Automatically converted via <see cref="JsObject.ConvertToJs"/>.
    /// </param>
    public void SetProperty(JsObject target, string name, object? value) {
        var idx = GetOrAddTarget(target.Handle);
        _commands.Add(new BatchCommand(idx, 0, name, JsObject.ConvertToJs(value), null));
    }

    /// <summary>
    /// Queues a void method invocation on the specified JavaScript object.
    /// </summary>
    /// <param name="target">
    /// The <see cref="JsObject"/> on which to invoke the method.
    /// </param>
    /// <param name="name">The JavaScript method name (camelCase).</param>
    /// <param name="args">
    /// The arguments to pass to the method. Each argument is automatically converted
    /// via <see cref="JsObject.ConvertToJs"/>.
    /// </param>
    public void InvokeVoid(JsObject target, string name, params object?[] args) {
        var idx = GetOrAddTarget(target.Handle);
        var converted = new object?[args.Length];
        for (var i = 0; i < args.Length; i++)
            converted[i] = JsObject.ConvertToJs(args[i]);
        _commands.Add(new BatchCommand(idx, 1, name, null, converted));
    }

    /// <summary>
    /// Sends all queued commands to JavaScript in a single interop call, then clears the batch.
    /// </summary>
    /// <returns>A task that completes when all commands have been executed in JavaScript.</returns>
    /// <remarks>
    /// <para>
    /// If the batch is empty (no commands have been queued), this method returns immediately
    /// without making an interop call.
    /// </para>
    /// <para>
    /// After execution, the batch is cleared and can be reused for a new set of operations.
    /// </para>
    /// </remarks>
    public async Task ExecuteAsync() {
        if (_commands.Count == 0) return;

        var browserApiHandle = JsObject.Backend.GetGlobal("browserApi");
        var targets = _targetHandles.ToArray();
        var commands = _commands.ToArray();

        await JsObject.Backend.InvokeVoidAsync(browserApiHandle, "batch", [targets, commands]);

        _commands.Clear();
        _targetIndices.Clear();
        _targetHandles.Clear();
    }

    /// <summary>
    /// Creates a new <see cref="JsBatch"/>, populates it using the specified action,
    /// and immediately executes all queued commands.
    /// </summary>
    /// <param name="action">
    /// A delegate that receives the batch and should call <see cref="SetProperty"/>
    /// and/or <see cref="InvokeVoid"/> to queue operations.
    /// </param>
    /// <returns>A task that completes when all commands have been executed in JavaScript.</returns>
    /// <example>
    /// <code>
    /// await JsBatch.RunAsync(batch => {
    ///     batch.SetProperty(element, "textContent", "Updated");
    ///     batch.InvokeVoid(element, "scrollIntoView");
    /// });
    /// </code>
    /// </example>
    public static async Task RunAsync(System.Action<JsBatch> action) {
        var batch = new JsBatch();
        action(batch);
        await batch.ExecuteAsync();
    }

    /// <summary>
    /// Returns the index for the given handle in the target list, adding it if it has not
    /// been seen before. This deduplicates targets so each JavaScript object reference
    /// is transmitted only once per batch.
    /// </summary>
    /// <param name="handle">The handle to look up or register.</param>
    /// <returns>The zero-based index of the handle in the targets array.</returns>
    private int GetOrAddTarget(JsHandle handle) {
        if (_targetIndices.TryGetValue(handle, out var idx))
            return idx;
        idx = _targetHandles.Count;
        _targetIndices[handle] = idx;
        _targetHandles.Add(handle.Value);
        return idx;
    }

    /// <summary>
    /// Internal record representing a single batched command to be executed in JavaScript.
    /// </summary>
    /// <param name="t">The index into the targets array identifying the target object.</param>
    /// <param name="o">The operation type: 0 for property set, 1 for method invocation.</param>
    /// <param name="n">The property or method name.</param>
    /// <param name="v">The value to set (for property set operations); <see langword="null"/> for method calls.</param>
    /// <param name="a">The arguments array (for method invocations); <see langword="null"/> for property sets.</param>
    internal sealed record BatchCommand(int t, int o, string n, object? v, object?[]? a);
}
