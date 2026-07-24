![Sidecar](https://i.imgur.com/Nki8DF1.png)

# Sidecar

**Preserve precious Codex usage by offloading planning, debugging, code review, and repository analysis to ChatGPT—then return the finished work to Codex as a detailed implementation handoff.**

Sidecar is a native Windows companion for the ChatGPT/Codex desktop app. It stays put until you manually attach it to the exact Codex window you choose, reads a selected saved Codex conversation and bounded repository context locally, and prepares that context inside an embedded ChatGPT session without submitting another prompt to the Codex thread.

> **Current source version: v0.8.1-alpha.8**

## Screenshots

### Codex + Sidecar

<p align="center">
  <img src="https://i.imgur.com/TE1iGrK.png" width="100%" alt="Codex with Sidecar attached" />
</p>

<p align="center"><em>Sidecar attached beside Codex while working from the same project and saved conversation context.</em></p>

### Sidecar

<p align="center">
  <img src="https://i.imgur.com/tTH2qWz.png" width="520" alt="Sidecar Windows application" />
</p>

### Theme examples

<p align="center">
  <img src="https://i.imgur.com/HADIHwh.png" width="48%" alt="Sidecar theme example 1" />
  <img src="https://i.imgur.com/u0EDuHg.png" width="48%" alt="Sidecar theme example 2" />
</p>

<p align="center">
  <img src="https://i.imgur.com/PWcAuQu.png" width="48%" alt="Sidecar theme example 3" />
  <img src="https://i.imgur.com/C8AjkwS.png" width="48%" alt="Sidecar theme example 4" />
</p>

## The workflow

```text
Codex context → Sidecar/ChatGPT planning → detailed Codex handoff → Codex implementation
```

1. Open the Codex project and conversation you are working on.
2. Run `Sidecar.exe` and sign into ChatGPT inside Sidecar.
3. Drag **Attach** over the exact Codex window and release.
4. Select the correct saved root Codex thread.
5. Enter a Plan, Debug, Review, or General request.
6. Select **Preview**, then **Prepare in ChatGPT**, review the populated message, and send it.
7. After the ChatGPT work is complete, select **Prepare handoff**.
8. Send the handoff request in ChatGPT.
9. When ChatGPT finishes, select **Copy latest reply** and paste that detailed prompt into Codex.

Sidecar populates prompts but does **not** auto-submit them.

## Current features

- **Manual-only docking:** Sidecar never guesses or auto-selects a window. It moves only after you drag **Attach** onto a specific window.
- **Persistent ChatGPT session:** sign in once through the embedded WebView2 browser.
- **Codex context reader:** choose recent saved root Codex conversations; subagent rollouts are excluded.
- **Repository context:** bounded Git status, staged and unstaged diffs, recent commits, instructions, manifests, and referenced files.
- **Context preview:** inspect exactly what will be placed into ChatGPT.
- **Return-to-Codex handoff:** asks ChatGPT for a self-contained implementation prompt covering decisions, files, steps, constraints, errors, tests, unresolved questions, and the next action.
- **Copy latest reply:** copies the completed ChatGPT handoff directly to the clipboard.
- **Built-in updater:** Sidecar checks GitHub Releases at startup and exposes an **Updates** button. New builds are downloaded and installed from inside Sidecar instead of requiring another manual EXE download.
- **Verified updates:** before replacement, Sidecar verifies the GitHub release asset SHA-256 digest and requires a valid trusted Authenticode signature. If the running build is already signed, the update must be signed by the same publisher.
- **Secret protection:** excludes sensitive file categories and redacts common credentials and tokens on a best-effort basis.
- **Codex-style themes:** Codex Green, Codex Dark, Midnight, Light, and System.
- **Fully themed window chrome:** the title bar, app icon, title text, minimize/maximize/close controls, cards, dropdowns, and footer all follow the selected Sidecar theme.
- **Readable themed controls:** dropdown selections and popup items use explicit theme-aware text and backgrounds.
- **Clean Windows app:** self-contained `Sidecar.exe` using the supplied chrome-car artwork.
- **Privacy-safe diagnostics:** startup and browser diagnostics exclude conversation and repository contents.

## Download

Download the latest release from **[GitHub Releases](https://github.com/mastachef/chatgpt-sidecar/releases/latest)**.

The public release contains only:

- `Sidecar.exe`
- `README-FIRST.txt`

Put both files in a normal user-writable folder and run `Sidecar.exe`. After that first install, Sidecar can pull newer signed releases from GitHub using its built-in updater.

### Code signing policy

Every public `Sidecar.exe` must be produced by the repository's automated release build and carry a valid trusted Authenticode signature before it can be published. If signing is unavailable or verification fails, the release workflow stops instead of publishing an unsigned executable. See **[Sidecar code signing policy](docs/CODE_SIGNING.md)** for the full policy and SignPath/Microsoft setup.

## Updates

Sidecar quietly checks recent GitHub Releases when it starts. You can also use the **Updates** button in the header to check manually.

When a newer release is available, Sidecar:

1. downloads the published `Sidecar.exe` to a staging folder under `%LOCALAPPDATA%\ChatGPTSidecar\Updates`
2. verifies the SHA-256 digest reported by GitHub for that release asset
3. requires Windows to report a valid trusted Authenticode signature on the downloaded executable
4. requires the same signer as the currently running build when the current build is already signed
5. closes Sidecar, swaps the executable with rollback protection, and restarts the updated copy

The updater will not replace the running executable when integrity or signature validation fails.

## Themes

Choose a theme from the Sidecar header. The choice is saved automatically and applies to the entire native Sidecar shell, including the custom title bar.

| Theme | Appearance |
|---|---|
| Codex Green | Near-black surfaces with muted terminal-green text and accents |
| Codex Dark | Neutral charcoal Codex-style interface |
| Midnight | Deep navy and violet styling inspired by the Sidecar artwork |
| Light | Bright high-contrast interface |
| System | Uses the Windows app light/dark preference |

The embedded ChatGPT page controls its own appearance separately.

## Requirements

- Windows 10 or Windows 11, 64-bit
- ChatGPT/Codex desktop app with at least one saved Codex conversation
- Microsoft Edge WebView2 Runtime
- Git available for repository status and diff collection

The release is self-contained for .NET; the .NET SDK is not required.

## Privacy and safety

Before anything is placed into ChatGPT:

- the selected Codex thread is shown explicitly
- context size is bounded
- `.env`, credential, key, dependency, build-output, traversal, and out-of-repository paths are excluded by default
- common token and credential patterns are redacted on a best-effort basis
- the complete prepared context can be previewed

Nothing is typed into the Codex composer, and Sidecar does not resume or submit to Codex threads. The return handoff is copied to the clipboard for the user to review and paste manually.

## Diagnostics

```text
Startup: %LOCALAPPDATA%\ChatGPTSidecar\Diagnostics\startup-crash.log
Runtime: %LOCALAPPDATA%\ChatGPTSidecar\Diagnostics\sidecar-dock.log
```

A trusted signature removes the **Unknown publisher** label; SmartScreen reputation is a separate Windows signal for direct downloads.

## Development

```powershell
npm test
npm run dock:test
npm run dock:publish
```

The Windows workflow builds and tests the native app, publishes a self-contained `Sidecar.exe`, verifies the embedded icon, requires a trusted signing provider, verifies the Authenticode signature, and only then produces a public release.

## License

MIT
