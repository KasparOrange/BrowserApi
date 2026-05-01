using System.Collections;
using System.Collections.Generic;

namespace BrowserApi.Css.Authoring;

/// <summary>
/// A collection of anonymous <see cref="Rule"/>s — for grouping resets,
/// element-style declarations, or any block of rules that share a logical
/// concern but don't need individual field names.
/// </summary>
/// <example>
/// <code>
/// public static readonly Rules Reset = new() {
///     new Rule(El.All)  { BoxSizing = BrowserApi.Css.BoxSizing.BorderBox },
///     new Rule(El.Body) { Margin    = Length.Px(0) },
///     new Rule(Where(El.H1, El.H2, El.H3)) { LineHeight = 1.2 },
/// };
/// </code>
/// </example>
/// <remarks>
/// Discovery is by C# type — the source generator (or <see cref="StyleSheet.Render(System.Type)"/>)
/// finds every <c>static readonly Rules</c> field regardless of the field name,
/// matching the spec's "no magic names" principle.
/// </remarks>
public sealed class Rules : IEnumerable<Rule> {
    private readonly List<Rule> _rules = new();

    /// <summary>Adds a rule. Used by the collection-initializer syntax.</summary>
    public void Add(Rule rule) => _rules.Add(rule);

    /// <inheritdoc/>
    public IEnumerator<Rule> GetEnumerator() => _rules.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
