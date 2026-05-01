using BrowserApi.Css;
using BrowserApi.Css.Authoring;
using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;

namespace BrowserApi.Tests.Css.Authoring;

/// <summary>
/// Tests for the C# 14 extension-property approach to <c>.Important</c> on
/// CSS keyword enums (spec §14). Both the bare enum value and the
/// <c>.Important</c>-wrapped value flow through the same property setter via
/// implicit conversion to <see cref="Keyword{TEnum}"/>.
/// </summary>
[Collection(nameof(CssRegistryCollection))]
public class EnumImportantTests {
    private class ImportantStyles : StyleSheet {
        public static readonly Class Bare = new() {
            Display = Display.Flex,
        };

        public static readonly Class Forced = new() {
            Display = Display.None.Important,
        };

        public static readonly Class Mixed = new() {
            Display  = Display.Grid,
            Position = Position.Absolute.Important,
            Cursor   = Cursor.Pointer,
            BoxSizing = BrowserApi.Css.BoxSizing.BorderBox.Important,
        };
    }

    [Fact]
    public void Bare_enum_emits_keyword_without_priority() {
        var css = StyleSheet.Render<ImportantStyles>();
        Assert.Contains(".bare { display: flex; }", css);
    }

    [Fact]
    public void Important_property_appends_bang_important() {
        var css = StyleSheet.Render<ImportantStyles>();
        Assert.Contains(".forced { display: none !important; }", css);
    }

    [Fact]
    public void Mixed_priorities_within_one_block_render_correctly() {
        var css = StyleSheet.Render<ImportantStyles>();
        Assert.Contains("display: grid;", css);
        Assert.Contains("position: absolute !important;", css);
        Assert.Contains("cursor: pointer;", css);
        // CSSOM enum (BoxSizing carries [StringValue("border-box")]) — Important works the same way:
        Assert.Contains("box-sizing: border-box !important;", css);
    }

    [Fact]
    public void Important_works_on_runtime_keyword_too() {
        // The extension property is generic over TEnum and applies to any enum,
        // including CSSOM-generated ones (BoxSizing, Visibility, etc.).
        var key = BrowserApi.Css.Visibility.Hidden.Important;
        Assert.Equal("hidden !important", key.ToCss());
    }
}
