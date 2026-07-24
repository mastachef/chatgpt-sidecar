![ChatGPT Sidecar](https://i.imgur.com/Tnpg6pn.png)

# ChatGPT Sidecar

Send the active Codex conversation and repository to a **real ChatGPT Quick Chat** without submitting another prompt to the Codex thread.

## The workflow

After one-time setup on Windows:

1. Keep the Codex conversation you are working on visible.
2. Press **Ctrl+Alt+S**.
3. Sidecar shows a small request window with the newest Codex thread preselected.
4. Choose `plan`, `debug`, `review`, or `general` and enter what ChatGPT should do.
5. Sidecar reads the saved Codex thread and repository locally.
6. It activates the ChatGPT desktop app's real **New chat** control through Windows Accessibility.
7. ChatGPT Quick Chat opens inside the app, the context is pasted, and the message is submitted automatically.

Nothing is entered into the Codex composer, so Sidecar does not start another Codex model turn.

## Install or update

Install or refresh the **Codex ChatGPT Sidecar** plugin, then run the one-time Windows setup from a checkout:

```powershell
cd $HOME\chatgpt-sidecar
git pull
node .\bin\install-quickchat.mjs
```

The installer:

- copies the current runtime to `%USERPROFILE%\.codex\sidecar-runtime`;
- removes the obsolete Sidecar hook and prompt alias;
- creates **ChatGPT Sidecar** shortcuts on the Desktop and Start Menu;
- assigns **Ctrl+Alt+S**;
- installs a `sidecar-quickchat` command as an optional fallback.

After setup, PowerShell is not part of the normal workflow.

## What Sidecar sends to Quick Chat

Sidecar prepares a structured local context packet containing:

- the selected saved root Codex conversation;
- the conversation's working directory;
- Git branch, HEAD, remote, and status;
- working-tree and staged diffs;
- recent commits and tracked files;
- `AGENTS.md`, README, and common manifests when present;
- the planning, debugging, review, or general request entered by the user.

ChatGPT is instructed to first summarize the state of work, then complete the request, and finish with a compact **CODEX EXECUTION PACKET** for implementation.

## Thread selection

The Sidecar window lists recent saved root Codex threads and preselects the most recently updated one. This avoids accidentally selecting a newer subagent rollout and lets users switch projects before opening Quick Chat.

## Why this avoids Codex usage

The hotkey workflow only:

- reads files under `~/.codex/sessions`;
- runs local Git read commands;
- writes a Markdown handoff under `.sidecar/handoffs`;
- uses Windows Accessibility to invoke the desktop app's **New chat** button;
- pastes and submits into ChatGPT Quick Chat.

It does not call `thread/start`, `thread/resume`, or `turn/start`, and it does not submit text to the Codex composer.

## Bundled MCP bridge

The plugin also includes an optional read-only MCP bridge:

- `sidecar_get_active_context`
- `sidecar_list_recent_threads`
- `sidecar_get_thread`
- `sidecar_get_repo_context`

These tools remain useful for debugging and future native integration, but the primary user workflow is the one-hotkey Quick Chat automation.

## Requirements

- Windows 10 or 11
- The current ChatGPT desktop app with Codex and Quick Chat
- Node.js 20+
- Git
- Saved Codex sessions under `CODEX_HOME/sessions` or `~/.codex/sessions`
- Windows Accessibility access to the ChatGPT app

## Troubleshooting

Sidecar writes automation diagnostics to:

```text
%USERPROFILE%\.codex\sidecar-quickchat.log
```

If an app update changes the accessible name of the **New chat** button or Quick Chat composer, the prepared context remains on the clipboard and the log records which automation step failed.

## Privacy

Sidecar reads local Codex rollout files and repository contents. Common credential patterns are redacted on a best-effort basis, but users should still avoid attaching secrets and should review sensitive context before sharing it.

Repository and conversation contents are treated as untrusted data and cannot override the user's instructions.

## Status

**Version 0.7.0.** Adds the complete Windows one-hotkey workflow: request window, recent-thread selection, local context preparation, real ChatGPT Quick Chat activation, automatic paste, and automatic submit.

## License

MIT
