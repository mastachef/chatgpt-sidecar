# Audit report — July 23, 2026

## Verdict

**Conditional pass after the version 0.2 hardening changes.** The handoff flow is internally consistent, covered by automated tests, and passed an end-to-end process simulation. A final live validation is still required in the Windows ChatGPT desktop/Codex environment because that exact application runtime is not available in the audit container.

## Confirmed design correction

The original rough draft used custom slash commands such as `/gpt-plan`. Current Codex rejects unknown slash commands before they can reach a `UserPromptSubmit` hook. Version 0.2 therefore uses normal prompt prefixes:

```text
gpt: <request>
gpt-plan: <request>
gpt-debug: <request>
gpt-review:
```

These are ordinary prompts, so the hook can intercept and block them before a Codex model turn begins.

## Verified

- The plugin manifest and marketplace JSON parse successfully.
- The hook is discovered from the conventional `hooks/hooks.json` path.
- The hook receives every submitted prompt and exits without output for non-sidecar prompts.
- A recognized sidecar prompt returns `decision: block`.
- The App Server client waits for the `initialize` response before sending `initialized` and `thread/read`.
- Repository collection includes Git branch, status, diff, staged diff, recent commits, tracked files, and selected project files.
- Repository file paths are checked with path containment rather than string-prefix matching.
- Generated handoffs have per-section and total size caps.
- Common credentials are redacted on a best-effort basis.
- Clipboard and browser launching degrade gracefully when unavailable.
- The Windows hook command uses `${PLUGIN_ROOT}`, matching Codex's plugin-root expansion behavior.

## Automated checks

The audit suite covers command parsing, handoff generation, App Server message ordering and failure handling, credential redaction, and plugin metadata. A separate process-level simulation exercised the real hook entrypoint with:

- a fake Codex App Server,
- a real temporary Git repository,
- fake clipboard and browser commands,
- a planted API key,
- and a normal non-sidecar prompt.

The sidecar prompt was blocked, the stored thread and repository state appeared in the handoff, the planted key was redacted, and the normal prompt passed through without hook output.

## Remaining live checks

1. Install the marketplace and plugin in the target Windows ChatGPT desktop/Codex release.
2. Trust the plugin hook through `/hooks`.
3. Confirm Node.js 20+ is visible in the desktop app's hook `PATH`.
4. Run `$sidecar plan summarize this project and propose one harmless README change` in a disposable repository.
5. Confirm the active thread is fresh, the handoff reaches the clipboard, ChatGPT opens, and Codex reports that the turn was blocked.

## Known limitations

- The consumer ChatGPT handoff is clipboard-based; it cannot silently submit into a chosen ChatGPT conversation through a documented consumer API.
- Secret redaction is defensive and best-effort, not a substitute for repository hygiene.
- Large repositories use a compact inventory and selected files rather than sending the full codebase.
- The plugin requires an external Node.js 20+ installation.

## Version 0.3 command update

Codex does not support plugin-defined literal `/sidecar` commands. Version 0.3 therefore uses the officially supported bundled skill `$sidecar`, plus an optional deprecated custom-prompt wrapper at `/prompts:sidecar`. The parser retains `/sidecar` for future compatibility, but the README does not claim current clients can execute it.
