namespace BrowserApi.Tests.Css.Authoring;

/// <summary>
/// xUnit collection definition for tests that mutate <c>CssRegistry</c>'s
/// shared static state (most importantly, those that call
/// <c>CssRegistry.Refresh()</c>). Members of this collection run sequentially
/// so a registry refresh in one test doesn't pull the cached state out from
/// under a concurrently-running test elsewhere.
/// </summary>
[CollectionDefinition(nameof(CssRegistryCollection), DisableParallelization = true)]
public class CssRegistryCollection { }
