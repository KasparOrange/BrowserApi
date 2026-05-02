using System;
using BrowserApi.Css;
using BrowserApi.Css.Authoring;
using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;

namespace BrowserApi.Tests.Css.Authoring;

/// <summary>
/// Real-world authoring patterns. Each test models a self-contained piece of UI
/// (button, card, form, modal, navigation, layout grid) and asserts that the
/// rendered CSS matches a sensible expectation. These tests double as
/// documentation of how the API is meant to be used end-to-end.
/// </summary>
[Collection(nameof(CssRegistryCollection))]
public class CookbookTests {
    // ═══════════════════════════════════════════════════════════════════════════════
    //  Design-token style — variables consumed by everything else
    // ═══════════════════════════════════════════════════════════════════════════════

    public class Tokens : StyleSheet {
        // Spacing scale — descriptive names render to friendly CSS variables.
        public static readonly CssVar<Length> SpacingXs = new(Length.Px(4));
        public static readonly CssVar<Length> SpacingSm = new(Length.Px(8));
        public static readonly CssVar<Length> SpacingMd = new(Length.Px(12));
        public static readonly CssVar<Length> SpacingLg = new(Length.Px(16));

        // Color palette
        public static readonly CssVar<CssColor> Primary    = new(CssColor.Hex("#0066cc"));
        public static readonly CssVar<CssColor> PrimaryDark = new(CssColor.Hex("#0052aa"));
        public static readonly CssVar<CssColor> Surface     = new(CssColor.White);
        public static readonly CssVar<CssColor> Border      = new(CssColor.Hex("#e5e5e5"));
        public static readonly CssVar<CssColor> Text        = new(CssColor.Hex("#1a1a1a"));
        public static readonly CssVar<CssColor> TextMuted   = new(CssColor.Hex("#666"));

        // Geometry
        public static readonly CssVar<Length> Radius      = new(Length.Px(8));
        public static readonly CssVar<Length> RadiusSmall = new(Length.Px(4));
    }

