param([string]$Runtime = 'win-x64', [string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
Push-Location $taskRoot
try {
    & $Dotnet build src/WinWidget/WinWidget.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    & $Dotnet run --project tests/WinWidget.Tests -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Behavior checks failed' }
    & $Dotnet publish src/WinWidget/WinWidget.csproj -c Release -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "artifacts/$Runtime"
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
    Copy-Item README.md "artifacts/$Runtime/README.md"
    Compress-Archive -Path "artifacts/$Runtime/*" -DestinationPath "artifacts/WinWidget-$Runtime.zip" -Force
} finally { Pop-Location }
