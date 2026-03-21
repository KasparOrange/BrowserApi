using BrowserApi.Dom;

namespace BrowserApi.Canvas;

public readonly struct CanvasStateScope : IDisposable {
    private readonly CanvasRenderingContext2D _ctx;

    internal CanvasStateScope(CanvasRenderingContext2D ctx) {
        _ctx = ctx;
        _ctx.Save();
    }

    public void Dispose() => _ctx.Restore();
}
