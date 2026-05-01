using BrowserApi.Css;
using BrowserApi.Css.Authoring;
using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;

namespace BrowserApi.Tests.Css.Authoring;

/// <summary>Tests for @layer support — both the [Layer] attribute and
/// the indexer form (<c>[CssLayer.Of("name")]</c>).</summary>
[Collection(nameof(CssRegistryCollection))]
public class LayerTests {
    [Layer("utilities")]
    public class UtilitiesStyles : StyleSheet {
        public static readonly Class HiddenSm = new() {
            Display = Display.None,
        };
    }

    [Fact]
    public void Layer_attribute_wraps_entire_body_in_at_layer_block() {
        var css = StyleSheet.Render<UtilitiesStyles>();
        Assert.StartsWith("@layer utilities {", css);
        Assert.Contains(".hidden-sm {", css);
        Assert.Contains("display: none;", css);
        Assert.EndsWith("}\n", css);
    }

    public class IndexerLayerStyles : StyleSheet {
        public static readonly Class Card = new() {
            Padding = Length.Px(16),
            // Single block goes into a layer; the rest of Card stays un-layered.
            [CssLayer.Of("components")] = new() {
                Background = CssColor.White,
            },
        };
    }

    [Fact]
    public void Indexer_form_layer_wraps_just_the_nested_block() {
        var css = StyleSheet.Render<IndexerLayerStyles>();
        Assert.Contains(".card {", css);
        Assert.Contains("padding: 16px;", css);
        Assert.Contains("@layer components {", css);
        Assert.Contains("background: white;", css);
    }
}
