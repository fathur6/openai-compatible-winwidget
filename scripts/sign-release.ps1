param(
    [Parameter(Mandatory)][ValidatePattern('^[A-Fa-f0-9]{40}$')][string]$CertificateThumbprint,
    [string]$Runtime = 'win-x64',
    [string]$SignToolPath = '',
    [string]$TimestampUrl = 'http://timestamp.digicert.com'
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskPublishDirectory = Join-Path $taskRoot "artifacts/$Runtime-signing"
if (!$SignToolPath) {
    $taskTool = Get-ChildItem "${env:ProgramFiles(x86)}/Windows Kits/10/bin" -Filter signtool.exe -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.Directory.Name -eq 'x64' } | Sort-Object FullName -Descending | Select-Object -First 1
    if (!$taskTool) { throw 'Install the Windows SDK signing tools or supply -SignToolPath.' }
    $SignToolPath = $taskTool.FullName
}
$taskCertificate = Get-Item -LiteralPath "Cert:/CurrentUser/My/$CertificateThumbprint" -ErrorAction Stop
if (!$taskCertificate.HasPrivateKey -or $taskCertificate.NotAfter -lt (Get-Date)) { throw 'A current code-signing certificate with an accessible private key is required.' }
if ($taskCertificate.Subject -eq $taskCertificate.Issuer) { throw 'Use a CA-issued code-signing certificate, not a self-signed certificate.' }
$taskChain = [Security.Cryptography.X509Certificates.X509Chain]::new()
try {
    $taskChain.ChainPolicy.ApplicationPolicy.Add([Security.Cryptography.Oid]::new('1.3.6.1.5.5.7.3.3'))
    if (!$taskChain.Build($taskCertificate)) { throw 'Certificate chain validation failed; a trusted CA-issued code-signing certificate is required.' }
} finally { $taskChain.Dispose() }
if (!(Test-Path -LiteralPath (Join-Path $taskPublishDirectory 'WinWidget.exe'))) { throw 'Run build.ps1 -DirectoryBundle -SkipPackage first.' }
$taskFiles = Get-ChildItem -LiteralPath $taskPublishDirectory -File -Recurse | Where-Object { $_.Extension -in '.exe', '.dll' }
foreach ($taskFile in $taskFiles) {
    $taskSignature = Get-AuthenticodeSignature -LiteralPath $taskFile.FullName
    if ($taskSignature.Status -eq 'Valid' -and $taskSignature.TimeStamperCertificate) { continue }
    & $SignToolPath sign /sha1 $CertificateThumbprint /s My /fd SHA256 /tr $TimestampUrl /td SHA256 $taskFile.FullName
    if ($LASTEXITCODE -ne 0) { throw "Signing failed for $($taskFile.Name)" }
}
& "$PSScriptRoot/verify-signatures.ps1" -PublishDirectory $taskPublishDirectory
Compress-Archive -Path "$taskPublishDirectory/*" -DestinationPath (Join-Path $taskRoot "artifacts/WinWidget-$Runtime-signed.zip") -Force
Write-Host 'Signed package created. Verify execution under the target Windows policy before distribution.'
