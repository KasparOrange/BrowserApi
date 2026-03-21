using System.Text;

namespace BrowserApi.Generator.Emit;

public sealed class CSharpCodeWriter {
    private readonly StringBuilder _sb = new();
    private int _indent;

    public void AppendLine(string text = "") {
        if (string.IsNullOrEmpty(text)) {
            _sb.AppendLine();
        } else {
            _sb.Append(new string(' ', _indent * 4));
            _sb.AppendLine(text);
        }
    }

    public IDisposable BeginBlock(string header) {
        AppendLine(header);
        AppendLine("{");
        _indent++;
        return new BlockScope(this);
    }

    public void EndBlock() {
        _indent--;
        AppendLine("}");
    }

    public override string ToString() => _sb.ToString();

    private sealed class BlockScope(CSharpCodeWriter writer) : IDisposable {
        public void Dispose() => writer.EndBlock();
    }
}
