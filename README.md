# ChatGPT Sidecar

A rough but runnable Codex plugin that offloads planning, debugging, review, and repository investigation to ChatGPT **before Codex starts a model turn**.

The goal is simple: spend Codex usage primarily on editing, testing, and verification—not on long exploratory planning conversations.

## Current MVP

Type one of these inside Codex:

```text
/gpt plan how to add account recovery
/gpt-plan design the cleanest migration path
/gpt-debug determine why login tests are failing
/gpt-review
```

The plugin then:

1. Intercepts the command through a `UserPromptSubmit` hook.
2. Reads the stored Codex conversation using `codex app-server` and `thread/read`.
3. Captures the active Git branch, status, diffs, recent commits, tracked files, `AGENTS.md`, README, and common project manifests.
4. Builds a structured Markdown handoff.
5. Saves it under the plugin data directory and copies it to your clipboard.
6. Opens ChatGPT.
7. Blocks the original prompt so Codex does not process it as a model turn.

Paste the prepared handoff into ChatGPT. ChatGPT is instructed to finish with a compact **CODEX EXECUTION PACKET** that you can paste back into Codex for implementation.

## Important limitation

This first version is intentionally a one-way handoff. Ordinary consumer ChatGPT does not currently expose a documented local API for silently submitting into a chosen existing chat, so the MVP uses clipboard + browser focus. A fully automatic mode can later use the OpenAI API, but that is separately billed.

## Requirements

- Node.js 20 or newer
- Git
- A current Codex installation with App Server and plugin hooks
- ChatGPT desktop or a browser

Run:

```bash
npm test
node ./bin/sidecar.mjs doctor
```

## Install from a GitHub marketplace

After this repository is published:

```bash
codex plugin marketplace add mastachef/chatgpt-sidecar
```

Restart the ChatGPT desktop app, open **Plugins**, select the marketplace, install **Codex ChatGPT Sidecar**, and review/trust its hook through `/hooks`.

## Local development install

Clone the repository, then add its directory as a local marketplace:

```bash
codex plugin marketplace add /absolute/path/to/chatgpt-sidecar
```

Restart the desktop app and install it from the Plugins Directory. Plugin-bundled hooks are not automatically trusted; inspect and trust the hook definition before testing.

## Test without installing the plugin

From any Git repository:

```bash
node /path/to/chatgpt-sidecar/bin/sidecar.mjs bundle "Plan the next feature"
```

That creates a repository-only handoff, copies it to the clipboard, and opens ChatGPT.

## Privacy and safety

The plugin reads the active stored Codex conversation and local repository context. Before broader release, it needs configurable secret redaction and ignore rules. Do not use the rough MVP on repositories containing credentials or material you do not want copied into ChatGPT.

Repository contents and transcript content are explicitly labeled as untrusted data in the generated handoff to reduce prompt-injection risk.

## Implementation notes

- `UserPromptSubmit` receives the prompt, Codex session ID, turn ID, model, and working directory.
- Returning `{ "decision": "block" }` prevents the `/gpt*` command from reaching the Codex model.
- `thread/read` retrieves stored thread history without resuming the thread.
- The transcript JSONL path is deliberately not parsed because OpenAI documents it as an unstable interface.

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) and [`docs/ROADMAP.md`](docs/ROADMAP.md).

## Status

**Version 0.1 rough draft.** The core handoff is implemented, but it still needs real-world testing in the current Windows ChatGPT desktop/Codex environment and hardening before public promotion.

## License

MIT
