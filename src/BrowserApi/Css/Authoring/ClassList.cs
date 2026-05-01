using System.Collections.Generic;
using System.Text;

namespace BrowserApi.Css.Authoring;

/// <summary>
/// A space-separated list of CSS class names, the Razor-side companion to
/// <see cref="Class"/>. Optimized for the common case (1–4 classes) with inline
/// fields so no heap allocation occurs; longer lists fall back to a list field.
/// </summary>
/// <remarks>
/// <para>
/// Build a <see cref="ClassList"/> with <c>+</c> on <see cref="Class"/> instances:
/// <c>Card + Active + Round</c> yields a list of three. The implicit string conversion
/// joins them with single spaces, suitable for <c>class="…"</c> attributes.
/// </para>
/// <para>
/// The <see cref="ClassList"/> ignores empty names (e.g. <see cref="Class.None"/>),
/// so <c>Card + Active.When(false)</c> renders as <c>"card"</c>, not <c>"card "</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;div class="@(Card + Active + Round)"&gt;
/// &lt;div class="@(Card + "vendor-specific")"&gt;   // string escape hatch
/// </code>
/// </example>
public struct ClassList {
    private string? _slot0, _slot1, _slot2, _slot3;
    private List<string>? _overflow;
    private int _count;

    /// <summary>Adds a typed class, ignoring empty names so conditional helpers compose cleanly.</summary>
    public ClassList Add(Class c) => string.IsNullOrEmpty(c?.Name) ? this : AddInternal(c.Name);

    /// <summary>Adds a raw class-name string. Empty/null is silently dropped.</summary>
    public ClassList Add(string? name) => string.IsNullOrEmpty(name) ? this : AddInternal(name);

    private ClassList AddInternal(string name) {
        switch (_count) {
            case 0: _slot0 = name; break;
            case 1: _slot1 = name; break;
            case 2: _slot2 = name; break;
            case 3: _slot3 = name; break;
            default:
                _overflow ??= new List<string>();
                _overflow.Add(name);
                break;
        }
        _count++;
        return this;
    }

    /// <summary>Composes another class onto an existing list.</summary>
    public static ClassList operator +(ClassList list, Class c) => list.Add(c);

    /// <summary>Composes a raw class-name string onto an existing list.</summary>
    public static ClassList operator +(ClassList list, string raw) => list.Add(raw);

    /// <summary>Renders as a space-joined class list.</summary>
    public override string ToString() {
        if (_count == 0) return string.Empty;
        var sb = new StringBuilder();
        AppendIfPresent(sb, _slot0);
        AppendIfPresent(sb, _slot1);
        AppendIfPresent(sb, _slot2);
        AppendIfPresent(sb, _slot3);
        if (_overflow is not null) {
            foreach (var s in _overflow) AppendIfPresent(sb, s);
        }
        return sb.ToString();

        static void AppendIfPresent(StringBuilder sb, string? s) {
            if (string.IsNullOrEmpty(s)) return;
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(s);
        }
    }

    /// <summary>Implicit conversion for <c>class="@(…)"</c> in Razor.</summary>
    public static implicit operator string(ClassList list) => list.ToString();
}
