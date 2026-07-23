# ChatGPT Sidecar

A Codex plugin that offloads planning, debugging, review, and repository investigation to ChatGPT **before Codex starts a model turn**.

## Use Sidecar

The supported, plugin-distributed command is:

```text
$sidecar plan how to add account recovery
$sidecar debug determine why login tests are failing
$sidecar review
```

You can open `/skills` and select **Sidecar**, or type `$sidecar` directly. Skills are the current Codex mechanism for plugin-distributed explicit workflows.

### Slash-style alias

Current Codex does **not** allow plugins to register a literal custom `/sidecar` command. Unknown custom slash commands are rejected before `UserPromptSubmit` hooks run. To get the closest supported slash command, install the included custom-prompt alias:

```bash
node ./bin/sidecar.mjs install-slash-alias
```

Restart Codex, then use:

```text
/prompts:sidecar plan how to add account recovery
```

The repository also keeps `/sidecar` parseable internally so it can become a true alias if Codex adds plugin-defined slash-command forwarding later. The plain-text fallback is:

```text
sidecar: plan how to add account recovery
```

## What happens

1. The `UserPromptSubmit` hook recognizes the Sidecar skill, prompt alias, or fallback marker.
2. It reads the stored Codex conversation through `codex app-server` and `thread/read`.
3. It captures Git status, diffs, commits, tracked files, `AGENTS.md`, README, and common manifests.
4. It redacts common credential patterns on a best-effort basis.
5. It saves and copies a structured Markdown handoff.
6. It opens ChatGPT.
7. It returns `decision: block`, preventing the Codex model turn.

ChatGPT is instructed to finish with a compact **CODEX EXECUTION PACKET** that can be pasted back into Codex for focused implementation.

## Requirements

- Node.js 20+
- Git
- Current Codex with App Server and hooks
- ChatGPT desktop or a browser

## Install

```bash
codex plugin marketplace add mastachef/chatgpt-sidecar
codex plugin add chatgpt-sidecar@mastachef-codex-plugins
```

Restart Codex, open `/hooks`, inspect the Sidecar `UserPromptSubmit` hook, and trust it. Plugin hooks do not run until trusted.

## Verify

```bash
git clone https://github.com/mastachef/chatgpt-sidecar.git
cd chatgpt-sidecar
npm test
node ./bin/sidecar.mjs doctor
```

## Privacy and limitations

The plugin reads the active stored Codex conversation and local repository context. Redaction is best-effort; review generated handoffs before sending sensitive code or secrets to ChatGPT.

Consumer ChatGPT handoff is currently clipboard-based because ordinary ChatGPT does not expose a documented local API for silently submitting into a selected existing chat. A fully automatic API mode can be added later but would be separately billed.

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md), [`docs/ROADMAP.md`](docs/ROADMAP.md), and [`docs/AUDIT.md`](docs/AUDIT.md).

## Status

**Version 0.3.** Sidecar now ships as an explicit plugin skill, includes an optional `/prompts:sidecar` slash alias, and preserves plain-text and legacy trigger compatibility.

## License

MIT
