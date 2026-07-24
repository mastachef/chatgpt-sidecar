# Sidecar updater

Sidecar's built-in updater uses GitHub Releases as the update source.

## Update flow

1. Check recent public releases from `mastachef/chatgpt-sidecar`.
2. Compare the published version to the running Sidecar version.
3. Locate the uploaded `Sidecar.exe` release asset.
4. Require GitHub's SHA-256 release-asset digest.
5. Download the candidate executable to `%LOCALAPPDATA%\ChatGPTSidecar\Updates`.
6. Recompute SHA-256 locally and require an exact digest match.
7. Require Windows `Get-AuthenticodeSignature` to report the candidate as `Valid` with a signer certificate.
8. When the current executable has a valid signer, require the candidate's signer subject to match it.
9. Start a separate replacement process, close Sidecar, move the old executable to a temporary backup, install the verified update, restart Sidecar, and remove the backup after a successful restart launch.

A failed digest or signature check aborts before the installed executable is replaced.

## First updater-capable release

The updater is introduced in `v0.8.1-alpha.8`. Existing builds older than that must be upgraded manually once. After an updater-capable build is installed, future signed GitHub releases can be installed from inside Sidecar.

## Signing dependency

The public release workflow refuses to publish unsigned builds. The updater therefore expects every release it installs to carry a valid trusted Authenticode signature. See `CODE_SIGNING.md` for SignPath and Microsoft Artifact Signing setup.
