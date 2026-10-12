# Trusted Windows signing

## Current status

v0.3.1 is unsigned. No code-signing certificate was found in the development PC's user or machine certificate stores. The scripts and workflow below prepare a signed package, but cannot sign until a real certificate or verified service account is available. Windows trust has not been changed and no signed release has been published.

Authenticode signing identifies the publisher and protects file integrity. It does not guarantee acceptance by every organizational Application Control policy or immediately eliminate SmartScreen reputation warnings. Validate the signed package on the target PC after signing.

## With an existing CA-issued certificate

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

SignPath Foundation is another possible free route for qualifying open-source projects, subject to their approval. This repository currently has no declared open-source license, and their policy also restricts signing of third-party binaries. Do not assume acceptance or label it as a SignPath-signed project before approval. [SignPath conditions](https://signpath.org/terms).

Official references: [Smart App Control signing requirements](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/code-signing-for-smart-app-control), [code-signing options](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options), [Artifact Signing eligibility and setup](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart), [Azure signing action](https://github.com/Azure/artifact-signing-action), [GitHub OIDC setup](https://github.com/Azure/artifact-signing-action/blob/main/docs/OIDC.md).
