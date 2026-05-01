namespace BrowserApi.Css.Authoring;

/// <summary>
/// CSS attribute-selector entry point. Five tiers:
/// <list type="number">
///   <item>Standard HTML attributes: <c>Attr.Type</c>, <c>Attr.Href</c>, <c>Attr.Disabled</c></item>
///   <item>ARIA role: <c>Attr.Role</c></item>
///   <item>ARIA attributes (the <c>aria-*</c> family): <c>Attr.Aria.Hidden</c>, <c>Attr.Aria.Label</c></item>
///   <item>Data attributes (the <c>data-*</c> family): <c>Attr.Data("stick")</c></item>
///   <item>Escape hatch — any attribute by name: <c>Attr.Of("potato")</c></item>
/// </list>
/// </summary>
/// <remarks>
/// Spec §15. The MVP slice ships representative attributes from each tier;
/// the full HTML-attribute and WAI-ARIA list comes from the same spec
/// generator that produces the rest of <c>El.*</c>.
/// </remarks>
public static class Attr {
    // Tier 1 — standard HTML attributes (subset).
    /// <summary>The HTML <c>type</c> attribute.</summary>
    public static AttrSelector Type { get; } = new("type");

    /// <summary>The HTML <c>href</c> attribute.</summary>
    public static AttrSelector Href { get; } = new("href");

    /// <summary>The HTML <c>id</c> attribute (matched as a selector — for STYLING use a class).</summary>
    public static AttrSelector Id { get; } = new("id");

    /// <summary>The HTML <c>name</c> attribute.</summary>
    public static AttrSelector Name { get; } = new("name");

    /// <summary>The HTML <c>value</c> attribute.</summary>
    public static AttrSelector Value { get; } = new("value");

    /// <summary>The HTML <c>title</c> attribute.</summary>
    public static AttrSelector Title { get; } = new("title");

    /// <summary>Boolean HTML <c>disabled</c> attribute (presence selector).</summary>
    public static AttrSelector Disabled { get; } = new("disabled");

    /// <summary>Boolean HTML <c>readonly</c> attribute.</summary>
    public static AttrSelector Readonly { get; } = new("readonly");

    /// <summary>Boolean HTML <c>required</c> attribute.</summary>
    public static AttrSelector Required { get; } = new("required");

    /// <summary>Boolean HTML <c>checked</c> attribute.</summary>
    public static AttrSelector Checked { get; } = new("checked");

    /// <summary>Boolean HTML <c>open</c> attribute (details/dialog).</summary>
    public static AttrSelector Open { get; } = new("open");

    // Tier 2 — ARIA role.
    /// <summary>The ARIA <c>role</c> attribute.</summary>
    public static AttrSelector Role { get; } = new("role");

    // Tier 3 — ARIA attributes (subset).
    /// <summary>ARIA attributes (<c>aria-*</c>).</summary>
    public static class Aria {
        /// <summary><c>aria-hidden</c></summary>
        public static AttrSelector Hidden { get; } = new("aria-hidden");
        /// <summary><c>aria-label</c></summary>
        public static AttrSelector Label { get; } = new("aria-label");
        /// <summary><c>aria-labelledby</c></summary>
        public static AttrSelector LabelledBy { get; } = new("aria-labelledby");
        /// <summary><c>aria-describedby</c></summary>
        public static AttrSelector DescribedBy { get; } = new("aria-describedby");
        /// <summary><c>aria-current</c></summary>
        public static AttrSelector Current { get; } = new("aria-current");
        /// <summary><c>aria-expanded</c></summary>
        public static AttrSelector Expanded { get; } = new("aria-expanded");
        /// <summary><c>aria-pressed</c></summary>
        public static AttrSelector Pressed { get; } = new("aria-pressed");
        /// <summary><c>aria-selected</c></summary>
        public static AttrSelector Selected { get; } = new("aria-selected");
        /// <summary><c>aria-disabled</c></summary>
        public static AttrSelector Disabled { get; } = new("aria-disabled");
        /// <summary><c>aria-checked</c></summary>
        public static AttrSelector Checked { get; } = new("aria-checked");
    }

    // Tier 4 — data-* attributes.
    /// <summary>Builds an attribute selector for <c>data-{suffix}</c>.</summary>
    /// <param name="suffix">The data-attribute suffix (without leading <c>data-</c>).</param>
    public static AttrSelector Data(string suffix) => new($"data-{suffix}");

    // Tier 5 — escape hatch.
    /// <summary>Builds an attribute selector for any attribute name. Use sparingly
    /// — prefer the typed entry points when one exists.</summary>
    /// <param name="name">The literal attribute name.</param>
    public static AttrSelector Of(string name) => new(name);
}

/// <summary>
/// A CSS attribute selector — wraps an attribute name and supports the full
/// set of CSS attribute matchers (<c>=</c>, <c>~=</c>, <c>|=</c>, <c>^=</c>,
/// <c>$=</c>, <c>*=</c>) plus the bare presence form.
/// </summary>
public readonly struct AttrSelector {
    private readonly string _attrName;

    /// <summary>Wraps an attribute name (e.g. <c>"href"</c>).</summary>
    public AttrSelector(string attrName) { _attrName = attrName; }

    /// <summary>Implicit conversion to <see cref="Selector"/> for the bare-presence
    /// form: <c>[attr]</c>.</summary>
    public static implicit operator Selector(AttrSelector a) => new($"[{a._attrName}]");

    /// <summary><c>[attr="value"]</c> — exact match.</summary>
    public Selector Equals(string value) => new($"[{_attrName}=\"{value}\"]");

    /// <summary><c>[attr~="value"]</c> — whitespace-separated word match.</summary>
    public Selector HasWord(string value) => new($"[{_attrName}~=\"{value}\"]");

    /// <summary><c>[attr|="value"]</c> — exact OR <c>value-</c> prefix (lang codes).</summary>
    public Selector DashMatch(string value) => new($"[{_attrName}|=\"{value}\"]");

    /// <summary><c>[attr^="value"]</c> — starts-with.</summary>
    public Selector StartsWith(string value) => new($"[{_attrName}^=\"{value}\"]");

    /// <summary><c>[attr$="value"]</c> — ends-with.</summary>
    public Selector EndsWith(string value) => new($"[{_attrName}$=\"{value}\"]");

    /// <summary><c>[attr*="value"]</c> — contains substring.</summary>
    public Selector Contains(string value) => new($"[{_attrName}*=\"{value}\"]");
}
