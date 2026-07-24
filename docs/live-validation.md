# Live Windows validation

This checklist validates the two capabilities that headless GitHub Actions cannot prove: ChatGPT authentication inside WebView2 and safe composer population.

## Before starting

1. Open the current Codex desktop app and keep a normal root conversation visible.
2. Make sure the repository has no secrets intentionally written into ordinary source files.
3. Build or run Sidecar Dock:

```powershell
cd $HOME\chatgpt-sidecar
git pull
dotnet run --project .\apps\Sidecar.Dock\Sidecar.Dock.csproj
```

## Test 1: magnetic docking

1. Confirm Sidecar appears beside the Codex window.
2. Move and resize Codex.
3. Minimize and restore Codex.
4. Move Codex between monitors when available.

Pass condition: Sidecar remains visible, on-screen, and aligned without rapid flicker.

## Test 2: embedded ChatGPT login

1. Sign into ChatGPT inside Sidecar when prompted.
2. Confirm a normal blank ChatGPT conversation loads.
3. Close Sidecar completely.
4. Start Sidecar again.

Pass condition: the ChatGPT login persists after restart.

## Test 3: thread selection

1. Open the **Thread** dropdown.
2. Confirm recent root Codex conversations appear.
3. Confirm obvious worker/subagent sessions do not appear.
4. Select the intended project and thread.

Pass condition: the title and project shown above the browser match the intended Codex work.

## Test 4: preview and privacy

1. Enter a harmless planning request.
2. Click **Preview context**.
3. Confirm the selected conversation, Git state, and referenced files are relevant.
4. Search the preview for `.env`, `PRIVATE KEY`, `Authorization: Bearer`, and known local credentials.

Pass condition: excluded files are absent and recognized credential values are replaced with redaction markers.

## Test 5: composer population

1. Close the preview.
2. Click **Prepare in ChatGPT**.
3. Do not press ChatGPT's Send button yet.
4. Inspect the ChatGPT composer.

Pass condition: the prepared Sidecar context appears only in the visible ChatGPT composer. Sidecar must not type into search, login, navigation, or any unknown field.

## On failure

Click **Copy diagnostics** and paste the resulting report into the GitHub issue or development chat.

The report includes:

- Sidecar and .NET versions
- Windows and process architecture
- WebView2 runtime version
- sanitized navigation route categories
- WebView process failures
- composer selector, candidate count, and failure reason

The report does **not** include:

- the Codex conversation
- the Sidecar request
- repository file contents
- Git diffs
- ChatGPT message contents
- full ChatGPT conversation URLs

The raw local log is stored at:

```text
%LOCALAPPDATA%\ChatGPTSidecar\Diagnostics\sidecar-dock.log
```

## Gate decision

- All five tests pass: continue toward resilient selector profiles and installer work.
- Login fails or is blocked: pivot to docking the official ChatGPT window.
- Login works but composer population fails: update the selector/adapter layer using the copied diagnostic report.
- Docking fails: fix Win32 window tracking before adding more ChatGPT features.
