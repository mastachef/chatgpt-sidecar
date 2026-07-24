using ChatGPT.Sidecar.Dock.Browser;
using Xunit;

namespace ChatGPT.Sidecar.Dock.Tests;

public sealed class ChatGptWebViewControllerTests
{
    [Fact]
    public void ParseComposerResult_ReadsStructuredProbeResult()
    {
        const string raw = "{\"success\":true,\"reason\":\"populated\",\"selector\":\"[data-testid='prompt-textarea']\",\"candidateCount\":3,\"elementTag\":\"div\"}";

        var result = ChatGptWebViewController.ParseComposerResult(raw);

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal("populated", result.Reason);
        Assert.Equal("[data-testid='prompt-textarea']", result.Selector);
        Assert.Equal(3, result.CandidateCount);
        Assert.Equal("div", result.ElementTag);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("not-json")]
    public void ParseComposerResult_RejectsMissingOrInvalidResults(string? raw)
    {
        Assert.Null(ChatGptWebViewController.ParseComposerResult(raw));
    }
}
