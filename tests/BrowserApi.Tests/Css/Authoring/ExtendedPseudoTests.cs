using BrowserApi.Css.Authoring;

namespace BrowserApi.Tests.Css.Authoring;

/// <summary>Tests for the expanded pseudo-class surface (form states,
/// link states, language, target, etc.).</summary>
public class ExtendedPseudoTests {
    private static readonly Selector A = new(".a");

    [Fact]
    public void Form_state_pseudos() {
        Assert.Equal(".a:required", A.Required.Css);
        Assert.Equal(".a:optional", A.Optional.Css);
        Assert.Equal(".a:valid",    A.Valid.Css);
        Assert.Equal(".a:invalid",  A.Invalid.Css);
        Assert.Equal(".a:read-only",  A.ReadOnly.Css);
        Assert.Equal(".a:read-write", A.ReadWrite.Css);
        Assert.Equal(".a:placeholder-shown", A.PlaceholderShown.Css);
    }

    [Fact]
    public void Link_state_pseudos() {
        Assert.Equal(".a:visited",  A.Visited.Css);
        Assert.Equal(".a:link",     A.Link.Css);
        Assert.Equal(".a:any-link", A.AnyLink.Css);
    }

    [Fact]
    public void Functional_pseudos() {
        Assert.Equal(".a:lang(en)",  A.Lang("en").Css);
        Assert.Equal(".a:dir(rtl)",  A.Dir("rtl").Css);
        Assert.Equal(".a:nth-of-type(2n+1)", A.NthOfType("2n+1").Css);
        Assert.Equal(".a:nth-last-child(odd)", A.NthLastChild("odd").Css);
    }

    [Fact]
    public void Postfix_is_and_where_attach_correctly() {
        var b = new Selector(".b");
        var c = new Selector(".c");
        Assert.Equal(".a:where(.b, .c)", A.Where(b, c).Css);
        Assert.Equal(".a:is(.b, .c)",    A.Is(b, c).Css);
    }

    [Fact]
    public void Empty_target_default_pseudos() {
        Assert.Equal(".a:empty",   A.Empty.Css);
        Assert.Equal(".a:target",  A.Target.Css);
        Assert.Equal(".a:default", A.Default.Css);
    }
}
