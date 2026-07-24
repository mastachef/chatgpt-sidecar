# Code signing Sidecar releases

Sidecar's GitHub release workflow supports Microsoft Artifact Signing (formerly Trusted Signing). When the signing configuration is present, the workflow Authenticode-signs and RFC3161-timestamps `Sidecar.exe` before uploading it to GitHub Releases, then verifies the signature with `Get-AuthenticodeSignature`.

## One-time Microsoft/Azure setup

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

- If all signing secrets are configured, `.github/workflows/release.yml` signs and verifies `Sidecar.exe` before publishing it.
- If any signing secret is missing, the build still publishes but emits an explicit warning and the release notes state that the artifact is unsigned.
- A trusted signature changes Windows from `Unknown publisher` to the verified publisher identity. SmartScreen reputation is separate, so a newly signed binary can still receive an initial reputation warning until Microsoft has enough reputation data for the signed publisher/file.

Do not use a self-signed certificate for public releases; Windows does not trust it by default.
