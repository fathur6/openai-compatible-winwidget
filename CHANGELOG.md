# Changelog

## Iteration 8 · Open-source signing application preparation

- Added the MIT licence with the maintainer's authorization.
- Replaced redistributed SQLite dependencies with a narrow adapter to Windows' System32 SQLite; existing history schema is preserved.
- Added checks for fractional values under non-English culture, Unicode paths and corrupt databases.
- Added a manual SignPath workflow, public privacy policy and code signing policy. Configuration and Foundation approval remain pending.
- Publish folders are cleaned before builds to prevent removed dependencies entering packages; bundles now include the licence.

## Iteration 7 · Trusted signing preparation

- Added CA-issued local certificate signing and timestamp/signature verification for every EXE/DLL.
- Added a manual Azure OIDC signing workflow with required configuration checks.
- Added a directory-package build mode so all binaries can be signed individually.
- No installed signing certificate was found. The released app remains unsigned pending a real signing identity.

## Iteration 6 · v0.3.1 visual polish

- Reduced card padding and chart whitespace so the full legend fits the standard layout.
- Added the release sample-data preview to the README.
- Documented the development PC's confirmed portable-EXE Application Control block.
- Preserved v0.3.0 and its iteration tags; the visual correction ships as v0.3.1.

## Iteration 5 · Release readiness

- GitHub Windows CI passed compilation, all 17 checks, native preview and portable packaging.
- Kept the initial window inside the work area on shorter displays.
- Bounded CI execution and documented verification and release limitations.

## Iteration 4 · Layout and verification

- Compacted cards and quota rows to leave room for chart labels and the legend.
- Preserved history-storage failures in the footer instead of overwriting them with a success message.
- All 17 local behavior checks pass and Release compilation has zero warnings/errors.
- Native demo rendering passed for iteration 3. Application Control intermittently blocks freshly rebuilt assemblies; final preview also runs in Windows CI.

## Iteration 3 · History and distribution

- Hourly SQLite ledger with 12-month retention and additive updates.
- Seven-day dual-axis chart with one-day navigation and gaps for unavailable observations.
- Portable x64 packaging, Windows CI, sample preview and tagged release workflow.
- Fixture-based checks cover quota parsing, provider isolation, encrypted storage and ledger behavior.
- Updated SQLite packages after NuGet identified a vulnerable native dependency.
- Local compilation passes; the local application-control runtime restriction is documented.

## Iteration 2 · Provider integration

- DeepSeek USD balance, MiniMax USD credit and Codex 5-hour/weekly quota.
- Quota windows are classified by duration, independent of payload order.
- Saved API keys use Windows DPAPI; optional existing OpenCode and Codex auth paths.
- Independent requests, 15-minute refresh, timestamps and cached fallback on failures.
- Provider visibility settings and sample-data demo mode.

## Iteration 1 · Desktop shell

- Draggable native window with an always-on-top toggle, saved position and tray controls.
- Dark provider cards and native render smoke mode.
- Release build verified on Windows with no compiler warnings.

## Planning

- Selected a native WPF desktop window with optional always-on-top behavior.
- Planned independent provider refreshes, Windows-protected credentials and local history.
- Public source history excludes credentials, user configuration and usage records.
