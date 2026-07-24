![Sidecar](https://i.imgur.com/Nki8DF1.png)

# Sidecar

**Use ChatGPT for planning, debugging, review, and repository analysis while keeping Codex focused on implementation.**

Sidecar is a native Windows companion for Codex. It attaches only to the exact Codex window you choose, reads a selected saved Codex conversation plus bounded local repository context, and prepares that context inside an embedded ChatGPT session. When the planning work is finished, Sidecar turns the result into a detailed handoff you can paste back into Codex.

Sidecar never submits a Codex prompt on your behalf.

## Screenshots

### Codex + Sidecar

<p align="center">
  <img src="https://i.imgur.com/TE1iGrK.png" width="100%" alt="Codex with Sidecar attached" />
</p>

### Sidecar

<p align="center">
  <img src="https://i.imgur.com/tTH2qWz.png" width="520" alt="Sidecar Windows application" />
</p>

### Themes

<p align="center">
  <img src="https://i.imgur.com/HADIHwh.png" width="48%" alt="Sidecar theme example 1" />
  <img src="https://i.imgur.com/u0EDuHg.png" width="48%" alt="Sidecar theme example 2" />
</p>

<p align="center">
  <img src="https://i.imgur.com/PWcAuQu.png" width="48%" alt="Sidecar theme example 3" />
  <img src="https://i.imgur.com/C8AjkwS.png" width="48%" alt="Sidecar theme example 4" />
</p>

## Workflow

```text
Codex context → Sidecar / ChatGPT planning → implementation handoff → Codex
```

1. Open the Codex project and conversation you are working on.
2. Start Sidecar and sign into ChatGPT in the embedded browser.
3. Drag **Attach** onto the exact Codex window you want Sidecar to follow.
4. Select the saved root Codex thread you want to use.
5. Enter a Plan, Debug, Review, or General request.
6. Use **Preview** to inspect the context, then **Prepare in ChatGPT**.
7. Review and send the prepared message in ChatGPT.
8. When the work is complete, use **Prepare handoff**, then **Copy latest reply**.
9. Paste the finished implementation handoff into Codex.

## Features

- **Manual window attachment** — Sidecar never guesses which window to use.
- **Persistent ChatGPT session** — WebView2 keeps your ChatGPT login between launches.
- **Saved Codex context** — choose recent root Codex conversations without resuming or submitting them.
- **Repository context** — bounded Git status, diffs, recent commits, project files, and referenced files.
- **Context preview** — inspect exactly what will be prepared before it reaches ChatGPT.
- **Return-to-Codex handoff** — produce a self-contained implementation prompt for Codex.
- **Secret filtering** — sensitive file categories are excluded and common credential patterns are redacted on a best-effort basis.
- **Native themes** — Codex Green, Codex Dark, Midnight, Light, and System, including the custom title bar.
- **Secure updater** — GitHub updates require both the release SHA-256 digest and a valid trusted Authenticode signature before replacement.
- **Privacy-safe diagnostics** — diagnostics exclude conversation text, repository contents, diffs, and ChatGPT messages.

## Download

GitHub Releases are Sidecar's portable distribution channel:

**[Download from GitHub Releases](https://github.com/mastachef/chatgpt-sidecar/releases/latest)**

New trusted releases publish two byte-identical signed executables:

- `Sidecar-Portable-win-x64.exe` — clearly named manual/portable download.
- `Sidecar.exe` — canonical asset used by the built-in updater.

The portable build is self-contained and does not require the .NET SDK or a separate .NET runtime installation.

Every public executable must pass trusted Authenticode verification before the release workflow can publish it. See [Code signing](docs/CODE_SIGNING.md).

### Microsoft Store

A separate MSIX distribution is being prepared for Microsoft Store installation and Store-managed updates. See [Microsoft Store publishing](docs/MICROSOFT_STORE.md).

## Updates

Sidecar checks GitHub Releases when it starts and also provides an **Updates** button.

Before replacing the running executable, it:

1. downloads the canonical `Sidecar.exe` release asset to `%LOCALAPPDATA%\ChatGPTSidecar\Updates`;
2. verifies GitHub's SHA-256 asset digest locally;
3. requires Windows to report a valid trusted Authenticode signature;
4. requires the same publisher as the running build when the current build is signed;
5. replaces the executable only after Sidecar exits, with rollback protection, then restarts it.

See [Updater design](docs/UPDATES.md).

## Requirements

- Windows 10 or Windows 11, 64-bit
- ChatGPT/Codex desktop app with at least one saved Codex conversation
- Microsoft Edge WebView2 Runtime
- Git for repository status/diff collection

## Privacy and safety

Sidecar keeps context collection local until you explicitly prepare it for ChatGPT. Context is bounded, sensitive file categories are excluded, common credentials are redacted on a best-effort basis, and the complete prepared payload can be previewed first.

Sidecar does not type into the Codex composer, resume Codex threads, or submit Codex turns. The return handoff is copied to the clipboard for you to review and paste manually.

## Diagnostics

```text
Startup: %LOCALAPPDATA%\ChatGPTSidecar\Diagnostics\startup-crash.log
Runtime: %LOCALAPPDATA%\ChatGPTSidecar\Diagnostics\sidecar-dock.log
```

A trusted signature removes the **Unknown publisher** label. SmartScreen reputation for direct downloads is a separate Windows signal.

## Development

```powershell
dotnet restore .\apps\Sidecar.Dock.Tests\Sidecar.Dock.Tests.csproj
dotnet test .\apps\Sidecar.Dock.Tests\Sidecar.Dock.Tests.csproj --configuration Release
dotnet run --project .\apps\Sidecar.Dock\Sidecar.Dock.csproj
```

For a self-contained portable build:

```powershell
dotnet publish .\apps\Sidecar.Dock\Sidecar.Dock.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -o .\artifacts\Sidecar-win-x64
```

More detail: [Architecture](docs/ARCHITECTURE.md) · [Roadmap](docs/ROADMAP.md) · [Windows validation](docs/live-validation.md)

## License

MIT
