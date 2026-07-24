namespace ChatGPT.Sidecar.Dock.Browser;

internal sealed record AssistantMessageResult(
    bool Success,
    string Reason,
    string? Text = null,
    string? Selector = null,
    int CandidateCount = 0)
{
    public static AssistantMessageResult NotReady(string reason) => new(false, reason);
}
