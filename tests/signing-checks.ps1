param([Parameter(Mandatory)][string]$UnsignedAssembly)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskFixtureRoot = Join-Path ([IO.Path]::GetTempPath()) ('WinWidgetSigningChecks-' + [Guid]::NewGuid().ToString())
New-Item -ItemType Directory -Path $taskFixtureRoot | Out-Null
try {
    # Use a real first-party unsigned PE file. Nothing in the fixture is executed.
    Copy-Item -LiteralPath $UnsignedAssembly -Destination (Join-Path $taskFixtureRoot 'WinWidget.exe')
    $taskRejected = $false
    try { & "$taskRoot/scripts/verify-signatures.ps1" -PublishDirectory $taskFixtureRoot }
    catch { $taskRejected = $_.Exception.Message -like 'Signature verification failed*' }
    if (!$taskRejected) { throw 'Signature gate must reject an unsigned executable.' }
    if (Test-Path -LiteralPath (Join-Path $taskFixtureRoot 'signatures.json')) { throw 'A rejected package must not receive a verified manifest.' }
    Write-Host 'Passed unsigned-package rejection and no-manifest checks.'
} finally {
    $taskResolvedFixture = [IO.Path]::GetFullPath($taskFixtureRoot)
    $taskResolvedTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (!$taskResolvedFixture.StartsWith($taskResolvedTemp, [StringComparison]::OrdinalIgnoreCase)) { throw 'Fixture cleanup path is outside the temporary directory.' }
    Remove-Item -LiteralPath $taskResolvedFixture -Recurse -Force
}
