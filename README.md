# OpenAI Compatible WinWidget

An always-visible Windows desktop companion for DeepSeek and MiniMax balances and Codex subscription quota. Inspired by [the macOS widget](https://github.com/fathur6/opencode-deepseek-minimax-macwidget).

Built with C# and WPF on .NET 10. Development is recorded as successive commits and milestone tags in this public repository. Credentials and personal usage data stay on the local machine.

![Sample-data preview](https://github.com/fathur6/openai-compatible-winwidget/releases/download/v0.3.1/widget-preview.png)

## Download and use

Get the Windows x64 ZIP from [Releases](https://github.com/fathur6/openai-compatible-winwidget/releases), extract it, and open **WinWidget.exe**. The portable package includes .NET. Windows 11 x64 is the primary target; compatible Windows 10 LTSC/Enterprise editions may also work but have not been tested.

To install an extracted package for your Windows account with desktop and Start menu shortcuts, run `./scripts/install.ps1 -PackageDirectory 'C:\path\to\extracted-package' -Launch` from this repository. It installs under `%LOCALAPPDATA%\Programs\OpenAI Compatible WinWidget`, requires no administrator rights and does not enable automatic startup. Windows may block the current unsigned build while SignPath approval is pending.

Drag the title to move the widget. **Pinned** keeps it above other windows. **Hide** sends it to the tray; double-click the tray icon to restore it, or right-click for Show, Settings and Quit. Position and pin preference survive restarts. Startup is manual in this release.

On shorter displays the content scrolls so the footer controls remain available. The release preview captures the standard 748-pixel layout using sample values.

In **Settings**, enter DeepSeek/MiniMax API keys or point to OpenCode's existing `auth.json`. Saved keys are protected with Windows DPAPI for your current account. Blank key fields preserve saved keys; clear checkboxes remove saved keys. Removing a saved key enables the OpenCode-file fallback again. Card visibility does not stop refreshes or history collection.

For Codex, use an existing Codex OAuth sign-in and select its `auth.json` path. The default respects `CODEX_HOME`, falling back to `%USERPROFILE%\.codex\auth.json`. The widget reads the access token and account ID in memory; it does not copy the token into its settings or renew the session. An API-only login cannot supply subscription quota. Sign in again in Codex when authentication expires.

Data refreshes every 15 minutes. **Refresh** checks all providers immediately. Failed requests keep the previous reading with its original observation timestamp and a visible error. Reset dates use your Windows local timezone. A passed reset timestamp is labeled as awaiting refresh.

The chart shows DeepSeek USD on the left axis and Codex weekly remaining percent on the right. Arrows move its seven-day window by one day. Hourly snapshots are retained for 12 months; unavailable hours are shown as gaps. The ledger also stores MiniMax balance and Codex 5-hour remaining percent. This is observed history, not a reconstruction of past account activity.

Local data lives in `%LOCALAPPDATA%\WinWidget`: `keys.dpapi`, non-secret preferences, cache, window position and `history.db`. These files never enter the repo. Existing auth files are read only. Provider requests go directly over HTTPS to DeepSeek, MiniMax and ChatGPT; the app has no telemetry.

## Build and verification

Install the .NET 10 SDK, then run:

```powershell
./scripts/build.ps1
```

This builds, runs the fixture-based behavior checks and creates `artifacts/WinWidget-win-x64.zip`. GitHub Actions repeats the process on Windows and renders a sample-data preview. Tags beginning with `v` publish a release after those checks pass.

```powershell
dotnet run --project src/WinWidget -- --demo
dotnet run --project src/WinWidget -- --smoke --output=widget-preview.png
dotnet run --project tests/WinWidget.Tests -c Release
```

Demo and smoke modes use sample values and make no provider requests. CI checks quota classification, malformed/partial responses, independent provider failure handling, stale-cache preservation, DPAPI storage, hourly upserts and retention. CI does not use real provider credentials.

The initial release is unsigned. Windows Application Control intermittently blocked newly built assemblies and blocked the portable EXE on the development PC. Trusted code signing is needed to run it under this PC's policy; do not disable that protection. Build results and remote CI verification are reported independently. Production signing requires an appropriate signing certificate.

## Scope and provider endpoints

We are pursuing the SignPath Foundation's free open-source signing programme. Approval and signatures are pending. See the [code signing policy and downloads](docs/code-signing-policy.md), [signing setup](docs/signing.md) and [privacy policy](docs/privacy.md). The current download remains unsigned until a real signed package is produced and verified.

This is an independent Windows implementation inspired by the Mac app; Swift source is not copied. It uses DeepSeek `/user/balance`, MiniMax's internal `/account/query_balance`, and ChatGPT's internal `/backend-api/wham/usage`. Internal endpoints can change without notice. A malformed response produces an error rather than a fabricated balance. Quota windows are identified by their duration (18,000 or 604,800 seconds), never their payload order.

The current provider integrations are DeepSeek, MiniMax and Codex subscription quota. Arbitrary OpenAI-compatible endpoints do not have a standard balance API, so custom-provider configuration is not yet implemented. Input-token charts, cost estimates, manual-credit override and emailed monthly reports are deferred.

## Iterations

1. Native desktop shell: draggable, pinned window, tray controls, saved position.
2. Provider integration: encrypted keys, local Codex session, refresh and cached readings.
3. History and distribution: SQLite history, charts, verification and portable Windows build.

See [CHANGELOG.md](CHANGELOG.md) for completed stages.

Licensed under [MIT](LICENSE). The current source uses Windows' System32 SQLite library for local history; it does not redistribute an unsigned SQLite dependency.
