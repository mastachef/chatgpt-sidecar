# ChatGPT Sidecar

A Codex companion that offloads planning, debugging, review, and repository investigation to ChatGPT **before Codex starts a model turn**.

## Why the global hook installer exists

Current Codex builds can install the Sidecar plugin and skill while failing to execute the plugin-local `hooks.json`. Sidecar v0.4 therefore keeps the plugin for discovery and `$sidecar`, but installs the executable `UserPromptSubmit` hook into the stable user-level Codex configuration at `~/.codex/hooks.json`.

## Install

Install the plugin normally:

```bash
codex plugin marketplace add mastachef/chatgpt-sidecar
codex plugin add chatgpt-sidecar@mastachef-codex-plugins
```

Then install the stable global hook from a checkout:

```bash
git clone https://github.com/mastachef/chatgpt-sidecar.git
cd chatgpt-sidecar
node ./bin/sidecar.mjs install-global-hook
```

The installer:

- Copies a stable Sidecar runtime into `~/.codex/sidecar-runtime`.
- Preserves existing hooks and backs up an existing `hooks.json`.
- Adds only the Sidecar `UserPromptSubmit` hook to `~/.codex/hooks.json`.
- Uses the absolute path to the Node executable that ran the installer.
- Installs the optional `/prompts:sidecar` alias.

Fully restart Codex after installation and trust the new global hook once when prompted.

## Test

The most deterministic first test is plain composer text:

```text
sidecar: plan summarize this project and propose one harmless README improvement. Do not modify files.
```

Correct behavior:

1. Codex does not answer the planning request.
2. Codex reports that Sidecar intercepted and blocked the turn.
3. ChatGPT opens.
4. A structured Markdown handoff is copied to the clipboard.
5. The handoff is also saved under the active repository's `.sidecar/handoffs` directory.

After that succeeds, these forms are also recognized:

```text
$sidecar plan how to add account recovery
$sidecar debug determine why login tests are failing
$sidecar review
/prompts:sidecar plan how to add account recovery
```

Some Codex clients display an invoked skill as `Sidecar plan ...` rather than preserving `$sidecar`; v0.4 recognizes both forms.

A literal plugin-defined `/sidecar` command is not currently supported by Codex. The optional slash-style alias is `/prompts:sidecar`.

## What happens

1. The global `UserPromptSubmit` hook recognizes the Sidecar trigger.
2. It reads the stored Codex conversation through `codex app-server` and `thread/read`.
3. It captures Git status, diffs, commits, tracked files, `AGENTS.md`, README, and common manifests.
4. It redacts common credential patterns on a best-effort basis.
5. It saves and copies a structured Markdown handoff.
6. It opens ChatGPT.
7. It returns `decision: block`, preventing the Codex model turn.

ChatGPT is instructed to finish with a compact **CODEX EXECUTION PACKET** that can be pasted back into Codex for focused implementation.

## Commands

```bash
node ./bin/sidecar.mjs doctor
node ./bin/sidecar.mjs install-global-hook
node ./bin/sidecar.mjs uninstall-global-hook
node ./bin/sidecar.mjs install-slash-alias
node ./bin/sidecar.mjs bundle "review this repository"
```

`uninstall-global-hook` removes only the Sidecar runtime and Sidecar hook entry; other user hooks are preserved.

## Requirements

- Node.js 20+
- Git
- Current Codex with App Server and hooks
- ChatGPT desktop or a browser

## Privacy and limitations

The hook reads the active stored Codex conversation and local repository context. Redaction is best-effort; review generated handoffs before sending sensitive code or secrets to ChatGPT.

Consumer ChatGPT handoff is clipboard-based because ordinary ChatGPT does not expose a documented local API for silently submitting into a selected existing chat. A fully automatic API mode can be added later but would be separately billed.

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md), [`docs/ROADMAP.md`](docs/ROADMAP.md), and [`docs/AUDIT.md`](docs/AUDIT.md).

## Status

**Version 0.4.** Uses a stable user-level hook to work around unreliable plugin-local hook discovery while preserving the Sidecar plugin and skill interface.

## License

MIT
