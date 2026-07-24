# Microsoft Store publishing plan

Sidecar will use two distribution tracks:

1. **GitHub Releases:** a clearly labeled portable `Sidecar-Portable-win-x64.exe`, plus the canonical signed `Sidecar.exe` used by Sidecar's updater.
2. **Microsoft Store:** an **MSIX-packaged** build so installation and Store updates are handled by Microsoft.

## Why MSIX for the Store

Microsoft currently supports two Win32 Store paths:

- package the desktop application as MSIX; or
- submit an existing MSI/EXE installer by URL.

The GitHub portable executable is intentionally not an installer, so it is not the right artifact for the MSI/EXE Store path. Microsoft's MSI/EXE path requires an offline installer with silent installation support, a versioned HTTPS URL, and trusted code signing for the installer and its PE files.

For Sidecar, MSIX is the cleaner Store path because the Store signs the submitted package and provides the normal Store installation/update experience while GitHub can remain the portable distribution channel.

## Step 1: create the Store developer account

Start from:

https://storedeveloper.microsoft.com/

Microsoft's current onboarding flow has no registration fee for either Individual or Company accounts when started from that site.

Choose the account type carefully:

- **Individual** publishes under the verified individual's publisher identity.
- **Company** is for publishing in connection with a business/legal entity and requires business/employment verification.

Microsoft currently does not support converting an Individual Store developer account into a Company account later; publishing as a company would require a new Company developer account.

## Step 2: reserve the Sidecar product name

In Partner Center:

1. Open **Apps & Games**.
2. Select **New product**.
3. Select **MSIX or PWA app**.
4. Check and reserve the desired product name, preferably **Sidecar** if available.

Name reservations are time-limited if no submission is made, so do this when we are ready to finish the package.

## Step 3: copy the Store identity values

After the product exists in Partner Center, copy the identity values shown for the app and provide them for the packaging work:

- Package/Identity/Name
- Publisher
- Publisher display name
- Package family name
- Store product ID

Do not invent these values in the manifest. The final package identity must match Partner Center.

## Step 4: add the MSIX packaging project

Once the Partner Center identity is available, the repository will add a Store packaging project/build that:

- packages the existing WPF `Sidecar.exe` as x64 MSIX;
- uses the Partner Center package identity exactly;
- includes Store-compliant visual assets;
- keeps the current application icon and Sidecar branding;
- declares the desktop full-trust entry point;
- produces an `.msixupload`/Store upload artifact;
- runs packaging validation in CI;
- does not alter the portable GitHub build.

For Store submission packages, Microsoft signs the package as part of Store distribution. Local/test MSIX packages still require a trusted test signature on the machine where they are installed.

## Step 5: complete the Partner Center submission

The Store submission will still require human-entered publishing information such as:

- markets/availability;
- category;
- age rating questionnaire;
- privacy/support URLs as applicable;
- Store description and search terms;
- screenshots and visual assets;
- certification notes/instructions;
- the generated package upload.

## Official Microsoft references

- Win32 Store distribution options: https://learn.microsoft.com/windows/apps/distribute-through-store/how-to-distribute-your-win32-app-through-microsoft-store
- Developer account onboarding: https://learn.microsoft.com/windows/apps/publish/partner-center/open-a-developer-account
- Reserve an MSIX app name: https://learn.microsoft.com/windows/apps/publish/publish-your-app/msix/reserve-your-apps-name
- MSIX signing overview: https://learn.microsoft.com/windows/msix/package/signing-package-overview
