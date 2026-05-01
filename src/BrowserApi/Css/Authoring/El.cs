namespace BrowserApi.Css.Authoring;

/// <summary>
/// Pre-defined HTML element <see cref="Selector"/> instances. Use these as the
/// type-selector half of any CSS rule (<c>El.Body</c>, <c>El.A</c>, …) or as
/// participants in compound and combinator expressions
/// (<c>Card &gt; El.Span</c>, <c>El.Article &gt;&gt; El.A.Hover</c>).
/// </summary>
/// <remarks>
/// <para>
/// In the finished implementation this list is generated from the HTML spec, so
/// every standard element ships as a typed entry. The MVP ships the most common
/// elements by hand to unblock the authoring API; the spec-driven generation
/// fills in the rest in a follow-up commit.
/// </para>
/// <para>
/// <c>El.Root</c> represents <c>:root</c> (used for global custom-property
/// definitions); <c>El.All</c> is the universal selector <c>*</c>.
/// </para>
/// </remarks>
public static class El {
    /// <summary>The CSS <c>:root</c> pseudo-class — used for global CSS variable definitions.</summary>
    public static Selector Root { get; } = new(":root");

    /// <summary>The CSS universal selector <c>*</c> — matches every element.</summary>
    public static Selector All { get; } = new("*");

    /// <summary>The HTML <c>&lt;html&gt;</c> element.</summary>
    public static Selector Html { get; } = new("html");

    /// <summary>The HTML <c>&lt;body&gt;</c> element.</summary>
    public static Selector Body { get; } = new("body");

    /// <summary>The HTML <c>&lt;div&gt;</c> element.</summary>
    public static Selector Div { get; } = new("div");

    /// <summary>The HTML <c>&lt;span&gt;</c> element.</summary>
    public static Selector Span { get; } = new("span");

    /// <summary>The HTML <c>&lt;p&gt;</c> (paragraph) element.</summary>
    public static Selector P { get; } = new("p");

    /// <summary>The HTML <c>&lt;a&gt;</c> (anchor) element.</summary>
    public static Selector A { get; } = new("a");

    /// <summary>The HTML <c>&lt;button&gt;</c> element.</summary>
    public static Selector Button { get; } = new("button");

    /// <summary>The HTML <c>&lt;input&gt;</c> element.</summary>
    public static Selector Input { get; } = new("input");

    /// <summary>The HTML <c>&lt;textarea&gt;</c> element.</summary>
    public static Selector Textarea { get; } = new("textarea");

    /// <summary>The HTML <c>&lt;select&gt;</c> element.</summary>
    public static Selector Select { get; } = new("select");

    /// <summary>The HTML <c>&lt;label&gt;</c> element.</summary>
    public static Selector Label { get; } = new("label");

    /// <summary>The HTML <c>&lt;form&gt;</c> element.</summary>
    public static Selector Form { get; } = new("form");

    /// <summary>The HTML <c>&lt;ul&gt;</c> (unordered list) element.</summary>
    public static Selector Ul { get; } = new("ul");

    /// <summary>The HTML <c>&lt;ol&gt;</c> (ordered list) element.</summary>
    public static Selector Ol { get; } = new("ol");

    /// <summary>The HTML <c>&lt;li&gt;</c> (list item) element.</summary>
    public static Selector Li { get; } = new("li");

    /// <summary>The HTML <c>&lt;table&gt;</c> element.</summary>
    public static Selector Table { get; } = new("table");

    /// <summary>The HTML <c>&lt;tr&gt;</c> (table row) element.</summary>
    public static Selector Tr { get; } = new("tr");

    /// <summary>The HTML <c>&lt;td&gt;</c> (table data cell) element.</summary>
    public static Selector Td { get; } = new("td");

    /// <summary>The HTML <c>&lt;th&gt;</c> (table header cell) element.</summary>
    public static Selector Th { get; } = new("th");

    /// <summary>The HTML <c>&lt;thead&gt;</c> element.</summary>
    public static Selector Thead { get; } = new("thead");

    /// <summary>The HTML <c>&lt;tbody&gt;</c> element.</summary>
    public static Selector Tbody { get; } = new("tbody");

    /// <summary>The HTML <c>&lt;article&gt;</c> element.</summary>
    public static Selector Article { get; } = new("article");

    /// <summary>The HTML <c>&lt;section&gt;</c> element.</summary>
    public static Selector Section { get; } = new("section");

    /// <summary>The HTML <c>&lt;nav&gt;</c> element.</summary>
    public static Selector Nav { get; } = new("nav");

    /// <summary>The HTML <c>&lt;header&gt;</c> element.</summary>
    public static Selector Header { get; } = new("header");

    /// <summary>The HTML <c>&lt;footer&gt;</c> element.</summary>
    public static Selector Footer { get; } = new("footer");

    /// <summary>The HTML <c>&lt;main&gt;</c> element.</summary>
    public static Selector Main { get; } = new("main");

    /// <summary>The HTML <c>&lt;aside&gt;</c> element.</summary>
    public static Selector Aside { get; } = new("aside");

    /// <summary>The HTML <c>&lt;img&gt;</c> element.</summary>
    public static Selector Img { get; } = new("img");

    /// <summary>The HTML <c>&lt;svg&gt;</c> element.</summary>
    public static Selector Svg { get; } = new("svg");

    /// <summary>The HTML <c>&lt;h1&gt;</c> heading element.</summary>
    public static Selector H1 { get; } = new("h1");

    /// <summary>The HTML <c>&lt;h2&gt;</c> heading element.</summary>
    public static Selector H2 { get; } = new("h2");

    /// <summary>The HTML <c>&lt;h3&gt;</c> heading element.</summary>
    public static Selector H3 { get; } = new("h3");

    /// <summary>The HTML <c>&lt;h4&gt;</c> heading element.</summary>
    public static Selector H4 { get; } = new("h4");

    /// <summary>The HTML <c>&lt;h5&gt;</c> heading element.</summary>
    public static Selector H5 { get; } = new("h5");

    /// <summary>The HTML <c>&lt;h6&gt;</c> heading element.</summary>
    public static Selector H6 { get; } = new("h6");
}
