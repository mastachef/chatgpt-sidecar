# Architecture

Sidecar is a native WPF application. The old hook/Node/MCP prototype is not part of the current runtime.

## Runtime flow

1. **Manual attach** — the user drags the Attach target onto a specific Codex window. Sidecar stores that exact native window handle and follows it until detached or the target disappears.
2. **Saved Codex context** — `CodexSessionReader` reads saved root Codex conversations from the local Codex data directory. Worker/subagent rollouts are excluded from the normal thread picker.
3. **Repository context** — `RepositoryContextCollector` gathers bounded Git status, staged/unstaged diffs, recent commits, selected project files, and files referenced by the conversation.
4. **Safety filtering** — path containment, ignored sensitive categories, size limits, and `SecretRedactor` reduce the chance of accidental credential exposure.
5. **Preview** — the complete payload can be inspected before it is prepared for ChatGPT.
6. **ChatGPT handoff** — `ChatGptWebViewController` hosts ChatGPT in WebView2 with a persistent user-data directory. Sidecar populates the visible composer but does not press Send.
7. **Return to Codex** — Sidecar prepares a self-contained implementation-handoff request and can copy the completed ChatGPT reply to the clipboard for the user to paste into Codex.

## Native application

`apps/Sidecar.Dock` contains the production application:

- `MainWindow.*` — shell, workflow controls, handoff, updater, theme, and window chrome behavior.
- `Docking/` and `WindowDetection/` — exact-window selection and attachment tracking.
- `CodexContext/` — saved conversation selection and context packaging.
- `RepositoryContext/` — bounded repository/Git context and referenced-file collection.
- `ChatGPT/` — WebView2 navigation, persistent session, composer population, and latest-reply extraction.
- `Security/` — credential/token redaction.
- `Diagnostics/` — privacy-safe startup/runtime diagnostics.
- `Updates/` — GitHub Release discovery, version comparison, integrity/signature verification, replacement, and restart.
- `UI/` — themes and Windows DWM/native-frame synchronization.

`apps/Sidecar.Dock.Tests` contains the native automated test suite.

## Distribution

### GitHub Releases

The GitHub build is a self-contained x64 single-file executable. Public binaries are gated behind trusted Authenticode signing. The release exposes:

- `Sidecar-Portable-win-x64.exe` for manual downloads;
- `Sidecar.exe` as the canonical asset consumed by the in-app updater.

They are byte-for-byte identical after signing.

### Microsoft Store

The Store distribution will use MSIX. Store packaging is intentionally separate from the portable GitHub build so Microsoft can manage installation and updates without changing the portable channel.

## Update trust model

The updater does not install an arbitrary URL. It requires a newer GitHub Release with a canonical `Sidecar.exe` asset, verifies GitHub's SHA-256 digest locally, requires a valid Windows Authenticode signature, and checks publisher continuity when the running build is signed. Replacement occurs only after verification succeeds.

## Privacy boundary

Conversation and repository content remain local until the user chooses to prepare a message for ChatGPT. Diagnostics intentionally exclude conversation text, repository file contents, diffs, and ChatGPT message contents.
