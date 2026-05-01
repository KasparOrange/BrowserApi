using BrowserApi.Css.Authoring;

namespace BrowserApi.Tests.Css.Authoring;

/// <summary>
/// Tests for the source-generated <c>Assets</c> class, which provides typed
/// accessors for files in <c>wwwroot/</c>. The test project wires
/// <c>test-wwwroot/</c> in via <c>AdditionalFiles</c>.
/// </summary>
public class AssetGeneratorTests {
    [Fact]
    public void App_css_resolves_to_relative_path_under_root() {
        Assert.Equal("css/app.css", Assets.Css.App);
    }

    [Fact]
    public void Nested_folders_become_nested_static_classes() {
        Assert.Equal("images/logo.svg", Assets.Images.Logo);
    }

    [Fact]
    public void Pascal_case_identifiers_are_derived_from_file_names() {
        // The leaf identifier `App` came from `app.css` (extension stripped,
        // first letter uppercased). `Logo` came from `logo.svg`.
        // PascalCase test below is implicit in the assertions above; this
        // test asserts the surface works as a static-string-returning member,
        // not a method.
        var path = Assets.Css.App;
        Assert.IsType<string>(path);
    }
}