    [Fact]
    public void Tokens_render_into_a_root_block_with_kebab_case_names() {
        var css = StyleSheet.Render<Tokens>();

        Assert.Contains(":root {", css);
        Assert.Contains("--spacing-xs: 4px;", css);
        Assert.Contains("--spacing-lg: 16px;", css);
        Assert.Contains("--primary: #0066cc;", css);
        Assert.Contains("--primary-dark: #0052aa;", css);
        Assert.Contains("--text-muted: #666;", css);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  Button — primary/secondary variants, hover, focus-visible, disabled
    // ═══════════════════════════════════════════════════════════════════════════════

    public class ButtonStyles : StyleSheet {
        public static readonly Class Btn = new() {
            Display       = Display.InlineFlex,
            AlignItems    = AlignItems.Center,
            JustifyContent = JustifyContent.Center,
            Gap           = Tokens.SpacingSm,
            // Use Sides.Of when feeding CssVars — C# tuple-element conversions
            // don't auto-apply CssVar→Length per element, the explicit factory does.
            Padding       = Sides.Of(Tokens.SpacingSm, Tokens.SpacingLg),
            Background    = Tokens.Primary,
            Color         = CssColor.White,
            BorderRadius  = Tokens.Radius,
            Border        = Border.None,
            Cursor        = Cursor.Pointer,
            FontWeight    = 600,
            FontSize      = Length.Rem(0.9375),
            BoxSizing     = BrowserApi.Css.BoxSizing.BorderBox,
            Transition    = Transition.For("background", 120.Ms, Easing.Ease),

            [Self.Hover]         = new() { Background = Tokens.PrimaryDark },
            [Self.FocusVisible]  = new() { Outline    = Border.Solid(Length.Px(2), Tokens.Primary) },
            [Self.Disabled]      = new() {
                Cursor  = Cursor.NotAllowed,
                Opacity = 0.5,
            },
        };
    }

    [Fact]
    public void Button_renders_base_state() {
        var css = StyleSheet.Render<ButtonStyles>();
        // Diagnostic: keep the assertion error visible if it ever drifts.
        Xunit.Assert.True(css.Contains(".btn {"), "Expected `.btn {` in: " + css);
        Assert.Contains("display: inline-flex;", css);
        Xunit.Assert.True(css.Contains("padding: var(--spacing-sm) var(--spacing-lg)"),
            "Expected padding shorthand in: " + css);
        Assert.Contains("background: var(--primary)", css);
        Assert.Contains("border-radius: var(--radius)", css);
        Assert.Contains("transition: background 120ms ease", css);
    }

    [Fact]
    public void Button_hover_focus_disabled_render_as_separate_rules() {
        var css = StyleSheet.Render<ButtonStyles>();
        Assert.Contains(".btn:hover {", css);
        Assert.Contains(".btn:focus-visible {", css);
        Assert.Contains(".btn:disabled {", css);
        Assert.Contains("cursor: not-allowed;", css);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  Card — content surface with hover and dark mode
    // ═══════════════════════════════════════════════════════════════════════════════

    public class CardStyles : StyleSheet {
        public static readonly Class Card = new() {
            Padding      = Tokens.SpacingLg,
            Background   = Tokens.Surface,
            Color        = Tokens.Text,
            Border       = Border.Solid(Length.Px(1), Tokens.Border),
            BorderRadius = Tokens.Radius,
            BoxShadow    = new Shadow("0 1px 2px rgba(0,0,0,0.04)"),

            [Self.Hover] = new() {
                BoxShadow = new Shadow("0 4px 12px rgba(0,0,0,0.08)"),
            },

            [MediaQuery.PrefersDark] = new() {
                Background  = CssColor.Hex("#1a1a1a"),
                Color       = CssColor.Hex("#eaeaea"),
                BorderColor = CssColor.Hex("#333"),
            },
        };
    }

    [Fact]
    public void Card_dark_mode_emits_inside_at_media_block() {
        var css = StyleSheet.Render<CardStyles>();
        Assert.Contains("@media (prefers-color-scheme: dark) {", css);
        Assert.Contains("background: #1a1a1a;", css);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  Form — inputs, labels, fieldsets — using :where for low specificity
    // ═══════════════════════════════════════════════════════════════════════════════

    public class FormStyles : StyleSheet {
        public static readonly Rule InputReset = new(Where(El.Input, El.Textarea, El.Select)) {
            FontFamily = "inherit",
            FontSize   = Length.Em(1),
            Color      = Tokens.Text,
            Background = CssColor.White,
            Border     = Border.Solid(Length.Px(1), Tokens.Border),
            BorderRadius = Tokens.RadiusSmall,
            Padding    = (Tokens.SpacingSm, Tokens.SpacingMd),
            BoxSizing  = BrowserApi.Css.BoxSizing.BorderBox,
        };

        public static readonly Class FormGroup = new() {
            Display       = Display.Flex,
            FlexDirection = BrowserApi.Css.FlexDirection.Column,
            Gap           = Tokens.SpacingXs,
            MarginBottom  = Tokens.SpacingLg,
        };
    }

    [Fact]
    public void Form_reset_uses_where_for_zero_specificity() {
        var css = StyleSheet.Render<FormStyles>();
        Assert.Contains(":where(input, textarea, select) {", css);
        Assert.Contains("font-family: inherit;", css);
    }

    [Fact]
    public void Form_group_uses_flex_column_layout() {
        var css = StyleSheet.Render<FormStyles>();
        Assert.Contains(".form-group {", css);
        Assert.Contains("display: flex;", css);
        Assert.Contains("flex-direction: column;", css);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  Layout grid — responsive columns via @media
    // ═══════════════════════════════════════════════════════════════════════════════

    public class GridLayoutStyles : StyleSheet {
        public static readonly Class Grid = new() {
            Display = Display.Grid,
            GridTemplateColumns = "1fr",
            Gap = Tokens.SpacingLg,

            [MediaQuery.MinWidth(Length.Px(640))] = new() {
                GridTemplateColumns = "repeat(2, 1fr)",
            },

            [MediaQuery.MinWidth(Length.Px(1024))] = new() {
                GridTemplateColumns = "repeat(3, 1fr)",
            },
        };
    }

    [Fact]
    public void Grid_uses_breakpoints_via_at_media() {
        var css = StyleSheet.Render<GridLayoutStyles>();
        Assert.Contains(".grid {", css);
        Assert.Contains("grid-template-columns: 1fr;", css);
        Assert.Contains("@media (min-width: 640px) {", css);
        Assert.Contains("grid-template-columns: repeat(2, 1fr);", css);
        Assert.Contains("@media (min-width: 1024px) {", css);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  Animation — fade-in keyframes with reduced-motion guard
    // ═══════════════════════════════════════════════════════════════════════════════

    public class AnimationStyles : StyleSheet {
        public static readonly Keyframes FadeIn = new() {
            [From] = new() { Opacity = 0 },
            [To]   = new() { Opacity = 1 },
        };

        public static readonly Class Fading = new() {
            Animation = "fade-in 200ms ease-out",
            [MediaQuery.PrefersReducedMotion] = new() {
                Animation = "none",
            },
        };
    }

    [Fact]
    public void Animation_keyframes_emit_with_kebab_case_name() {
        var css = StyleSheet.Render<AnimationStyles>();
        Assert.Contains("@keyframes fade-in {", css);
        Assert.Contains("0% {", css);
        Assert.Contains("100% {", css);
    }

    [Fact]
    public void Animation_respects_reduced_motion_preference() {
        var css = StyleSheet.Render<AnimationStyles>();
        Assert.Contains("@media (prefers-reduced-motion: reduce) {", css);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  Container query — card layout switches at container width, not viewport
    // ═══════════════════════════════════════════════════════════════════════════════

    public class ContainerStyles : StyleSheet {
        public static readonly Class Wrapper = new() {
            ContainerType = "inline-size",
            ContainerName = "card",
        };

        public static readonly Class CardContent = new() {
            Display = Display.Flex,
            FlexDirection = BrowserApi.Css.FlexDirection.Column,

            [ContainerQuery.MinWidth(Length.Px(400))] = new() {
                FlexDirection = BrowserApi.Css.FlexDirection.Row,
            },
        };
    }

    [Fact]
    public void Container_query_emits_at_container_block() {
        var css = StyleSheet.Render<ContainerStyles>();
        Assert.Contains("container-type: inline-size;", css);
        Assert.Contains("@container (min-width: 400px) {", css);
        Assert.Contains("flex-direction: row;", css);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  @supports — progressive enhancement for native CSS nesting
    // ═══════════════════════════════════════════════════════════════════════════════

    public class SupportsStyles : StyleSheet {
        public static readonly Class Layout = new() {
            Display = Display.Block,
            [Supports.Grid] = new() { Display = Display.Grid },
        };
    }

    [Fact]
    public void Supports_grid_emits_at_supports_block() {
        var css = StyleSheet.Render<SupportsStyles>();
        Assert.Contains("@supports (display: grid) {", css);
        Assert.Contains("display: grid;", css);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  Important — forced specificity for utility-style classes
    // ═══════════════════════════════════════════════════════════════════════════════

    public class ImportantStyles : StyleSheet {
        public static readonly Class HiddenForced = new() {
            // !important on a Length value via the .Important property
            // (sets display via raw enum, opacity via Important Length wrapping)
            Opacity = 0,
            // padding via Length .Important — append the priority flag
        };

        public static readonly Class ZeroPadding = new() {
            Padding = Length.Px(0).Important,
        };
    }

    [Fact]
    public void Length_Important_appends_bang_important() {
        var css = StyleSheet.Render<ImportantStyles>();
        Assert.Contains("padding: 0px !important;", css);
    }
}
