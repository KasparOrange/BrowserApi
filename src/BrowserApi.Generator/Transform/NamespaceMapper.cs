namespace BrowserApi.Generator.Transform;

public static class NamespaceMapper {
    private static readonly List<(string Pattern, string Namespace)> MappingRules = [
        ("Console", "BrowserApi.Console"),
        ("DOM", "BrowserApi.Dom"),
        ("HTML", "BrowserApi.Dom"),
        ("Element", "BrowserApi.Dom"),
        ("Node", "BrowserApi.Dom"),
        ("CSS", "BrowserApi.Css"),
        ("CSSOM", "BrowserApi.Css"),
        ("Fetch", "BrowserApi.Fetch"),
        ("XMLHttpRequest", "BrowserApi.Fetch"),
        ("Canvas", "BrowserApi.Canvas"),
        ("Storage", "BrowserApi.WebStorage"),
        ("IndexedDB", "BrowserApi.WebStorage"),
        ("Web Storage", "BrowserApi.WebStorage"),
        ("Event", "BrowserApi.Events"),
        ("UI Events", "BrowserApi.Events"),
        ("Pointer Events", "BrowserApi.Events"),
        ("Touch Events", "BrowserApi.Events"),
        ("Animation", "BrowserApi.Animations"),
        ("Web Animations", "BrowserApi.Animations"),
        ("WebGL", "BrowserApi.WebGl"),
        ("WebGPU", "BrowserApi.WebGpu"),
        ("Web Audio", "BrowserApi.WebAudio"),
        ("WebRTC", "BrowserApi.WebRtc"),
        ("Geometry", "BrowserApi.Geometry"),
        ("Streams", "BrowserApi.Streams"),
        ("Encoding", "BrowserApi.Encoding"),
        ("File", "BrowserApi.FileApi"),
        ("URL", "BrowserApi.Url"),
        ("Credential", "BrowserApi.Credentials"),
        ("Payment", "BrowserApi.Payments"),
        ("Service Worker", "BrowserApi.ServiceWorkers"),
        ("Web Worker", "BrowserApi.Workers"),
        ("WebSocket", "BrowserApi.WebSockets"),
        ("Intersection Observer", "BrowserApi.Observers"),
        ("Resize Observer", "BrowserApi.Observers"),
        ("Mutation Observer", "BrowserApi.Observers"),
        ("Performance", "BrowserApi.PerformanceApi"),
        ("Navigation", "BrowserApi.NavigationApi"),
        ("Screen", "BrowserApi.ScreenApi"),
        ("Clipboard", "BrowserApi.ClipboardApi"),
        ("Notifications", "BrowserApi.Notifications"),
        ("Media", "BrowserApi.Media"),
        ("Fullscreen", "BrowserApi.Fullscreen"),
    ];

    public static string MapToNamespace(string? specTitle) {
        if (string.IsNullOrEmpty(specTitle))
            return "BrowserApi";

        foreach (var (pattern, ns) in MappingRules) {
            if (specTitle.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return ns;
        }

        return "BrowserApi";
    }
}
