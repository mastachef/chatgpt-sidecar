namespace ChatGPT.Sidecar.Dock.Browser;

internal sealed record ComposerPopulateResult(
    bool Success,
    string Reason,
    string? Selector = null,
    int CandidateCount = 0,
    string? ElementTag = null)
{
    public static ComposerPopulateResult NotReady(string reason) => new(false, reason);
}
