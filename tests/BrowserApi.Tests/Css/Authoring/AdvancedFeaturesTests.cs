using BrowserApi.Css;
using BrowserApi.Css.Authoring;
using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;

namespace BrowserApi.Tests.Css.Authoring;

/// <summary>Tests for the multi-form Padding/Margin shorthand via <see cref="Sides"/>.</summary>
public class SidesTests {
    private class SidesStyles : StyleSheet {
        public static readonly Class All = new() {
            Padding = Length.Px(10),
        };

        public static readonly Class TwoAxis = new() {
            Padding = (Length.Px(10), Length.Px(20)),
        };

        public static readonly Class Quad = new() {
            Padding = Sides.Of(top: Length.Px(10), right: Length.Px(20), bottom: Length.Px(30), left: Length.Px(40)),
        };
    }

    [Fact]
    public void Single_length_emits_one_value_for_all_sides() {
        var css = StyleSheet.Render<SidesStyles>();
        Assert.Contains(".all { padding: 10px; }", css);
    }

    [Fact]
    public void Two_length_tuple_emits_vertical_horizontal() {
        var css = StyleSheet.Render<SidesStyles>();
        Assert.Contains(".two-axis { padding: 10px 20px; }", css);
    }

    [Fact]
    public void Four_length_named_factory_emits_top_right_bottom_left() {
        var css = StyleSheet.Render<SidesStyles>();
        Assert.Contains(".quad { padding: 10px 20px 30px 40px; }", css);
    }
}

/// <summary>Tests for media queries via the nesting indexer.</summary>
public class MediaQueryTests {
    private class MediaStyles : StyleSheet {
        public static readonly Class Card = new() {
            Padding = Length.Px(16),
            [MediaQuery.MaxWidth(Length.Px(768))] = new() {
                Padding = Length.Px(8),
            },
            [MediaQuery.PrefersDark] = new() {
                Background = CssColor.Hex("#1a1a1a"),
            },
        };
    }

    [Fact]
    public void Max_width_emits_an_at_media_block() {
        var css = StyleSheet.Render<MediaStyles>();
        Assert.Contains("@media (max-width: 768px) {", css);
        Assert.Contains(".card { padding: 8px; }", css);
    }

    [Fact]
    public void Prefers_dark_emits_color_scheme_query() {
        var css = StyleSheet.Render<MediaStyles>();
        Assert.Contains("@media (prefers-color-scheme: dark) {", css);
        Assert.Contains("background: #1a1a1a", css);
    }

    [Fact]
    public void Combined_media_features_use_and() {
        var combined = MediaQuery.MinWidth(Length.Px(768)) & MediaQuery.MaxWidth(Length.Px(1023));
        Selector sel = combined;
        Assert.Equal("@media (min-width: 768px) and (max-width: 1023px)", sel.Css);
    }
}

/// <summary>Tests for @keyframes animations.</summary>
public class KeyframesTests {
    private class AnimStyles : StyleSheet {
        public static readonly Keyframes FadeIn = new() {
            [From] = new() { Opacity = 0 },
            [50.Percent()] = new() { Opacity = 0.5 },
            [To] = new() { Opacity = 1 },
        };
    }

    [Fact]
    public void Field_name_becomes_kebab_cased_animation_name() {
        var css = StyleSheet.Render<AnimStyles>();
        Assert.Contains("@keyframes fade-in {", css);
    }

    [Fact]
    public void From_to_constants_render_as_zero_and_hundred_percent() {
        var css = StyleSheet.Render<AnimStyles>();
        Assert.Contains("0% {", css);
        Assert.Contains("100% {", css);
    }

    [Fact]
    public void Intermediate_percentage_stops_render() {
        var css = StyleSheet.Render<AnimStyles>();
        Assert.Contains("50% {", css);
        Assert.Contains("opacity: 0.5", css);
    }
}
