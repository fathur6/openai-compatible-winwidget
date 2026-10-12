# Code signing policy and downloads

Download published builds from [GitHub Releases](https://github.com/fathur6/openai-compatible-winwidget/releases).

## Status

Current downloads are unsigned. We are preparing an application to the SignPath Foundation's free open-source programme. Acceptance and signing are pending; no SignPath certificate or signed release is available yet.

After approval, code signing is planned to be provided by [SignPath.io](https://signpath.io), with a certificate issued by the [SignPath Foundation](https://signpath.org). This is a planned arrangement, not a claim that current downloads are signed.

## Release policy

- Maintainer and release approver: Fathurrahman Lananan (`fathur6`). Contributors are identified through public Git commits and reviews.
- Build origin: this public repository and GitHub-hosted Windows Actions runners. The signing service must verify the uploaded artifact's build provenance.
- Production signing requests require the maintainer's manual approval in SignPath. Account setup must satisfy SignPath's MFA requirements.
- Only this project's `WinWidget.exe`, `WinWidget.dll` and `WinWidget.Core.dll` may be signed with its project certificate. Microsoft runtime signatures must be preserved. Windows' SQLite library is loaded from System32 and is not redistributed.
- All packaged EXEs and DLLs must have valid, timestamped Authenticode signatures before a package is labelled signed. Verification produces a public SHA-256 signature manifest; failures prevent packaging.
- Credentials, private keys and API tokens never enter Git. Signed artifacts are reviewed before publication to GitHub Releases.

See [signing setup](signing.md) and [privacy policy](privacy.md). The application may be declined, including because this is a newly released project without established adoption.
