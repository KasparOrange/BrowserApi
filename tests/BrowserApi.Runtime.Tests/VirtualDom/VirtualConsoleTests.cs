using BrowserApi.Runtime.VirtualDom;

namespace BrowserApi.Runtime.Tests.VirtualDom;

public class VirtualConsoleTests {
    [Fact]
    public void Log_captures_message_with_log_level() {
        var console = new VirtualConsole();
        console.Log("hello");

        Assert.Single(console.Messages);
        Assert.Equal("log", console.Messages[0].Level);
        Assert.Equal("hello", console.Messages[0].Text);
    }

    [Fact]
    public void Error_captures_message_with_error_level() {
        var console = new VirtualConsole();
        console.Error("something broke");

        Assert.Single(console.Messages);
        Assert.Equal("error", console.Messages[0].Level);
        Assert.Equal("something broke", console.Messages[0].Text);
    }

    [Fact]
    public void Warn_captures_message_with_warn_level() {
        var console = new VirtualConsole();
        console.Warn("be careful");

        Assert.Single(console.Messages);
        Assert.Equal("warn", console.Messages[0].Level);
        Assert.Equal("be careful", console.Messages[0].Text);
    }

    [Fact]
    public void Info_captures_message_with_info_level() {
        var console = new VirtualConsole();
        console.Info("FYI");

        Assert.Single(console.Messages);
        Assert.Equal("info", console.Messages[0].Level);
        Assert.Equal("FYI", console.Messages[0].Text);
    }

    [Fact]
    public void Log_multiple_args_joined_by_space() {
        var console = new VirtualConsole();
        console.Log("hello", "world", 42);

        Assert.Equal("hello world 42", console.Messages[0].Text);
    }

    [Fact]
    public void Error_multiple_args_joined_by_space() {
        var console = new VirtualConsole();
        console.Error("code", 500);

        Assert.Equal("code 500", console.Messages[0].Text);
    }

    [Fact]
    public void Warn_multiple_args_joined_by_space() {
        var console = new VirtualConsole();
        console.Warn("limit", "reached", 100);

        Assert.Equal("limit reached 100", console.Messages[0].Text);
    }

    [Fact]
    public void Info_multiple_args_joined_by_space() {
        var console = new VirtualConsole();
        console.Info("version", "2.0");

        Assert.Equal("version 2.0", console.Messages[0].Text);
    }

    [Fact]
    public void FormatArgs_null_becomes_undefined() {
        var console = new VirtualConsole();
        console.Log("value:", null);

        Assert.Equal("value: undefined", console.Messages[0].Text);
    }

    [Fact]
    public void FormatArgs_all_null_args() {
        var console = new VirtualConsole();
        console.Log(null, null);

        Assert.Equal("undefined undefined", console.Messages[0].Text);
    }

    [Fact]
    public void Clear_removes_all_messages() {
        var console = new VirtualConsole();
        console.Log("one");
        console.Error("two");
        console.Warn("three");
        Assert.Equal(3, console.Messages.Count);

        console.Clear();
        Assert.Empty(console.Messages);
    }

    [Fact]
    public void Clear_on_empty_is_noop() {
        var console = new VirtualConsole();
        console.Clear();
        Assert.Empty(console.Messages);
    }

    [Fact]
    public void Messages_preserve_order() {
        var console = new VirtualConsole();
        console.Log("first");
        console.Error("second");
        console.Warn("third");
        console.Info("fourth");

        Assert.Equal(4, console.Messages.Count);
        Assert.Equal("log", console.Messages[0].Level);
        Assert.Equal("error", console.Messages[1].Level);
        Assert.Equal("warn", console.Messages[2].Level);
        Assert.Equal("info", console.Messages[3].Level);
        Assert.Equal("first", console.Messages[0].Text);
        Assert.Equal("second", console.Messages[1].Text);
        Assert.Equal("third", console.Messages[2].Text);
        Assert.Equal("fourth", console.Messages[3].Text);
    }

    [Fact]
    public void InvokeJsMethod_log_dispatches_to_Log() {
        var console = new VirtualConsole();
        console.InvokeJsMethod("log", ["dispatched"]);

        Assert.Single(console.Messages);
        Assert.Equal("log", console.Messages[0].Level);
        Assert.Equal("dispatched", console.Messages[0].Text);
    }

    [Fact]
    public void InvokeJsMethod_error_dispatches_to_Error() {
        var console = new VirtualConsole();
        console.InvokeJsMethod("error", ["err msg"]);

        Assert.Single(console.Messages);
        Assert.Equal("error", console.Messages[0].Level);
        Assert.Equal("err msg", console.Messages[0].Text);
    }

    [Fact]
    public void InvokeJsMethod_warn_dispatches_to_Warn() {
        var console = new VirtualConsole();
        console.InvokeJsMethod("warn", ["warning!"]);

        Assert.Single(console.Messages);
        Assert.Equal("warn", console.Messages[0].Level);
        Assert.Equal("warning!", console.Messages[0].Text);
    }

    [Fact]
    public void InvokeJsMethod_info_dispatches_to_Info() {
        var console = new VirtualConsole();
        console.InvokeJsMethod("info", ["info msg"]);

        Assert.Single(console.Messages);
        Assert.Equal("info", console.Messages[0].Level);
        Assert.Equal("info msg", console.Messages[0].Text);
    }

    [Fact]
    public void InvokeJsMethod_clear_dispatches_to_Clear() {
        var console = new VirtualConsole();
        console.Log("will be cleared");
        Assert.Single(console.Messages);

        console.InvokeJsMethod("clear", []);
        Assert.Empty(console.Messages);
    }

    [Fact]
    public void InvokeJsMethod_unknown_method_returns_null() {
        var console = new VirtualConsole();
        var result = console.InvokeJsMethod("unknownMethod", ["arg"]);
        Assert.Null(result);
        Assert.Empty(console.Messages);
    }

    [Fact]
    public void InvokeJsMethod_log_with_multiple_args() {
        var console = new VirtualConsole();
        console.InvokeJsMethod("log", ["a", "b", "c"]);

        Assert.Equal("a b c", console.Messages[0].Text);
    }

    [Fact]
    public void GetJsProperty_always_returns_null() {
        var console = new VirtualConsole();
        Assert.Null(console.GetJsProperty("anything"));
        Assert.Null(console.GetJsProperty("log"));
    }

    [Fact]
    public void SetJsProperty_is_noop() {
        var console = new VirtualConsole();
        console.SetJsProperty("anything", "value");
        // No exception, no side effects
        Assert.Empty(console.Messages);
    }

    [Fact]
    public void Log_empty_args_produces_empty_text() {
        var console = new VirtualConsole();
        console.Log();

        Assert.Single(console.Messages);
        Assert.Equal("", console.Messages[0].Text);
    }

    [Fact]
    public void ConsoleMessage_record_equality() {
        var msg1 = new VirtualConsole.ConsoleMessage("log", "hello");
        var msg2 = new VirtualConsole.ConsoleMessage("log", "hello");
        var msg3 = new VirtualConsole.ConsoleMessage("error", "hello");

        Assert.Equal(msg1, msg2);
        Assert.NotEqual(msg1, msg3);
    }
}
