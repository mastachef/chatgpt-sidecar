![Project Screenshot](https://i.imgur.com/xfmBoS9.jpeg)

# ChatGPT Sidecar

A hook-free companion for the ChatGPT Codex app. Sidecar reads the newest saved Codex conversation directly from `~/.codex/sessions`, captures the associated repository context, copies a structured handoff, and opens ChatGPT.

**It does not submit anything into Codex, start a Codex thread, or start a Codex model turn.**

## Install on Windows

```powershell
cd $HOME\chatgpt-sidecar
git pull
node .\bin\sidecar.mjs install-launcher
```

The installer:

- Copies the current Sidecar runtime to `%USERPROFILE%\.codex\sidecar-runtime`.
- Removes Sidecar hook entries and the old prompt alias.
- Installs a `sidecar` command.
- Adds the command directory to your user `PATH`.
- Creates a **ChatGPT Sidecar** Desktop shortcut.
- Assigns **Ctrl+Alt+S** to that shortcut.

Open a new PowerShell window after installation.

## Use it

While your Codex conversation is open, press:

```text
Ctrl+Alt+S
```

Choose `plan`, `debug`, `review`, or `general`, then enter what ChatGPT should do.

You can also use PowerShell:

```powershell
sidecar plan "work out the safest implementation plan"
sidecar debug "diagnose the current failure"
sidecar review "review the current changes"
```

For a direct test from the checkout:

```powershell
node .\bin\sidecar.mjs launch plan "summarize the current Codex discussion and propose the next step"
```

## What happens

1. Sidecar searches `%USERPROFILE%\.codex\sessions` for the newest saved root Codex conversation.
2. It avoids newer subagent rollouts when the user's visible root conversation is available.
3. It reads the saved JSONL locally; no Codex executable or App Server is needed.
4. It identifies the conversation's working directory.
5. It captures Git status, diffs, commits, tracked files, `AGENTS.md`, README, and common manifests.
6. It redacts common credential patterns on a best-effort basis.
7. It saves the handoff under the repository's `.sidecar\handoffs` directory.
8. It copies the handoff to the clipboard and opens ChatGPT.

ChatGPT is instructed to end with a compact **CODEX EXECUTION PACKET** that can be pasted back into Codex for focused implementation.

## Verify without Codex usage

```powershell
node .\bin\sidecar.mjs doctor
```

A ready installation reports:

```json
{
  "savedSessionsAvailable": true,
  "externalLauncherInstalled": true,
  "legacyGlobalHookInstalled": false,
  "modelTurnRequired": false
}
```

Running `launch` is also safe from Codex usage because it only reads local files and runs Git commands.

## Requirements

- Windows with Node.js 20+
- Git
- ChatGPT with Codex sessions stored under `%USERPROFILE%\.codex\sessions`
- ChatGPT desktop or a browser

## Privacy and limitations

The launcher reads the newest locally saved root Codex session. If multiple unrelated Codex conversations are active at exactly the same time, it chooses the most recently updated root session. Redaction is best-effort; review generated handoffs before sending sensitive code or secrets to ChatGPT.

Consumer ChatGPT handoff is clipboard-based because ordinary ChatGPT does not expose a documented local API for silently submitting into a selected existing chat.

## Legacy hook commands

The old hook commands remain in the CLI for rollback and diagnosis, but they are no longer the recommended architecture. Sidecar v0.5 intentionally ships no plugin hook and no bundled Codex skill.

## Status

**Version 0.5.0.** Replaces the unreliable Codex Desktop hook workflow with a standalone launcher that reads saved session files directly and cannot consume a Codex model turn.

## License

MIT
