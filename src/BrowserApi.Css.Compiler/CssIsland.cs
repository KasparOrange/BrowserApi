namespace BrowserApi.Css.Compiler;

/// <summary>
/// Locates the stylesheet "island" of a project: every <c>*.css.cs</c> file
/// below a directory, excluding build output.
/// </summary>
/// <remarks>
/// <para>
/// The island convention is what makes an isolated, MSBuild-free compile
/// legitimate. Stylesheet files depend on <c>BrowserApi</c> and on each other
/// (<c>Tokens.Spacing</c>) — never on the app's components, services, or
/// generated Razor code. So the island plus one assembly reference is a
/// complete compilation, finished in tens of milliseconds.
/// </para>
/// <para>
/// Consequence for authors: a <c>.css.cs</c> file that reaches outside the
/// island (an app enum, a DI service) compiles fine in the app but breaks the
/// island build. The compiler reports that as a normal C# diagnostic; the fix
/// is to move the shared value into a stylesheet (e.g. a <c>CssVar</c>).
/// </para>
/// </remarks>
public static class CssIsland {
    /// <summary>File suffix that marks a file as part of the island.</summary>
    public const string FileSuffix = ".css.cs";

    /// <summary>
    /// Enumerates island files under <paramref name="projectDirectory"/>, sorted
    /// ordinally by full path so compilation input is deterministic.
    /// </summary>
    public static IReadOnlyList<string> FindFiles(string projectDirectory) {
        if (string.IsNullOrWhiteSpace(projectDirectory)) throw new ArgumentException("Project directory is required.", nameof(projectDirectory));
        var root = Path.GetFullPath(projectDirectory);
        var files = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*" + FileSuffix, SearchOption.AllDirectories)) {
            if (IsBuildOutput(root, file)) continue;
            files.Add(file);
        }
        files.Sort(StringComparer.Ordinal);
        return files;
    }

    /// <summary>True when <paramref name="path"/> ends with <see cref="FileSuffix"/> (ordinal, case-insensitive).</summary>
    public static bool IsIslandFile(string path) =>
        path.EndsWith(FileSuffix, StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the file sits under a <c>bin/</c> or <c>obj/</c> segment relative to <paramref name="root"/>.</summary>
    public static bool IsBuildOutput(string root, string file) {
        var relative = Path.GetRelativePath(root, file);
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) {
            if (segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals("obj", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }
        return false;
    }
}
