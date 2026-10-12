# Privacy policy

OpenAI Compatible WinWidget is maintained by Fathurrahman Lananan (`fathur6`). It has no telemetry, advertising, analytics or maintainer-operated backend.

When you configure provider accounts, the widget sends authenticated HTTPS requests directly to DeepSeek, MiniMax and ChatGPT to read balances and quota. Those providers process requests under their own privacy policies. Your account credentials are necessarily sent to their corresponding provider, never to this project's maintainer or signing service.

Manually saved API keys are encrypted using Windows DPAPI for your Windows account. Existing OpenCode/Codex authentication files are read locally; Codex tokens are used in memory and are not copied into widget settings. Provider response bodies and credentials are not logged.

Preferences, window position, cached balances/quota and hourly observations are kept under `%LOCALAPPDATA%\WinWidget`. History is retained for 12 months. Card visibility does not stop refreshes or history recording. Quit the widget to stop requests. Clear saved keys in Settings; after quitting, remove the local data folder to delete the widget's stored data. Existing external authentication files are unaffected.

Downloading from GitHub and visiting provider websites are subject to those services' policies. The signing programme processes maintainer application details separately; installed widgets do not contact SignPath.

Report privacy concerns through the repository's GitHub issues without including credentials, account data or private authentication files.
