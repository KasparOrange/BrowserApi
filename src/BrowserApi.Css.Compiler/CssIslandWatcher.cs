namespace BrowserApi.Css.Compiler;

/// <summary>
/// Watches a project directory for <c>*.css.cs</c> changes and recompiles the
/// island after a short quiet period, publishing each result via <see cref="Compiled"/>.
/// </summary>
/// <remarks>
/// Saves arrive as bursts (editors write temp files, rename, touch); the
/// debounce collapses a burst into one compile. Compiles are serialized — a
/// change during a compile schedules exactly one follow-up, so the last save
/// always wins and no result is published out of order.
/// </remarks>
public sealed class CssIslandWatcher : IDisposable {
    private readonly string _projectDirectory;
    private readonly CssIslandCompiler _compiler;
    private readonly TimeSpan _debounce;
    private readonly FileSystemWatcher _fsw;
    private readonly object _gate = new();
    private Timer? _timer;
    private bool _compiling;
    private bool _pending;

    /// <summary>Fires after every compile, success or failure, on a thread-pool thread.</summary>
    public event Action<CssIslandResult>? Compiled;

    /// <summary>Fires when a compile throws unexpectedly (I/O race, reflection failure).</summary>
    public event Action<System.Exception>? Faulted;

    /// <param name="projectDirectory">Root to watch recursively.</param>
    /// <param name="compiler">Compiler to reuse across saves (its reference set is cached).</param>
    /// <param name="debounce">Quiet period before compiling; default 150 ms.</param>
    public CssIslandWatcher(string projectDirectory, CssIslandCompiler compiler, TimeSpan? debounce = null) {
        _projectDirectory = Path.GetFullPath(projectDirectory);
        _compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
        _debounce = debounce ?? TimeSpan.FromMilliseconds(150);

        _fsw = new FileSystemWatcher(_projectDirectory) {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
        };
        _fsw.Changed += OnFsEvent;
        _fsw.Created += OnFsEvent;
        _fsw.Deleted += OnFsEvent;
        _fsw.Renamed += OnFsEvent;
    }

    /// <summary>Starts watching. Does not compile immediately — call <see cref="CompileNow"/> for that.</summary>
    public void Start() => _fsw.EnableRaisingEvents = true;

    /// <summary>Compiles the current island synchronously and publishes the result.</summary>
    public CssIslandResult CompileNow() {
        var result = _compiler.Compile(CssIsland.FindFiles(_projectDirectory));
        Compiled?.Invoke(result);
        return result;
    }

    private void OnFsEvent(object sender, FileSystemEventArgs e) {
        if (!IsRelevant(e.FullPath) && !(e is RenamedEventArgs r && IsRelevant(r.OldFullPath))) return;
        lock (_gate) {
            _timer ??= new Timer(_ => Tick(), null, Timeout.Infinite, Timeout.Infinite);
            _timer.Change(_debounce, Timeout.InfiniteTimeSpan);
        }
    }

    private bool IsRelevant(string path) =>
        CssIsland.IsIslandFile(path) && !CssIsland.IsBuildOutput(_projectDirectory, path);

    private void Tick() {
        lock (_gate) {
            if (_compiling) { _pending = true; return; }
            _compiling = true;
        }
        try {
            do {
                lock (_gate) _pending = false;
                try {
                    CompileNow();
                } catch (System.Exception ex) {
                    Faulted?.Invoke(ex);
                }
                lock (_gate) {
                    if (!_pending) { _compiling = false; return; }
                }
            } while (true);
        } finally {
            lock (_gate) _compiling = false;
        }
    }

    public void Dispose() {
        _fsw.EnableRaisingEvents = false;
        _fsw.Dispose();
        lock (_gate) {
            _timer?.Dispose();
            _timer = null;
        }
    }
}
