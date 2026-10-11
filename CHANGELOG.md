# Changelog

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
