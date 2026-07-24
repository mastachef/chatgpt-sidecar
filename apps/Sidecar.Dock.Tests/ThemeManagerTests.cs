using ChatGPT.Sidecar.Dock.UI;
using Xunit;

namespace ChatGPT.Sidecar.Dock.Tests;

public sealed class ThemeManagerTests
{
    [Fact]
    public void ThemeCatalog_HasStableUniqueOptions()
    {
        var ids = ThemeManager.Options.Select(option => option.Id).ToArray();

        Assert.Equal(5, ids.Length);
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("codex-green", ids);
        Assert.Contains("codex-dark", ids);
        Assert.Contains("midnight", ids);
        Assert.Contains("light", ids);
        Assert.Contains("system", ids);
    }

    [Fact]
    public void PublishedAssemblyName_IsCleanSidecarName()
    {
        Assert.Equal("Sidecar", typeof(ThemeManager).Assembly.GetName().Name);
    }
}