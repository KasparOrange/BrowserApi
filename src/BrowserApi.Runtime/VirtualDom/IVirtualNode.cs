namespace BrowserApi.Runtime.VirtualDom;

/// <summary>
/// Defines the contract for a virtual DOM node that can be accessed and manipulated through
/// JavaScript-style property and method names.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IVirtualNode"/> is the foundation of the server-side virtual DOM used by
/// <see cref="JintBackend"/>. Each virtual node responds to JavaScript property reads, writes,
/// and method calls using the same names that a real browser DOM would expose (e.g.,
/// <c>"textContent"</c>, <c>"appendChild"</c>, <c>"querySelector"</c>).
/// </para>
/// <para>
/// This interface is implemented by <see cref="VirtualNode"/>, <see cref="VirtualElement"/>,
/// <see cref="VirtualTextNode"/>, <see cref="VirtualStyle"/>, and <see cref="VirtualConsole"/>.
/// </para>
/// </remarks>
/// <seealso cref="VirtualNode"/>
/// <seealso cref="JintBackend"/>
public interface IVirtualNode {
    /// <summary>
    /// Gets the value of a JavaScript property by its JS-side name.
    /// </summary>
    /// <param name="jsName">
    /// The JavaScript property name (e.g., <c>"textContent"</c>, <c>"className"</c>, <c>"id"</c>).
    /// </param>
    /// <returns>The property value, or <see langword="null"/> if the property is not recognized.</returns>
    object? GetJsProperty(string jsName);

    /// <summary>
    /// Sets the value of a JavaScript property by its JS-side name.
    /// </summary>
    /// <param name="jsName">
    /// The JavaScript property name (e.g., <c>"textContent"</c>, <c>"className"</c>).
    /// </param>
    /// <param name="value">The value to assign to the property.</param>
    void SetJsProperty(string jsName, object? value);

    /// <summary>
    /// Invokes a JavaScript method by its JS-side name with the given arguments.
    /// </summary>
    /// <param name="jsName">
    /// The JavaScript method name (e.g., <c>"appendChild"</c>, <c>"querySelector"</c>).
    /// </param>
    /// <param name="args">The arguments to pass to the method.</param>
    /// <returns>The method's return value, or <see langword="null"/> if not applicable.</returns>
    object? InvokeJsMethod(string jsName, object?[] args);
}
