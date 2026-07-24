namespace ChatGPT.Sidecar.Dock.RepositoryContext;

internal sealed record RepositorySnapshot(
    string Root,
    string Status,
    string Diff,
    string StagedDiff,
    string RecentCommits,
    IReadOnlyDictionary<string, string> ProjectFiles);
