# Trusted Windows signing

## Current status

v0.3.1 is unsigned. No code-signing certificate was found in the development PC's user or machine certificate stores. The scripts and workflow below prepare a signed package, but cannot sign until a real certificate or verified service account is available. Windows trust has not been changed and no signed release has been published.

Authenticode signing identifies the publisher and protects file integrity. It does not guarantee acceptance by every organizational Application Control policy or immediately eliminate SmartScreen reputation warnings. Validate the signed package on the target PC after signing.

## Selected route: SignPath Foundation

The maintainer authorized MIT licensing and a free SignPath Foundation application. The application is being prepared; approval is not guaranteed, especially for a new project. See the [public signing policy](code-signing-policy.md) and [privacy policy](privacy.md).

The current 0.4.0 source replaces redistributed SQLite dependencies with Windows' `winsqlite3.dll`, loaded only from System32. Existing `history.db` files use the same schema. A clean directory build contains three unsigned project files; all other packaged binaries retain their Microsoft signatures.

After Foundation approval:

1. Complete SignPath account setup and required MFA. Install the SignPath GitHub App with access limited to this repository, reviewing its permissions.
2. Configure a SignPath project linked to this repository and GitHub-hosted Actions. Its artifact configuration must use a ZIP root and select **only** `WinWidget.exe`, `WinWidget.dll` and `WinWidget.Core.dll` for signing. Preserve all existing Microsoft runtime signatures. Confirm the `OpenAI Compatible WinWidget` product identity and consistent version metadata.
3. Configure a production signing policy requiring maintainer approval for each release and restricting build origin to this project's GitHub-hosted workflow and approved main branch.
4. Create a GitHub `signpath` environment with an appropriate main-branch restriction. Set environment variables `SIGNPATH_ORGANIZATION_ID`, `SIGNPATH_PROJECT_SLUG`, `SIGNPATH_SIGNING_POLICY_SLUG`, and secret `SIGNPATH_API_TOKEN`. The token must have only the required signing-request submitter permissions; never paste it in chat or commit it.
5. Run **SignPath Windows package** on main. It builds and checks a clean directory bundle, uploads a GitHub artifact for provenance verification, submits a signing request and waits up to 20 minutes for approval. Approve it in SignPath. A timeout requires inspecting the existing request before starting another build/request.
6. The workflow verifies all binary signatures and timestamps before making `WinWidget-win-x64-signed.zip`. Download and validate this artifact on the target Windows PC, then attach it to the corresponding GitHub release. Update the signing status and attribution only after successful production signing.

The workflow is manual and fails before building when provider configuration is missing. It does not create an account, accept terms, install certificates or change Windows protection settings. Provider project and artifact configuration must be reviewed with SignPath before the first production request.

References: [Foundation conditions](https://signpath.org/terms), [GitHub integration and action parameters](https://docs.signpath.io/trusted-build-systems/github).

## Alternative: an existing CA-issued certificate

Install the Windows SDK signing tools and use a publicly trusted code-signing certificate whose private key is accessible through the CurrentUser `My` certificate store (including supported hardware-backed keys). Private keys and passwords must not be committed or pasted into chat.

```powershell
./scripts/build.ps1 -DirectoryBundle -SkipPackage
./scripts/sign-release.ps1 -CertificateThumbprint YOUR_40_CHARACTER_THUMBPRINT
```

This signs unsigned EXEs and DLLs with SHA-256 and an RFC 3161 timestamp, preserves already valid timestamped signatures, checks every binary and creates `artifacts/WinWidget-win-x64-signed.zip`. A `signatures.json` manifest records public publisher details and SHA-256 hashes. Failed verification stops packaging. The signed path uses a directory package so app and native dependency DLLs can be signed individually. It never installs certificates or changes Application Control settings.

## With Azure Artifact Signing

The manual **Signed Windows package** workflow uses Azure OIDC; it requires no long-lived client secret or exported private key. It is inactive until configured and intentionally does not label the ordinary unsigned release as trusted.

1. Obtain a verified **Public Trust** signing profile. Microsoft currently allows individual developers in the US/Canada and organizations in a listed set of countries. Malaysia is not currently listed; do not use a Private Trust or test profile as a substitute for public trust.
2. Configure an Entra application's federated GitHub credential with subject `repo:fathur6/openai-compatible-winwidget:environment:signing`. Grant the Artifact Signing Certificate Profile Signer role scoped to the intended profile. Configure Azure Login's subscription access as documented.
3. Create a GitHub `signing` environment and configure its deployment branch restrictions to allow only the intended protected release branch. Optional environment reviewers can control use of the signing identity.
4. Set these GitHub environment variables: `SIGNING_AZURE_CLIENT_ID`, `SIGNING_AZURE_TENANT_ID`, `SIGNING_AZURE_SUBSCRIPTION_ID`, `SIGNING_ENDPOINT`, `SIGNING_ACCOUNT`, `SIGNING_PROFILE`.
5. Run the workflow on the release branch. Download its signed artifact and validate it on the target Windows PC before attaching it to a release.

If you are applying from Malaysia, use another publicly trusted provider that supports your legal identity and location, or investigate Microsoft Store distribution. An account, identity verification and any provider fees must be completed before an actual signature can be produced.

## Without an existing account

For a free route, Microsoft Store distribution through an MSIX package provides Microsoft-managed signing after certification. It requires a developer account and a reserved app identity from Partner Center. The existing portable EXE ZIP is not an MSIX submission and is not automatically signed by the Store. No Store account or app identity has been supplied yet.

This repository is now MIT-licensed and is pursuing SignPath Foundation approval as described above. Do not label a download as signed before production signing and verification succeed.

Official references: [Smart App Control signing requirements](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/code-signing-for-smart-app-control), [code-signing options](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options), [Artifact Signing eligibility and setup](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart), [Azure signing action](https://github.com/Azure/artifact-signing-action), [GitHub OIDC setup](https://github.com/Azure/artifact-signing-action/blob/main/docs/OIDC.md).
