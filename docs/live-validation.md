# Live Windows validation

This checklist covers behavior that headless CI cannot fully prove: exact-window docking, WebView2 authentication, visible composer population, and real desktop interaction.

## Before starting

1. Open the current Codex desktop app and a normal root conversation.
2. Build or run Sidecar:

```powershell
dotnet run --project .\apps\Sidecar.Dock\Sidecar.Dock.csproj
```

3. Use a repository that does not intentionally contain real credentials in ordinary source files.

## 1. Manual attach and docking

1. Start Sidecar without attaching it.
2. Confirm it does not guess or automatically select a Codex window.
3. Drag **Attach** onto the intended Codex window and release.
4. Move and resize Codex.
5. Minimize and restore Codex.
6. Move Codex between monitors when available.

Pass: Sidecar follows only the selected window, remains on-screen, and does not jump to another window.

## 2. Embedded ChatGPT login

1. Sign into ChatGPT inside Sidecar.
2. Confirm a normal ChatGPT conversation loads.
3. Close Sidecar completely and start it again.

Pass: the ChatGPT session persists after restart.

## 3. Codex thread selection

1. Open the thread selector.
2. Confirm recent root Codex conversations appear.
3. Confirm obvious worker/subagent sessions are not offered as normal root threads.
4. Select the intended project/thread.

Pass: the displayed project and conversation match the work you intended to hand off.

## 4. Context preview and privacy

1. Enter a harmless planning request.
2. Select **Preview**.
3. Confirm the selected conversation, Git state, diffs, and referenced files are relevant.
4. Search the preview for `.env`, `PRIVATE KEY`, `Authorization: Bearer`, and any known test credential values.

Pass: excluded sensitive files are absent and recognized credential values are replaced with redaction markers.

## 5. ChatGPT composer population

1. Close the preview.
2. Select **Prepare in ChatGPT**.
3. Do not send the message yet.
4. Inspect the visible ChatGPT composer.

Pass: the prepared context appears in the ChatGPT composer and Sidecar does not auto-submit it or type into unrelated fields.

## 6. Return-to-Codex handoff

1. Send the prepared request in ChatGPT and wait for the response.
2. Select **Prepare handoff** and send that request in ChatGPT.
3. After the implementation handoff is returned, select **Copy latest reply**.
4. Paste into a text editor before using Codex.

Pass: the copied reply is the expected self-contained implementation handoff and contains no unexpected hidden/local data.

## 7. Update path

Run this only with a trusted signed test/release pair.

1. Start an older updater-capable Sidecar build.
2. Check for updates.
3. Confirm the newer release is detected.
4. Install it through Sidecar.

Pass: Sidecar verifies the release, exits, replaces the executable, restarts at the newer version, and leaves the original untouched when validation fails.

## Diagnostics

```text
Startup: %LOCALAPPDATA%\ChatGPTSidecar\Diagnostics\startup-crash.log
Runtime: %LOCALAPPDATA%\ChatGPTSidecar\Diagnostics\sidecar-dock.log
```

Diagnostics should not contain Codex conversation text, repository file contents, Git diffs, or ChatGPT message contents.
