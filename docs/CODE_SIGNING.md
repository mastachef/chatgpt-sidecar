# Code signing Sidecar releases

Sidecar public releases must be Authenticode-signed before GitHub Releases will publish them. The workflow supports two trusted signing paths:

1. **SignPath Foundation / Open Source Code Signing** — free for eligible open-source projects.
2. **Microsoft Artifact Signing Public Trust** — Microsoft's managed signing service.

The workflow signs `Sidecar.exe`, timestamps it, verifies the resulting Authenticode signature with `Get-AuthenticodeSignature`, and only then uploads the executable to GitHub Releases.

## Recommended for Sidecar: SignPath Foundation

Sidecar is a public MIT-licensed project, so the first path to try is SignPath Foundation's free open-source signing program. Eligibility is decided by SignPath Foundation; approval is not automatic.

After the project is approved and configured in SignPath:

1. Link the GitHub repository as the trusted build source.
2. Configure an Authenticode artifact configuration for `Sidecar.exe`.
3. Create/use a release signing policy.
4. Create a CI API token with permission to submit release signing requests.
5. Add these GitHub Actions repository secrets:

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
- If neither trusted signing configuration is complete, the workflow **fails before publishing**. Sidecar will no longer intentionally publish another `Unknown publisher` executable.
- A valid trusted signature removes `Unknown publisher` and shows the certificate publisher identity instead.
- SmartScreen reputation is a separate Windows signal. A newly signed non-Store binary can still receive a reputation warning until the publisher/file gains reputation.

Do not use a self-signed certificate for public releases; Windows treats self-signed public downloads similarly to unsigned software unless the certificate is manually trusted on each machine.

## Guaranteed no-SmartScreen distribution

Microsoft documents Microsoft Store distribution as the reliable route for avoiding SmartScreen download warnings entirely. Sidecar can continue offering GitHub releases as well, but the Store is the strongest option for users who want a normal trusted install experience.
