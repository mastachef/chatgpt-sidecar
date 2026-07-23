# Architecture

## MVP flow

1. Codex invokes the plugin's `UserPromptSubmit` hook for every submitted prompt.
2. The hook exits immediately for ordinary prompts.
3. For `/gpt*` commands, it reads the stored thread through `codex app-server` using `thread/read`.
4. It gathers repository context with deterministic Git commands and selected project files.
5. It writes a Markdown handoff into the plugin data directory.
6. It copies the handoff to the clipboard and opens ChatGPT.
7. It returns `decision: block`, preventing the command from becoming a Codex model turn.
8. ChatGPT produces a compact execution packet that the user pastes back into Codex for implementation.

## Why the MVP uses clipboard handoff

A consumer ChatGPT chat does not expose a documented local API that lets a Codex plugin silently target a specific existing chat. Clipboard plus browser focus is therefore the least invasive subscription-based handoff.

## Later phases

- Small local sidecar UI that tracks one ChatGPT planning session per Codex thread.
- Repository search and selective file attachment instead of a static context bundle.
- Optional OpenAI API mode for fully automatic planning and structured responses.
- Supported return-channel integration when an official ChatGPT conversation API becomes available.
- Context delta caching so repeated `/gpt` calls include only new turns and Git changes.
