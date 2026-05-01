using System.Linq;
using BrowserApi.Css;
using BrowserApi.Css.Authoring;
using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;

namespace BrowserApi.Tests.Css.Authoring;

/// <summary>
/// End-to-end tests that simulate a real Blazor app's stylesheet shape and verify
/// the rendered CSS is what an HTTP-served stylesheet would contain. These are the
/// tests MitWare's first integration would lean on.
/// </summary>
[Collection(nameof(CssRegistryCollection))]
public class EndToEndTests {
    /// <summary>
    /// A representative "design tokens" stylesheet — defines variables consumed by
    /// other stylesheets. In MitWare this would be one shared file.
    /// </summary>
    public class DesignTokens : StyleSheet {
        public static readonly CssVar<Length> Radius = new(Length.Px(8));
        public static readonly CssVar<Length> SpacingMd = new(Length.Px(12));
        public static readonly CssVar<CssColor> BgSurface = new(CssColor.White);
        public static readonly CssVar<CssColor> Primary = new(CssColor.Hex("#0066cc"));
    }

    /// <summary>
    /// A representative component stylesheet — uses the design tokens.
    /// </summary>
    public class ComponentStyles : StyleSheet {
        public static readonly Class Card = new() {
            Background = DesignTokens.BgSurface,
            Padding = DesignTokens.SpacingMd,
            BorderRadius = DesignTokens.Radius,
            Border = Border.Solid(Length.Px(1), CssColor.Hex("#e5e5e5")),
            Display = Display.Block,
            [Self.Hover] = new() {
                BorderColor = DesignTokens.Primary,
            },
        };

        public static readonly Class Btn = new() {
            Display = Display.InlineFlex,
            AlignItems = AlignItems.Center,
            JustifyContent = JustifyContent.Center,
            Gap = Length.Px(8),
            Padding = Length.Px(8),
            Background = DesignTokens.Primary,
            Color = CssColor.White,
            BorderRadius = DesignTokens.Radius,
            Cursor = Cursor.Pointer,
            FontWeight = 600,
            BoxSizing = BrowserApi.Css.BoxSizing.BorderBox,
        };
    }

    [Fact]
    public void CssVars_emit_into_root_block_with_kebab_case_names() {
        var css = StyleSheet.Render<DesignTokens>();

        Assert.Contains(":root {", css);
        Assert.Contains("--radius: 8px;", css);
        Assert.Contains("--spacing-md: 12px;", css);
        Assert.Contains("--bg-surface:", css);
        Assert.Contains("--primary:", css);
    }

    [Fact]
    public void Component_stylesheet_consumes_CssVars_via_var_function() {
        var css = StyleSheet.Render<ComponentStyles>();

        Assert.Contains(".card {", css);
        Assert.Contains("background: var(--bg-surface)", css);
        Assert.Contains("padding: var(--spacing-md)", css);
        Assert.Contains("border-radius: var(--radius)", css);
    }

    [Fact]
    public void Hover_block_resolves_into_separate_rule() {
        var css = StyleSheet.Render<ComponentStyles>();

        Assert.Contains(".card:hover {", css);
        Assert.Contains("border-color: var(--primary)", css);
    }

    [Fact]
    public void Btn_uses_inline_flex_with_centered_axes() {
        var css = StyleSheet.Render<ComponentStyles>();

        Assert.Contains(".btn {", css);
        Assert.Contains("display: inline-flex;", css);
        Assert.Contains("align-items: center;", css);
        Assert.Contains("justify-content: center;", css);
        Assert.Contains("box-sizing: border-box;", css);
        Assert.Contains("cursor: pointer;", css);
    }

    [Fact]
    public void Combined_registry_output_includes_all_stylesheets() {
        CssRegistry.Refresh();
        var combined = CssRegistry.GetCombinedCss();

        // Both stylesheets and their declarations should be present.
        Assert.Contains(":root {", combined);
        Assert.Contains(".card {", combined);
        Assert.Contains(".btn {", combined);
    }

    [Fact]
    public void Class_name_resolves_lazily_for_Razor_consumption() {
        // Simulate Razor markup access path — ToString/implicit string before any
        // Render call happens. Should still get the kebab-cased name.
        CssRegistry.Refresh();
        string asAttribute = ComponentStyles.Btn;
        Assert.Equal("btn", asAttribute);
    }

    /// <summary>
    /// Snapshot-style test — locks in a known-good output for the design-tokens
    /// stylesheet. If the emitter format changes (whitespace, declaration order,
    /// etc.) this fails clearly so we can review intentionally.
    /// </summary>
    [Fact]
    public void DesignTokens_renders_to_known_canonical_output() {
        var css = StyleSheet.Render<DesignTokens>();
        // CssColor.White serializes to the keyword "white" (cleaner devtools output)
        // rather than the equivalent hex. Hex factory output is preserved verbatim.
        // Each typed CssVar also produces an @property block (spec §30) right after
        // the :root declaration, so the browser can type-check the variable.
        Assert.StartsWith(":root { --radius: 8px; --spacing-md: 12px; --bg-surface: white; --primary: #0066cc; }\n", css);
        Assert.Contains("@property --radius { syntax: \"<length>\"; inherits: true; initial-value: 8px; }", css);
        Assert.Contains("@property --primary { syntax: \"<color>\"; inherits: true; initial-value: #0066cc; }", css);
    }
}
