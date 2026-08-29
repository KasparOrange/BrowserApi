using BrowserApi.Css.Compiler;

namespace BrowserApi.Css.Compiler.Tests;

[Collection("CssRegistry")]
public class CssIslandWatcherTests : IDisposable {
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "browserapi-island-" + Guid.NewGuid().ToString("N"));

    public CssIslandWatcherTests() {
        Directory.CreateDirectory(Path.Combine(_dir, "bin"));
        Directory.CreateDirectory(Path.Combine(_dir, "Pages"));
    }

    [Fact]
    public void FindFiles_skips_build_output_and_sorts() {
        File.WriteAllText(Path.Combine(_dir, "Pages", "B.css.cs"), "");
        File.WriteAllText(Path.Combine(_dir, "A.css.cs"), "");
        File.WriteAllText(Path.Combine(_dir, "bin", "Z.css.cs"), "");
        File.WriteAllText(Path.Combine(_dir, "NotIsland.cs"), "");

        var files = CssIsland.FindFiles(_dir).Select(f => Path.GetRelativePath(_dir, f)).ToArray();
        Assert.Equal(["A.css.cs", Path.Combine("Pages", "B.css.cs")], files);
    }

    [Fact]
    public async Task Save_triggers_one_debounced_compile() {
        var path = Path.Combine(_dir, "S.css.cs");
        File.WriteAllText(path, Sheet("8"));
        using var watcher = new CssIslandWatcher(_dir, new CssIslandCompiler(), TimeSpan.FromMilliseconds(100));
        var results = new List<CssIslandResult>();
        var gotOne = new TaskCompletionSource();
        watcher.Compiled += r => { lock (results) results.Add(r); gotOne.TrySetResult(); };
        watcher.Start();

        // Burst of writes within the debounce window.
        File.WriteAllText(path, Sheet("16"));
        File.WriteAllText(path, Sheet("24"));

        await gotOne.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await Task.Delay(300); // let any spurious second compile surface

        lock (results) {
            Assert.Single(results);
            Assert.Contains("padding: 24px", results[0].CombinedCss);
        }
    }

    private static string Sheet(string px) => $$"""
        using BrowserApi.Css;
        using BrowserApi.Css.Authoring;
        public class S : BrowserApi.Css.Authoring.StyleSheet {
            public static readonly Class Box = new() { Padding = Length.Px({{px}}) };
        }
        """;

    public void Dispose() {
        try { Directory.Delete(_dir, recursive: true); } catch { /* temp */ }
    }
}
