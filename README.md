![ChatGPT Sidecar](https://i.imgur.com/Tnpg6pn.png)

# ChatGPT Sidecar Dock

A native Windows companion panel that magnetically attaches to the ChatGPT/Codex desktop window, reads the active saved Codex conversation and repository locally, and prepares that context inside an embedded ChatGPT session without submitting another prompt to Codex.

> **Current status: v0.8.0-alpha.1 feasibility build.** The native dock is implemented and must now be validated against the current ChatGPT desktop/web authentication and composer behavior.

## What the alpha does

- Finds the visible Codex/ChatGPT desktop window.
- Magnetically docks to its right or left edge and follows movement and resizing.
- Hosts `chatgpt.com` in a persistent WebView2 profile so login can survive restarts.
- Reads the newest saved root Codex rollout from `CODEX_HOME/sessions` or `~/.codex/sessions`.
- Collects bounded Git status, staged and unstaged diffs, recent commits, `AGENTS.md`, README, and common manifests.
- Builds a redacted, size-limited context package.
- Populates the ChatGPT composer for the user to review and send.
- Falls back to the clipboard when the composer cannot be safely identified.

The alpha deliberately does **not** auto-submit. Populate-only behavior is the safety gate until the embedded ChatGPT workflow proves stable.

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
4. Enter a planning, debugging, review, or general request.
5. Click **Preview context** to inspect exactly what will be provided.
6. Click **Prepare in ChatGPT**.
7. Review the populated ChatGPT composer and press Send.

Nothing is typed into the Codex composer and no Codex thread is resumed or started.

## Native project structure

```text
apps/Sidecar.Dock/
├── ChatGPT/              Persistent WebView2 host and composer adapter
├── CodexContext/         Saved rollout parser and context package builder
├── Docking/              Magnetic dock controller
├── Interop/              Win32 window APIs
├── RepositoryContext/    Bounded Git and project context collection
├── WindowDetection/      Codex/ChatGPT window detection
├── App.xaml
├── MainWindow.xaml
└── Sidecar.Dock.csproj
```

## Build a self-contained Windows alpha

```powershell
dotnet publish .\apps\Sidecar.Dock\Sidecar.Dock.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -o .\artifacts\Sidecar.Dock-win-x64
```

The `Sidecar Dock` GitHub Actions workflow builds and publishes the same portable artifact on Windows.

## Feasibility gates

The project does not advance to a public beta until all of these pass:

1. ChatGPT loads and allows normal login inside WebView2.
2. Login persists after restarting Sidecar.
3. The ChatGPT composer can be identified without relying on a single brittle CSS class.
4. Populating the composer never writes into an unknown field.
5. Magnetic docking remains stable while moving, resizing, maximizing, minimizing, and switching monitors.
6. The selected Codex rollout matches the project the user is actually viewing.

If ChatGPT blocks or behaves unreliably inside WebView2, the fallback architecture is to dock the official ChatGPT window instead of embedding the site.

## Privacy and safety

- Sidecar reads local Codex rollout files and repository contents.
- Common secret patterns are redacted on a best-effort basis.
- Context is bounded and previewable before it is placed into ChatGPT.
- Sidecar does not automatically include `.env`, private-key, credential, or arbitrary untracked files.
- Repository and conversation content are treated as untrusted data, not instructions.

Review sensitive context before sending it to ChatGPT.

## Legacy prototypes

The Node MCP, hook, shortcut, hotkey, and PowerShell UI-automation experiments remain in the repository temporarily as implementation history. They are **not the supported product workflow** and will be moved under `legacy/` after the native feasibility gate passes.

## Roadmap

- `v0.8.0-alpha`: WebView2 and magnetic-dock feasibility
- `v0.8.1-alpha`: accurate active-thread resolver and recent-thread picker
- `v0.8.2-beta`: complete context-to-ChatGPT workflow
- `v0.9.0-beta`: installer, diagnostics, updates, and public testing
- `v1.0.0`: signed stable Windows release

## License

MIT
