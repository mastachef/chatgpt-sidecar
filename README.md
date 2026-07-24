![Sidecar](https://i.imgur.com/Tnpg6pn.png)

# Sidecar

**Preserve precious Codex token usage by offloading planning, debugging, review, and repository analysis to ChatGPT—without leaving your Codex workspace.**

Sidecar is a native Windows companion panel that attaches to the exact ChatGPT/Codex desktop window you select, reads a chosen saved Codex conversation and its repository locally, and prepares that context inside an embedded ChatGPT session without submitting another prompt to Codex.

> **Current status: v0.8.1-alpha.4.** The Windows alpha uses drag-to-attach exact window targeting, crash-safe startup, a clean `Sidecar.exe`, branded car artwork, and persistent Codex-inspired themes.

## Why Sidecar

Codex usage is most valuable when it is writing, testing, and repairing code. Planning discussions, architecture comparisons, debugging research, repository summaries, and review can consume that same limited usage before implementation even begins.

Sidecar gives ChatGPT the relevant Codex conversation, Git state, diffs, instructions, and referenced files so those thinking-heavy tasks can happen beside Codex while the main Codex thread remains untouched.

## What the alpha does

- Provides an **Attach** handle that can be dragged over the real Codex window and released.
- Pins Sidecar to that exact Windows window handle and follows its movement and resizing.
- Keeps automatic Codex detection as an optional fallback through **Auto**.
- Includes persistent **Codex Green**, **Codex Dark**, **Midnight**, **Light**, and **System** themes.
- Publishes a clean, branded `Sidecar.exe` with the chrome car application icon.
- Hosts `chatgpt.com` in a persistent WebView2 profile.
- Lists recent saved **root** Codex threads and excludes subagent rollouts.
- Lets the user explicitly select the conversation to share.
- Collects bounded Git status, staged and unstaged diffs, recent commits, `AGENTS.md`, README, and common manifests.
- Detects safe repository files referenced in the selected Codex conversation.
- Blocks `.env`, credential, key, build-output, dependency, traversal, and out-of-repository paths.
- Redacts common private keys, bearer tokens, API keys, GitHub tokens, AWS keys, passwords, and credential URLs.
- Builds a previewable, size-limited context package.
- Populates the ChatGPT composer for the user to review and send.
- Falls back to the clipboard when the composer cannot be safely identified.
- Records startup, WebView runtime, navigation, process, and composer-probe diagnostics without logging context contents.

The alpha deliberately does **not** auto-submit. Populate-only behavior remains the safety gate until the embedded ChatGPT workflow proves stable.

## Run the downloaded Windows alpha

1. Extract the entire ZIP to a normal folder. Do not run the EXE from inside Windows' compressed-folder preview.
2. Open the extracted folder.
3. Run `Sidecar.exe`.
4. Hold the **Attach** handle, drag the cursor over the actual Codex window, and release.
5. Confirm the target label changes to the selected window title and Sidecar snaps beside it.
6. Pick a theme from the header to match your Codex setup. The choice is saved automatically.
7. Use **Auto** only when you want to return to automatic window detection.
8. If the process appears to do nothing, run `START_SIDECAR.cmd`. It shows the exit code and opens the startup report automatically.

The startup report is stored at:

```text
%LOCALAPPDATA%\ChatGPTSidecar\Diagnostics\startup-crash.log
```

The published EXE is self-contained for .NET. Microsoft Edge WebView2 Runtime is still required for the embedded ChatGPT browser.

## Run from source

Requirements:

- Windows 10 or 11
- .NET 8 SDK
- Microsoft Edge WebView2 Runtime
- Git
- At least one saved Codex session

```powershell
cd $HOME\chatgpt-sidecar
git pull
dotnet run --project .\apps\Sidecar.Dock\Sidecar.Dock.csproj
```

On first launch, sign into ChatGPT inside the Sidecar panel. The browser profile is stored under:

```text
%LOCALAPPDATA%\ChatGPTSidecar\WebView2Profile
```

## Workflow

1. Open the Codex project and conversation you are working on.
2. Launch **Sidecar**.
3. Drag **Attach** over the real Codex window and release.
4. Sidecar pins itself to that exact window and follows it.
5. Choose the correct recent root Codex thread from the thread picker.
6. Enter a planning, debugging, review, or general request.
7. Click **Preview** to inspect exactly what will be provided.
8. Click **Prepare in ChatGPT**.
9. Review the populated ChatGPT composer and press Send.

Nothing is typed into the Codex composer and no Codex thread is resumed or started.

## Themes

- **Codex Green:** near-black background, muted green cards, and terminal-green text modeled after the Codex setup shown during development.
- **Codex Dark:** neutral charcoal Codex-style interface.
- **Midnight:** dark navy and violet styling that complements the Sidecar car artwork.
- **Light:** bright high-contrast interface.
- **System:** follows Windows' light/dark app preference.

The selected theme is stored under `%LOCALAPPDATA%\ChatGPTSidecar\Settings\ui.json`.

## Window targeting behavior

Manual targeting is authoritative:

- **Attach drag succeeds:** Sidecar follows only the selected window handle.
- **Selected window is minimized:** Sidecar waits for it to be restored.
- **Selected window closes:** Sidecar stops following and asks for another Attach drag rather than jumping to a random application.
- **Auto:** clears the manual pin and re-enables heuristic Codex detection.
- **Follow:** pauses or resumes movement without changing the selected target.

## Live validation and diagnostics

Follow [`docs/live-validation.md`](docs/live-validation.md) to validate docking, login persistence, thread selection, privacy, and composer population.

Click **Diagnostics** after any in-app failure. The copied report includes:

- app, .NET, Windows, and architecture versions
- WebView2 runtime version
- sanitized route categories such as `/auth/*` or `/c/*`
- WebView process failures
- the composer selector, candidate count, and failure reason

It does **not** include the Codex conversation, request, repository contents, Git diffs, ChatGPT messages, or full conversation URLs.

The rolling in-app log is stored at:

```text
%LOCALAPPDATA%\ChatGPTSidecar\Diagnostics\sidecar-dock.log
```

## Native project structure

```text
apps/
├── Sidecar.Dock/
│   ├── Assets/               Branded Windows application icon
│   ├── ChatGPT/              Persistent WebView2 host and composer adapter
│   ├── CodexContext/         Rollout parser, thread list, and package builder
│   ├── Diagnostics/          Startup and privacy-bounded runtime diagnostics
│   ├── Docking/              Manual and automatic magnetic docking controller
│   ├── Interop/              Win32 window and cursor APIs
│   ├── RepositoryContext/    Git, manifests, and referenced-file collection
│   ├── Security/             Credential redaction
│   ├── UI/                   Runtime theme system and saved UI preferences
│   ├── WindowDetection/      Automatic locator and drag target picker
│   ├── App.xaml
│   ├── MainWindow.xaml
│   └── Sidecar.Dock.csproj
└── Sidecar.Dock.Tests/       Native parser, file-safety, redaction, and diagnostics tests
```

## Build, test, and publish

```powershell
npm test
npm run dock:test
npm run dock:publish
```

The `Sidecar Dock` GitHub Actions workflow:

1. Restores the native projects.
2. Builds the WPF application.
3. Runs the .NET test suite.
4. Publishes a self-contained single-file `Sidecar.exe` for `win-x64`.
5. Launches the packaged executable in startup-smoke mode and verifies successful `MainWindow` construction.
6. Uploads `Sidecar-win-x64` as a workflow artifact.

## Feasibility gates

The project does not advance to a public beta until all of these pass:

1. ChatGPT loads and allows normal login inside WebView2.
2. Login persists after restarting Sidecar.
3. The ChatGPT composer can be identified without relying on one brittle selector.
4. Populating the composer never writes into an unknown field.
5. Manual docking remains stable while moving, resizing, maximizing, minimizing, and switching monitors.
6. The selected Codex thread and repository match what the user intends to share.

If ChatGPT blocks or behaves unreliably inside WebView2, the fallback architecture is to dock the official ChatGPT window instead of embedding the site.

## Privacy and safety

- Sidecar reads local Codex rollout files and repository contents.
- Recent root threads are selected explicitly rather than silently assuming the newest file is correct.
- Common secret patterns are redacted on a best-effort basis.
- Referenced files are restricted to bounded text files inside the selected repository.
- Context is size-limited and previewable before it is placed into ChatGPT.
- Sidecar does not automatically include `.env`, private-key, credential, dependency, build-output, or arbitrary untracked files.
- Startup and runtime diagnostics intentionally exclude context payloads and file contents.
- Repository and conversation content are treated as untrusted data, not instructions.

Review sensitive context before sending it to ChatGPT.

## Legacy prototypes

The Node MCP, hook, shortcut, hotkey, and PowerShell UI-automation experiments remain as implementation history. They are not the supported product workflow; package scripts label them with the `legacy:` prefix.

## Roadmap

- `v0.8.0-alpha`: WebView2 and magnetic-dock feasibility scaffold
- `v0.8.1-alpha.1`: root-thread picker, safe referenced files, redaction, tests, portable artifact
- `v0.8.1-alpha.2`: live WebView/composer diagnostics and validation checklist
- `v0.8.1-alpha.3`: crash-safe startup, single-file publish, and packaged startup smoke test
- `v0.8.1-alpha.4`: drag-to-attach exact window targeting, themed UI, branded icon, and clean executable naming
- `v0.8.2-beta`: validated context-to-ChatGPT workflow and resilient selector profiles
- `v0.9.0-beta`: installer, updates, and public testing
- `v1.0.0`: signed stable Windows release

## License

MIT
