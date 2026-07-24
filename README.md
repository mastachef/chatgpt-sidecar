![Sidecar](https://i.imgur.com/Nki8DF1.png)

# Sidecar

**Preserve precious Codex usage by offloading planning, debugging, code review, and repository analysis to ChatGPT.**

Sidecar is a native Windows companion for the ChatGPT/Codex desktop app. It attaches beside the exact Codex window you choose, reads a selected saved Codex conversation and repository context locally, and prepares that context inside an embedded ChatGPT session—without sending another prompt to the Codex thread.

> **Current release: v0.8.1-alpha.4**

## Why use it?

Codex usage is most valuable when Codex is implementing, testing, and repairing code. Sidecar lets ChatGPT handle thinking-heavy work such as:

- planning an implementation
- comparing architectures
- investigating a bug
- reviewing current changes
- summarizing a long Codex thread
- analyzing repository structure and Git diffs

The main Codex conversation remains untouched until you are ready to continue implementation there.

## Current features

- **Drag-to-attach docking:** drag the Attach control onto the real Codex window and Sidecar pins itself to that exact window.
- **Persistent ChatGPT session:** sign in once through the embedded WebView2 browser.
- **Codex context reader:** choose from recent saved root Codex conversations; subagent rollouts are excluded.
- **Repository context:** bounded Git status, staged and unstaged diffs, recent commits, instructions, manifests, and referenced files.
- **Context preview:** inspect exactly what will be placed into ChatGPT before sending.
- **Secret protection:** blocks sensitive file categories and redacts common credentials and tokens on a best-effort basis.
- **Codex-style themes:** Codex Green, Codex Dark, Midnight, Light, and System.
- **Clean Windows app:** single-file `Sidecar.exe` with the Sidecar car icon.
- **Privacy-safe diagnostics:** startup and browser diagnostics exclude conversation and repository contents.

## Download and run

Download the latest public release from **[GitHub Releases](https://github.com/mastachef/chatgpt-sidecar/releases/latest)**.

1. Download `Sidecar.exe` and `README-FIRST.txt`.
2. Put both files in a normal folder.
3. Run `Sidecar.exe`.
4. Sign into ChatGPT inside Sidecar when prompted.
5. Hold **Attach**, drag it over the real Codex window, and release.
6. Choose the correct Codex thread.
7. Enter a Plan, Debug, Review, or General request.
8. Select **Preview** to inspect the prepared context.
9. Select **Prepare in ChatGPT**, review the populated message, and send it.

Sidecar currently populates the ChatGPT composer but does **not** auto-submit. That is intentional while the embedded workflow remains in alpha.

## Themes

Choose a theme from the Sidecar header. The choice is saved automatically.

| Theme | Appearance |
|---|---|
| Codex Green | Near-black surfaces with muted terminal-green text and accents |
| Codex Dark | Neutral charcoal Codex-style interface |
| Midnight | Deep navy and violet styling inspired by the Sidecar artwork |
| Light | Bright high-contrast interface |
| System | Uses the Windows app light/dark preference |

The theme changes Sidecar's native shell. The embedded ChatGPT page controls its own appearance.

## Requirements

- Windows 10 or Windows 11, 64-bit
- ChatGPT/Codex desktop app with at least one saved Codex conversation
- Microsoft Edge WebView2 Runtime
- Git available for repository status and diff collection

The release is self-contained for .NET; installing the .NET SDK is not required.

## Privacy and safety

Sidecar reads local Codex rollout files and selected repository content. Before anything is placed into ChatGPT:

- the selected thread is shown explicitly
- context size is bounded
- sensitive paths such as `.env`, credentials, keys, dependency folders, and build output are excluded by default
- common token and credential patterns are redacted on a best-effort basis
- the complete prepared context can be previewed

Nothing is typed into the Codex composer, and Sidecar does not resume or submit to Codex threads.

## Alpha notes

This is an unsigned early Windows build. SmartScreen may show a warning. Live WebView2 login, docking behavior, and ChatGPT composer compatibility are still being validated across different Windows and ChatGPT app versions.

Should Sidecar fail to start, the startup report is stored at:

```text
%LOCALAPPDATA%\ChatGPTSidecar\Diagnostics\startup-crash.log
```

In-app diagnostics are stored at:

```text
%LOCALAPPDATA%\ChatGPTSidecar\Diagnostics\sidecar-dock.log
```

## Development

```powershell
npm test
npm run dock:test
npm run dock:publish
```

The Windows workflow builds and tests the native app, publishes a self-contained `Sidecar.exe`, verifies the embedded icon, and launches the packaged executable in startup-smoke mode before producing an artifact.

## License

MIT
