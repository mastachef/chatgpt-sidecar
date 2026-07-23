---
name: sidecar
description: Explicitly hand planning, debugging, review, or repository investigation from Codex to the ChatGPT Sidecar without asking Codex to solve it.
---

This skill marks a request for the Sidecar `UserPromptSubmit` hook. Invoke it with `$sidecar` followed by the mode and request. The hook must intercept and block the Codex model turn before these instructions are used by the model.
