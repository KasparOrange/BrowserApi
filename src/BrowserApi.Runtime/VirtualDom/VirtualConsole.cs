namespace BrowserApi.Runtime.VirtualDom;

public class VirtualConsole : IVirtualNode {
    public List<ConsoleMessage> Messages { get; } = [];

    public void Log(params object?[] data) =>
        Messages.Add(new ConsoleMessage("log", FormatArgs(data)));

    public void Error(params object?[] data) =>
        Messages.Add(new ConsoleMessage("error", FormatArgs(data)));

    public void Warn(params object?[] data) =>
        Messages.Add(new ConsoleMessage("warn", FormatArgs(data)));

    public void Info(params object?[] data) =>
        Messages.Add(new ConsoleMessage("info", FormatArgs(data)));

    public void Clear() => Messages.Clear();

    private static string FormatArgs(object?[] data) =>
        string.Join(" ", data.Select(d => d?.ToString() ?? "undefined"));

    public object? GetJsProperty(string jsName) => null;
    public void SetJsProperty(string jsName, object? value) { }

    public object? InvokeJsMethod(string jsName, object?[] args) {
        switch (jsName) {
            case "log": Log(args); break;
            case "error": Error(args); break;
            case "warn": Warn(args); break;
            case "info": Info(args); break;
            case "clear": Clear(); break;
        }
        return null;
    }

    public record ConsoleMessage(string Level, string Text);
}
