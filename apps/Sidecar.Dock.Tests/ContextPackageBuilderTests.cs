using ChatGPT.Sidecar.Dock.CodexContext;
using ChatGPT.Sidecar.Dock.RepositoryContext;
using Xunit;

namespace ChatGPT.Sidecar.Dock.Tests;

public sealed class ContextPackageBuilderTests
{
    [Fact]
    public void Build_RedactsCommonCredentialFormats()
    {
        var session = new CodexSession(
            "rollout.jsonl",
            "session-1",
            "thread-1",
            "Test thread",
            "C:/repo",
            DateTimeOffset.UtcNow,
            false,
            [new CodexMessage("user", "Authorization: Bearer abcdefghijklmnopqrstuvwxyz", DateTimeOffset.UtcNow)]);
        var repository = new RepositorySnapshot(
            "C:/repo",
            "clean",
            "OPENAI_API_KEY=sk-abcdefghijklmnopqrstuvwxyz123456",
            "Password=super-secret;Server=localhost",
            "commit",
            new Dictionary<string, string>
            {
                ["package.json"] = "{\"token\": \"github_pat_should_not_escape\"}"
            },
            new Dictionary<string, string>
            {
                ["src/auth.cs"] = "var token = \"ghp_abcdefghijklmnopqrstuvwxyz123456\";"
            });

        var result = new ContextPackageBuilder().Build("review", "Check auth", session, repository);

        Assert.DoesNotContain("abcdefghijklmnopqrstuvwxyz123456", result, StringComparison.Ordinal);
        Assert.DoesNotContain("super-secret", result, StringComparison.Ordinal);
        Assert.DoesNotContain("Bearer abcdefghijklmnopqrstuvwxyz", result, StringComparison.Ordinal);
        Assert.Contains("[REDACTED", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_EnforcesOverallPackageLimit()
    {
        var hugeText = new string('x', 120_000);
        var session = new CodexSession(
            "rollout.jsonl",
            "session-1",
            "thread-1",
            "Large thread",
            "C:/repo",
            DateTimeOffset.UtcNow,
            false,
            [new CodexMessage("user", hugeText, DateTimeOffset.UtcNow)]);
        var repository = new RepositorySnapshot(
            "C:/repo",
            hugeText,
            hugeText,
            hugeText,
            hugeText,
            new Dictionary<string, string>(),
            new Dictionary<string, string>());

        var result = new ContextPackageBuilder().Build("plan", "Plan it", session, repository);

        Assert.True(result.Length <= ContextPackageBuilder.MaxPackageCharacters);
        Assert.Contains("truncated", result, StringComparison.OrdinalIgnoreCase);
    }
}
