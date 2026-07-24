namespace ChatGPT.Sidecar.Dock.CodexContext;

internal sealed record CodexMessage(string Role, string Text, DateTimeOffset? Timestamp);

internal sealed record CodexSession(
    string RolloutPath,
    string? SessionId,
    string? ThreadId,
    string Title,
    string? WorkingDirectory,
    DateTimeOffset UpdatedAt,
    bool IsSubagent,
    IReadOnlyList<CodexMessage> Messages);
