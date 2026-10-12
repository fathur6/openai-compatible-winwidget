param([Parameter(Mandatory)][string]$PublishDirectory)
$ErrorActionPreference = 'Stop'
$taskDirectory = (Resolve-Path -LiteralPath $PublishDirectory).Path
$taskFiles = @(Get-ChildItem -LiteralPath $taskDirectory -File -Recurse | Where-Object { $_.Extension -in '.exe', '.dll' })
if ($taskFiles.Count -eq 0 -or !(Test-Path -LiteralPath (Join-Path $taskDirectory 'WinWidget.exe'))) { throw 'No WinWidget executable found.' }
$taskManifest = foreach ($taskFile in $taskFiles) {
    $taskSignature = Get-AuthenticodeSignature -LiteralPath $taskFile.FullName
    if ($taskSignature.Status -ne 'Valid') { throw "Signature verification failed for $($taskFile.Name): $($taskSignature.Status)" }
    if (!$taskSignature.TimeStamperCertificate) { throw "Missing trusted timestamp on $($taskFile.Name)" }
    if ($taskSignature.SignerCertificate.Subject -eq $taskSignature.SignerCertificate.Issuer) { throw "Self-signed publisher certificate rejected for $($taskFile.Name)" }
    [pscustomobject]@{
        File = [IO.Path]::GetRelativePath($taskDirectory, $taskFile.FullName)
        SHA256 = (Get-FileHash -LiteralPath $taskFile.FullName -Algorithm SHA256).Hash
        Publisher = $taskSignature.SignerCertificate.Subject
        Issuer = $taskSignature.SignerCertificate.Issuer
        Thumbprint = $taskSignature.SignerCertificate.Thumbprint
        Status = $taskSignature.Status.ToString()
    }
}
$taskManifest | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $taskDirectory 'signatures.json') -Encoding utf8
Write-Host "Verified $($taskFiles.Count) timestamped Authenticode signatures."
