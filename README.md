![ChatGPT Sidecar](https://i.imgur.com/xfmBoS9.jpeg)

# ChatGPT Sidecar

Use the active Codex conversation and repository inside **ChatGPT Quick Chat**, without asking the main Codex thread to spend another model turn on planning, debugging, review, or investigation.

Sidecar v0.6 bundles a local, read-only MCP server with the Codex plugin. It reads saved Codex rollout files and repository state directly from your machine, then supplies that context to the ChatGPT conversation opened from Codex.

## Primary workflow: inside the ChatGPT Codex app

1. Install or refresh the **Codex ChatGPT Sidecar** plugin.
2. Open the Codex project and conversation you are working on.
3. Select **New chat** while in Codex to open ChatGPT Quick Chat.
4. Invoke Sidecar with one of these prompts:

```text
$sidecar plan the safest implementation for the feature we were discussing
$sidecar debug the failure from my active Codex thread
$sidecar review the current Codex discussion and repository changes
```

You can also select the Sidecar plugin/tool from the chat's plugin or tools menu and say:

```text
Use Sidecar to pull my active Codex context and work out the next implementation step.
```

Sidecar calls `sidecar_get_active_context`, which returns the latest saved root Codex thread, its repository path, Git status and diffs, recent commits, tracked files, and common project instruction or manifest files.

## Why this avoids another Codex turn

The MCP bridge reads files under `~/.codex/sessions` and the associated repository. It does not call `thread/start`, `thread/resume`, or `turn/start`, and it does not submit text to the main Codex composer.

The final response stays in ChatGPT Quick Chat and ends with a compact **CODEX EXECUTION PACKET** that can be pasted into Codex only when implementation is ready.

## Bundled tools

- `sidecar_get_active_context` — fetch the newest root Codex conversation and repo context.
- `sidecar_list_recent_threads` — list recent root conversations when several projects are active.
- `sidecar_get_thread` — fetch a selected saved thread by ID.
- `sidecar_get_repo_context` — fetch Git and project context for a selected directory.

All tools are read-only.

## Install or update

Install through the Codex plugin marketplace, or refresh the existing plugin from the Plugins screen to pull the latest source.

For development from a checkout:

```powershell
cd $HOME\chatgpt-sidecar
git pull
(Get-Content .\package.json | ConvertFrom-Json).version
```

The version should be `0.6.0`.

The plugin manifest bundles:

```json
{
  "skills": "./skills/",
  "mcpServers": "./.mcp.json"
}
```

The MCP server starts automatically through:

```json
{
  "command": "node",
  "args": ["./mcp/server.mjs", "--stdio"]
}
```

## Safe verification

Opening the MCP/tools panel in the Codex app should show a server named `chatgpt-sidecar` with these four tools. Inspecting the tool list does not submit a model turn.

A protocol-level smoke test is also available from the checkout:

```powershell
node .\mcp\server.mjs --stdio
```

For normal use, do not run the server manually; the plugin starts it.

## Thread selection

By default Sidecar chooses the most recently updated saved **root** Codex conversation and ignores newer subagent rollouts. When that is not the intended project, ChatGPT can call `sidecar_list_recent_threads` and re-run `sidecar_get_active_context` with the selected `session_id`.

## Fallback launcher

The v0.5 external launcher remains available for clients that do not expose plugin MCP tools in Quick Chat:

```powershell
node .\bin\sidecar.mjs install-launcher
sidecar plan "your request"
```

This is a fallback, not the primary experience.

## Requirements

- ChatGPT desktop with Codex
- Node.js 20+
- Git
- Saved Codex sessions under `CODEX_HOME/sessions` or `~/.codex/sessions`

## Privacy

Sidecar reads local Codex rollout files and repository contents. Common credential patterns are redacted on a best-effort basis, but users should still avoid attaching secrets and should review sensitive context before relying on it outside the local machine.

Repository and conversation contents are treated as untrusted data and cannot override the user's instructions.

## Status

**Version 0.6.0.** Adds a bundled read-only MCP bridge and a Sidecar skill designed for ChatGPT Quick Chat inside the Codex desktop experience. The external launcher remains as a compatibility fallback.

## License

MIT
