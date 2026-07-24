using System.IO;

namespace ChatGPT.Sidecar.Dock.CodexContext;

internal static class CodexHandoffPromptBuilder
{
    public static string Build(CodexSession? session, string? originalRequest)
    {
        var project = ProjectName(session);
        var thread = string.IsNullOrWhiteSpace(session?.Title)
            ? "Unknown Codex thread"
            : session!.Title.Trim();
        var request = string.IsNullOrWhiteSpace(originalRequest)
            ? "No separate Sidecar request was recorded. Infer the completed task from this ChatGPT conversation."
            : originalRequest.Trim();

        return $$"""
        Prepare a detailed, self-contained handoff prompt that I can paste directly into Codex to continue this exact project after the work completed in this ChatGPT conversation.

        Use the entire current ChatGPT conversation, including the original Codex context, repository details, decisions, debugging, research, and conclusions already discussed. Do not repeat exploratory reasoning that has already been resolved. Convert the finished work into precise implementation instructions for Codex.

        Selected project: {{project}}
        Selected Codex thread: {{thread}}
        Original Sidecar request: {{request}}

        The handoff prompt must include, whenever applicable:

        1. The exact objective Codex should continue from.
        2. A concise summary of what was investigated or completed in ChatGPT.
        3. Decisions already made and the reasons that matter for implementation.
        4. The current repository or implementation state.
        5. Exact files, classes, functions, components, routes, schemas, or commands Codex should inspect or modify.
        6. Concrete implementation steps in the correct order.
        7. Constraints, invariants, compatibility requirements, privacy or security rules, and things Codex must not change.
        8. Relevant error messages, reproduction details, edge cases, and known failure modes.
        9. Tests or validation already performed, tests still required, and clear pass conditions.
        10. Any unresolved questions or assumptions that Codex must verify from the repository before editing.
        11. A clear first action for Codex to take immediately.

        Tell Codex to inspect the current repository and git diff before making changes, preserve valid work already present, and continue rather than restart the analysis. Include code snippets or commands only when they materially help implementation.

        Output only the final prompt to paste into Codex. Do not add an introduction, explanation, Markdown code fence, or commentary outside the prompt.
        """;
    }

    private static string ProjectName(CodexSession? session)
    {
        if (string.IsNullOrWhiteSpace(session?.WorkingDirectory))
        {
            return "Unknown project";
        }

        var normalized = session.WorkingDirectory.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
        return Path.GetFileName(normalized) is { Length: > 0 } name
            ? name
            : normalized;
    }
}
