using BrowserApi.Css.Authoring;
using BrowserApi.Css.Authoring.External;

namespace BrowserApi.Tests.Css.Authoring;

/// <summary>
/// Tests for the external-CSS source generator that builds typed surfaces
/// from third-party stylesheets like MudBlazor's bundle. The test project
/// wires <c>test-external/sample.css</c> in via <c>AdditionalFiles</c>
/// with root-class <c>Xx</c> and prefix <c>xx-</c>.
/// </summary>
public class ExternalCssTests {
    [Fact]
    public void Single_segment_class_becomes_a_leaf_under_root() {
        // .xx-card-content / .xx-card-header are nested under Xx.Card.
        Assert.Equal("xx-card-content", (string)Xx.Card.Content);
        Assert.Equal("xx-card-header",  (string)Xx.Card.Header);
    }

    [Fact]
    public void Custom_property_groups_emit_as_typed_CssVar_references() {
        // --xx-palette-primary → Xx.Palette.Primary, typed as CssVar<CssColor>.
        Assert.Equal("var(--xx-palette-primary)",   Xx.Palette.Primary.ToCss());
        Assert.Equal("var(--xx-palette-secondary)", Xx.Palette.Secondary.ToCss());
    }

    [Fact]
    public void Underscore_leaf_resolves_to_the_bare_root_class() {
        // .xx-button collides with `Xx.Button` (the nested class for variants),
        // so the bare leaf is bumped into the nested class as `_`. Users get
        // the bare class via `Xx.Button._`.
        Assert.Equal("xx-button", (string)Xx.Button._);
        Assert.Equal("xx-button-primary", (string)Xx.Button.Primary);
    }
}
