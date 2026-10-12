param(
    [string]$PackageDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts/win-x64-signing'),
    [switch]$Launch
)
$ErrorActionPreference = 'Stop'
$taskSource = (Resolve-Path -LiteralPath $PackageDirectory).Path
$taskExecutable = Join-Path $taskSource 'WinWidget.exe'
if (!(Test-Path -LiteralPath $taskExecutable)) { throw 'Package must contain WinWidget.exe.' }
$taskInstall = Join-Path $env:LOCALAPPDATA 'Programs/OpenAI Compatible WinWidget'
if (Get-Process WinWidget -ErrorAction SilentlyContinue | Where-Object { $_.Path -and $_.Path.StartsWith($taskInstall, [StringComparison]::OrdinalIgnoreCase) }) {
    throw 'Quit the installed widget before updating it.'
}
New-Item -ItemType Directory -Path $taskInstall -Force | Out-Null
# An existing installation requires an explicit update strategy; avoid stale dependencies.
if (Test-Path -LiteralPath (Join-Path $taskInstall 'WinWidget.exe')) { throw 'An installation already exists. Quit it and move its application folder aside before reinstalling. Local account data is stored separately.' }
Get-ChildItem -LiteralPath $taskSource | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $taskInstall -Recurse -Force }
$taskInstalledExe = Join-Path $taskInstall 'WinWidget.exe'
if (!(Test-Path -LiteralPath $taskInstalledExe)) { throw 'Installation did not copy the executable.' }
$taskShell = New-Object -ComObject WScript.Shell
foreach ($taskFolder in @([Environment]::GetFolderPath('Desktop'), [Environment]::GetFolderPath('Programs'))) {
    $taskShortcut = $taskShell.CreateShortcut((Join-Path $taskFolder 'OpenAI Compatible WinWidget.lnk'))
    $taskShortcut.TargetPath = $taskInstalledExe
    $taskShortcut.WorkingDirectory = $taskInstall
    $taskShortcut.Description = 'Always-visible AI balances and quota widget'
    $taskShortcut.IconLocation = "$taskInstalledExe,0"
    $taskShortcut.Save()
}
Write-Output "Installed to $taskInstall"
if ($Launch) {
    # The user requests a visible interactive desktop widget.
    Start-Process -FilePath $taskInstalledExe -WorkingDirectory $taskInstall -WindowStyle Normal
}
