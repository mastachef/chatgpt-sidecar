# Sidecar code signing policy

## Code signing policy

Every public Sidecar Windows binary must come from the repository's automated release build and must carry a valid trusted Authenticode signature before publication.

Sidecar's policy is:

- `Sidecar.exe` is built from the public `mastachef/chatgpt-sidecar` repository by GitHub Actions.
- Public release builds are produced from the release commit on `main`; the workflow does not accept a locally supplied replacement binary.
- The Windows product name and release/version metadata come from the native project file.
- The unsigned build is submitted to the configured trusted signing provider by the release workflow.
- The resulting Authenticode signature is verified on the GitHub Actions Windows runner before upload.
- If trusted signing is unavailable, build/test may succeed but the public executable release is skipped.
- If signing is attempted and signature validation fails, publication is blocked.
- Self-signed certificates are not accepted for public releases.
- Changes to build, release, signing, or updater logic are source-controlled and should receive the same review attention as application code.

The workflow supports two trusted signing paths:

1. **SignPath Foundation / Open Source Code Signing** — preferred for Sidecar and free for eligible open-source projects.
2. **Microsoft Artifact Signing Public Trust** — alternative managed signing service.

## SignPath Foundation setup

Sidecar has applied to SignPath Foundation's open-source program. Eligibility and approval are controlled by SignPath Foundation.

After the project is approved and configured in SignPath:

1. Link `mastachef/chatgpt-sidecar` as the trusted GitHub build source.
2. Configure the Authenticode artifact configuration so only the expected Sidecar Windows executable is signed.
3. Configure metadata restrictions for the Sidecar product name and release version as required by SignPath Foundation.
4. Create/use a release signing policy tied to the approved repository/branch/build origin.
5. Keep manual approval enabled for release signing when required by the Foundation policy.
6. Create a CI API token with permission to submit release signing requests.
7. Add these GitHub Actions repository secrets:

   - `SIGNPATH_API_TOKEN`
   - `SIGNPATH_ORGANIZATION_ID`
   - `SIGNPATH_PROJECT_SLUG`
   - `SIGNPATH_SIGNING_POLICY_SLUG`

The release workflow uploads the unsigned build as a short-lived GitHub Actions artifact, submits that exact artifact to SignPath, receives the signed executable, verifies the Windows signature, and replaces the unsigned build before publication.

## Alternative: Microsoft Artifact Signing

### One-time Microsoft/Azure setup

1. Create an Artifact Signing **Public Trust** account and complete identity validation.
2. Create a Public Trust certificate profile for Sidecar.
3. Create an Entra application/service principal for GitHub Actions and grant it the **Artifact Signing Certificate Profile Signer** role on the certificate profile.
4. Configure GitHub Actions OIDC/federated credentials for this repository and the `main` branch.
5. Add these GitHub Actions repository secrets:

   - `AZURE_CLIENT_ID`
   - `AZURE_TENANT_ID`
   - `AZURE_SUBSCRIPTION_ID`
   - `ARTIFACT_SIGNING_ENDPOINT`
   - `ARTIFACT_SIGNING_ACCOUNT_NAME`
   - `ARTIFACT_SIGNING_CERT_PROFILE_NAME`

The endpoint must match the Artifact Signing region, for example `https://wus2.codesigning.azure.net/` for West US 2.

## Release behavior

- SignPath is used when all four SignPath secrets are present.
- Otherwise Microsoft Artifact Signing is used when all six Microsoft/Azure secrets are present.
- If neither trusted signing configuration is complete, the build/test workflow can finish successfully but the public executable release is skipped.
- Sidecar will not intentionally publish another `Unknown publisher` executable.
- A valid trusted signature removes `Unknown publisher` and shows the certificate publisher identity instead.
- SmartScreen reputation is a separate Windows signal. A newly signed non-Store binary can still receive a reputation warning while reputation develops.

## Updater trust

The in-app updater does not treat a GitHub download alone as sufficient trust. It requires the GitHub release asset's SHA-256 digest to match the downloaded file and requires Windows to validate the downloaded executable's trusted Authenticode signature before replacement. When the currently running Sidecar build is signed, the update must also match its signer.

## Microsoft Store option

Microsoft Store distribution can be added alongside GitHub Releases for users who prefer a Store-managed install/update path. GitHub Releases remain useful for the portable build and Sidecar's in-app updater.
