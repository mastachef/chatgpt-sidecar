# ChatGPT Sidecar

A rough but runnable Codex plugin that offloads planning, debugging, review, and repository investigation to ChatGPT **before Codex starts a model turn**.

The goal is simple: spend Codex usage primarily on editing, testing, and verification—not on long exploratory planning conversations.

## Current MVP

Type one of these as normal composer text inside Codex:

```text
gpt: plan how to add account recovery
gpt-plan: design the cleanest migration path
gpt-debug: determine why login tests are failing
gpt-review:
```

Do **not** type `/gpt`. Current Codex clients reject unregistered custom slash commands before `UserPromptSubmit` hooks run. The `gpt:` prefix is ordinary prompt text, so the hook can intercept and block it before the model receives it.

The plugin then intercepts the prompt, reads the stored Codex conversation through App Server, gathers Git/repository context, redacts common credential patterns, saves and copies a structured handoff, opens ChatGPT, and blocks the original Codex prompt.

## Important limitation

This version is intentionally a one-way handoff. Ordinary consumer ChatGPT does not expose a documented local API for silently submitting into a chosen existing chat, so the MVP uses clipboard + browser focus. A fully automatic mode can later use the OpenAI API, but that is separately billed.

## Requirements

- Node.js 20 or newer
- Git
- A current Codex installation with App Server and plugin hooks
- ChatGPT desktop or a browser

## Install

```bash
codex plugin marketplace add mastachef/chatgpt-sidecar
codex plugin add chatgpt-sidecar@mastachef-codex-plugins
```

Restart Codex or the ChatGPT desktop app after installation. Open `/hooks`, inspect this plugin's `UserPromptSubmit` hook, and trust it before testing.

Then type normal composer text such as:

```text
gpt-plan: plan the next feature and identify exact files and tests
```

## Verify the local checkout

```bash
git clone https://github.com/mastachef/chatgpt-sidecar.git
cd chatgpt-sidecar
npm test
node ./bin/sidecar.mjs doctor
```

## Privacy and safety

The plugin reads the active stored Codex conversation and local repository context. It applies best-effort redaction for common credentials, but no redactor is perfect. Review the generated handoff before pasting it into ChatGPT when working with sensitive repositories.

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md), [`docs/ROADMAP.md`](docs/ROADMAP.md), and [`docs/AUDIT.md`](docs/AUDIT.md).

## Status

**Version 0.2 audited rough draft.** Static contract issues and the simulated end-to-end path are covered, but a final live validation still needs to run on Windows with the current Codex desktop/CLI installed.

## License

MIT
