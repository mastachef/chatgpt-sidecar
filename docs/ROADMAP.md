# Roadmap

## Working today

- [x] Native WPF Sidecar shell for Windows 10/11.
- [x] Manual drag-to-attach targeting for the exact Codex window.
- [x] Persistent embedded ChatGPT session through WebView2.
- [x] Saved root Codex conversation selection.
- [x] Bounded Git/repository context collection and referenced-file extraction.
- [x] Context preview before preparing anything for ChatGPT.
- [x] Best-effort secret filtering and sensitive-path exclusions.
- [x] Plan, Debug, Review, and General handoff modes.
- [x] Return-to-Codex implementation handoff and latest-reply copy.
- [x] Native themed window chrome and persistent themes.
- [x] Privacy-safe startup and WebView diagnostics.
- [x] Secure in-app GitHub updater with digest and Authenticode verification.
- [x] Trusted-signing release pipeline with SignPath and Microsoft Artifact Signing support.

## Current release work

- [ ] Complete trusted public signing setup and publish the first updater-capable signed release.
- [ ] Finish Microsoft Store developer/product setup.
- [ ] Add the Partner Center identity to an MSIX packaging project.
- [ ] Produce and validate the first Store submission package.
- [ ] Add Store listing metadata, screenshots, certification notes, and privacy/support links.

## Next improvements

- [ ] Reduce repeated context by tracking what was already handed to ChatGPT for a selected thread.
- [ ] Improve relevant-file selection for large repositories without expanding the default context budget.
- [ ] Add clearer update/restart progress and recovery feedback.
- [ ] Add optional diagnostics export for GitHub issues without exposing project content.
- [ ] Continue hardening WebView2 composer/reply selectors against ChatGPT UI changes.

## Distribution strategy

- **GitHub Releases:** signed, self-contained portable x64 executable with the built-in updater.
- **Microsoft Store:** MSIX installation with Store-managed updates.
