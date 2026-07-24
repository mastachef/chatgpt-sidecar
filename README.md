![ChatGPT Sidecar](https://i.imgur.com/Tnpg6pn.png)

# ChatGPT Sidecar Dock

A native Windows companion panel that magnetically attaches to the ChatGPT/Codex desktop window, reads a selected saved Codex conversation and its repository locally, and prepares that context inside an embedded ChatGPT session without submitting another prompt to Codex.

> **Current status: v0.8.1-alpha.2.** The native dock builds, passes automated Node and .NET tests, publishes a portable Windows artifact, and now includes privacy-safe live WebView/composer diagnostics. The remaining feasibility gate is validation on a real Windows ChatGPT session.

## What the alpha does

- Finds the visible Codex/ChatGPT desktop window.
- Magnetically docks to its edge and follows movement and resizing.
- Hosts `chatgpt.com` in a persistent WebView2 profile.
- Lists recent saved **root** Codex threads and excludes subagent rollouts.
- Lets the user explicitly select the conversation to send.
- Collects bounded Git status, staged and unstaged diffs, recent commits, `AGENTS.md`, README, and common manifests.
- Detects safe repository files referenced in the selected Codex conversation.
- Blocks `.env`, credential, key, build-output, dependency, traversal, and out-of-repository paths.
- Redacts common private keys, bearer tokens, API keys, GitHub tokens, AWS keys, passwords, and credential URLs.
- Builds a previewable, size-limited context package.
- Populates the ChatGPT composer for the user to review and send.
- Falls back to the clipboard when the composer cannot be safely identified.
- Records WebView runtime, navigation, process, and composer-probe diagnostics without logging context contents.

The alpha deliberately does **not** auto-submit. Populate-only behavior remains the safety gate until the embedded ChatGPT workflow proves stable.

## Run the native alpha

Requirements:

- Windows 10 or 11
- .NET 8 SDK for development builds
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

## Alpha workflow

1. Open the Codex project and conversation you are working on.
2. Launch **ChatGPT Sidecar Dock**.
3. The panel finds and attaches to Codex.
4. Choose the correct recent root Codex thread from the thread picker.
5. Enter a planning, debugging, review, or general request.
6. Click **Preview context** to inspect exactly what will be provided.
7. Click **Prepare in ChatGPT**.
8. Review the populated ChatGPT composer and press Send.

Nothing is typed into the Codex composer and no Codex thread is resumed or started.

## Live validation and diagnostics

Follow [`docs/live-validation.md`](docs/live-validation.md) to validate docking, login persistence, thread selection, privacy, and composer population.

Click **Copy diagnostics** after any failure. The copied report includes:

- app, .NET, Windows, and architecture versions
- WebView2 runtime version
- sanitized route categories such as `/auth/*` or `/c/*`
- WebView process failures
- the composer selector, candidate count, and failure reason

It does **not** include the Codex conversation, request, repository contents, Git diffs, ChatGPT messages, or full conversation URLs.

The rolling local log is stored at:

```text
%LOCALAPPDATA%\ChatGPTSidecar\Diagnostics\sidecar-dock.log
```

## Native project structure

```text
apps/
├── Sidecar.Dock/
│   ├── ChatGPT/              Persistent WebView2 host and composer adapter
│   ├── CodexContext/         Rollout parser, thread list, and package builder
│   ├── Diagnostics/          Privacy-bounded runtime validation log
│   ├── Docking/              Magnetic dock controller
│   ├── Interop/              Win32 window APIs
│   ├── RepositoryContext/    Git, manifests, and referenced-file collection
│   ├── Security/             Credential redaction
│   ├── WindowDetection/      Codex/ChatGPT window detection
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
4. Publishes a self-contained `win-x64` build.
5. Uploads `Sidecar.Dock-win-x64` as a workflow artifact.

## Feasibility gates

The project does not advance to a public beta until all of these pass:

1. ChatGPT loads and allows normal login inside WebView2.
2. Login persists after restarting Sidecar.
3. The ChatGPT composer can be identified without relying on one brittle selector.
4. Populating the composer never writes into an unknown field.
5. Magnetic docking remains stable while moving, resizing, maximizing, minimizing, and switching monitors.
6. The selected Codex thread and repository match what the user intends to share.

If ChatGPT blocks or behaves unreliably inside WebView2, the fallback architecture is to dock the official ChatGPT window instead of embedding the site.

## Privacy and safety

- Sidecar reads local Codex rollout files and repository contents.
- Recent root threads are selected explicitly rather than silently assuming the newest file is correct.
- Common secret patterns are redacted on a best-effort basis.
- Referenced files are restricted to bounded text files inside the selected repository.
- Context is size-limited and previewable before it is placed into ChatGPT.
- Sidecar does not automatically include `.env`, private-key, credential, dependency, build-output, or arbitrary untracked files.
- Runtime diagnostics intentionally exclude context payloads and file contents.
- Repository and conversation content are treated as untrusted data, not instructions.

Review sensitive context before sending it to ChatGPT.

## Legacy prototypes

The Node MCP, hook, shortcut, hotkey, and PowerShell UI-automation experiments remain as implementation history. They are not the supported product workflow; package scripts label them with the `legacy:` prefix.

## Roadmap

- `v0.8.0-alpha`: WebView2 and magnetic-dock feasibility scaffold
- `v0.8.1-alpha.1`: root-thread picker, safe referenced files, redaction, tests, portable artifact
- `v0.8.1-alpha.2`: live WebView/composer diagnostics and validation checklist
- `v0.8.2-beta`: validated context-to-ChatGPT workflow and resilient selector profiles
- `v0.9.0-beta`: installer, updates, and public testing
- `v1.0.0`: signed stable Windows release

## License

MIT
