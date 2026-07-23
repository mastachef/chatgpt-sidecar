# Audit report — July 23, 2026

## Verdict

**Conditional pass after the version 0.2 hardening changes.** The handoff flow is internally consistent, covered by automated tests, and passed an end-to-end process simulation. A final live validation is still required in the Windows ChatGPT desktop/Codex environment because that exact application runtime is not available in the audit container.

## Confirmed design correction

The original draft used `/gpt-plan`. Current Codex rejects unknown slash commands before they reach a `UserPromptSubmit` hook. Version 0.2 therefore uses normal prompt prefixes:

```text
gpt: <request>
gpt-plan: <request>
gpt-debug: <request>
gpt-review:
```

## Verified

- The plugin and marketplace metadata parse successfully.
- The hook is discovered from `hooks/hooks.json`.
- Non-sidecar prompts exit without output.
- Recognized prompts return `decision: block`.
- App Server initialization is ordered before `thread/read`.
- Git context, selected files, size limits, and best-effort credential redaction are applied.
- The Windows hook command uses `${PLUGIN_ROOT}`.

## Automated checks

The audit suite covers command parsing, handoff generation, App Server message ordering and failure handling, credential redaction, and plugin metadata. A process-level simulation used a fake Codex App Server, real temporary Git repository, fake clipboard/browser commands, and a planted API key. The sidecar prompt was blocked, context appeared in the handoff, the key was redacted, and a normal prompt passed through without output.

## Remaining live checks

1. Install the marketplace and plugin in the target Windows Codex release.
2. Trust the plugin hook through `/hooks`.
3. Confirm Node.js 20+ is visible in the hook `PATH`.
4. Run `gpt-plan: summarize this project and propose one harmless README change` in a disposable repository.
5. Confirm fresh thread context, clipboard output, ChatGPT launch, and a blocked Codex turn.

## Known limitations

- Consumer ChatGPT handoff is clipboard-based.
- Secret redaction is best-effort.
- Large repositories use compact context rather than full source.
- Node.js 20+ is an external prerequisite.
