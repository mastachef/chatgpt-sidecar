using ChatGPT.Sidecar.Dock.CodexContext;

namespace ChatGPT.Sidecar.Dock.Tests;

public sealed class CodexHandoffPromptBuilderTests
{
    [Fact]
    public void Build_IncludesProjectThreadRequestAndImplementationRequirements()
    {
        var session = new CodexSession(
            "rollout.jsonl",
            "session-1",
            "thread-1",
            "Repair the Hyperliquid backfill",
            @"C:\Projects\openchart",
            DateTimeOffset.UtcNow,
            false,
            Array.Empty<CodexMessage>());

        var prompt = CodexHandoffPromptBuilder.Build(session, "Choose the safest backfill architecture.");

        Assert.Contains("openchart", prompt);
        Assert.Contains("Repair the Hyperliquid backfill", prompt);
        Assert.Contains("Choose the safest backfill architecture", prompt);
        Assert.Contains("Exact files, classes, functions", prompt);
        Assert.Contains("Tests or validation", prompt);
        Assert.Contains("Output only the final prompt", prompt);
    }
}
