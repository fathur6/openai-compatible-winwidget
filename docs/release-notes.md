Always-visible Windows desktop widget for DeepSeek and MiniMax balances and Codex subscription quota.

- Draggable window, pin toggle, tray show/hide/quit and saved position.
- DeepSeek and MiniMax USD balances; separate Codex 5-hour and weekly quota/reset rows.
- Windows DPAPI key storage, existing OpenCode/Codex credentials and provider visibility settings.
- 15-minute refresh with per-provider cached fallback and observation timestamps.
- Navigable 7-day quota chart and local hourly SQLite snapshots retained for 12 months.
- Portable self-contained x64 build, sample-data mode, native preview and fixture-based behavior checks.

Download the ZIP, extract it and open WinWidget.exe. No separate .NET installation is required.

The Release build, all 17 behavior checks, native sample-data rendering and portable packaging passed on the GitHub Windows runner. Live provider credentials are not used in CI.

This initial build is unsigned. Application Control intermittently blocked freshly built assemblies on the development PC. No security protections were changed. Internal provider endpoints may change. Input-token charts, emailed monthly reports and arbitrary custom endpoints are not part of this release.
