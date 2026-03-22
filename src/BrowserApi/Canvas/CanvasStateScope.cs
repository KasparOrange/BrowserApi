using BrowserApi.Dom;

namespace BrowserApi.Canvas;

/// <summary>
/// A disposable scope that automatically saves and restores the canvas 2D rendering context state.
/// </summary>
/// <remarks>
/// <para>
/// When constructed, <see cref="CanvasStateScope"/> calls
/// <see cref="CanvasRenderingContext2D.Save"/> on the context. When disposed, it calls
/// <see cref="CanvasRenderingContext2D.Restore"/>, ensuring that any transformations,
/// clipping regions, or style changes made within the scope are reverted.
/// </para>
/// <para>
/// Use <see cref="CanvasExtensions.SaveState"/> to create an instance of this type.
/// This pattern prevents mismatched <c>Save</c>/<c>Restore</c> calls, which can corrupt
/// the canvas state stack.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var ctx = canvas.GetContext2D();
///
/// using (ctx.SaveState()) {
///     ctx.Translate(50, 50);
///     ctx.Rotate(Math.PI / 4);
///     ctx.FillRect(0, 0, 100, 100);
/// }
/// // Context state is automatically restored here -- translation and rotation are undone.
/// </code>
/// </example>
/// <seealso cref="CanvasExtensions.SaveState"/>
/// <seealso cref="CanvasRenderingContext2D"/>
public readonly struct CanvasStateScope : IDisposable {
    private readonly CanvasRenderingContext2D _ctx;

    internal CanvasStateScope(CanvasRenderingContext2D ctx) {
        _ctx = ctx;
        _ctx.Save();
    }

    /// <summary>
    /// Restores the canvas context to the state it had when this scope was created.
    /// </summary>
    public void Dispose() => _ctx.Restore();
}
