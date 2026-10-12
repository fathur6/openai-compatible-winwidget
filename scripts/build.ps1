param([string]$Runtime = 'win-x64', [string]$Dotnet = 'dotnet', [switch]$DirectoryBundle, [switch]$SkipPackage)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
Push-Location $taskRoot
try {
    & $Dotnet build src/WinWidget/WinWidget.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    & $Dotnet run --project tests/WinWidget.Tests -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Behavior checks failed' }
    $taskSingleFile = (!$DirectoryBundle).ToString().ToLowerInvariant()
    $taskOutput = if ($DirectoryBundle) { "artifacts/$Runtime-signing" } else { "artifacts/$Runtime" }
    & $Dotnet publish src/WinWidget/WinWidget.csproj -c Release -r $Runtime --self-contained true "-p:PublishSingleFile=$taskSingleFile" -p:IncludeNativeLibrariesForSelfExtract=true -o $taskOutput
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
    Copy-Item README.md "$taskOutput/README.md"
    if (!$SkipPackage) {
        if ($DirectoryBundle) { throw 'Use sign-release.ps1 or the signed workflow to verify signatures before packaging a directory bundle.' }
        Compress-Archive -Path "$taskOutput/*" -DestinationPath "artifacts/WinWidget-$Runtime.zip" -Force
    }
} finally { Pop-Location }
