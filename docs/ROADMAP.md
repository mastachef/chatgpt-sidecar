# Roadmap

## 0.1 — Rough MVP

- [x] Intercept `/gpt`, `/gpt-plan`, `/gpt-debug`, and `/gpt-review`.
- [x] Block the corresponding Codex prompt before a model turn.
- [x] Read stored Codex history through App Server.
- [x] Gather branch, status, diffs, commits, tracked files, and key manifests.
- [x] Save and copy a structured ChatGPT handoff.
- [x] Open ChatGPT automatically.
- [ ] Validate the plugin inside the current ChatGPT desktop/Codex release on Windows.

## 0.2 — Better context retrieval

- [ ] Track the last exported Codex turn.
- [ ] Send conversation deltas instead of the entire thread.
- [ ] Add ripgrep-based relevant-file selection from the request.
- [ ] Detect secrets and redact likely credentials before export.
- [ ] Add configurable size limits and ignored paths.

## 0.3 — Two-way sidecar

- [ ] Local session dashboard keyed by Codex thread ID.
- [ ] Paste/import a ChatGPT execution packet with one click.
- [ ] Optional Responses API mode for automatic planning.
- [ ] Structured execution-packet validation.
