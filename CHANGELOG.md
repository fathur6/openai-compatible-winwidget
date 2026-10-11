# Changelog

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
