using ChatGPT.Sidecar.Dock.Updates;

namespace ChatGPT.Sidecar.Dock.Tests;

public sealed class SidecarVersionTests
{
    [Theory]
    [InlineData("v0.8.1-alpha.7", "0.8.1-alpha.8")]
    [InlineData("0.8.1-alpha.8", "0.8.1-beta.1")]
    [InlineData("0.8.1-beta.4", "0.8.1-rc.1")]
    [InlineData("0.8.1-rc.2", "0.8.1")]
    [InlineData("0.8.1", "0.8.2-alpha.1")]
    [InlineData("0.9.9", "1.0.0")]
    public void Later_versions_compare_greater(string olderText, string newerText)
    {
        Assert.True(SidecarVersion.TryParse(olderText, out var older));
        Assert.True(SidecarVersion.TryParse(newerText, out var newer));
        Assert.True(newer.CompareTo(older) > 0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("v1")]
    [InlineData("1.2")]
    [InlineData("hello")]
    [InlineData("1.2.x")]
    public void Invalid_versions_are_rejected(string text)
    {
        Assert.False(SidecarVersion.TryParse(text, out _));
    }
}
