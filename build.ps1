#Requires -Version 5.1
<#
    Build + test script for Dock Manager (run on Windows).

    Usage:
        .\build.ps1               build Release and run the unit tests
        .\build.ps1 -SkipTests    build Release only
        .\build.ps1 -Publish      build Release and emit a single-file self-contained exe
#>
param(
    [switch]$SkipTests,
    [switch]$Publish
)

$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot

try {
    Write-Host "dotnet: $((Get-Command dotnet -ErrorAction SilentlyContinue).Source)"

    Write-Host "`n== Restore =="
    dotnet restore DockManager.sln
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "`n== Build (Release) =="
    dotnet build DockManager.sln --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if (-not $SkipTests) {
        Write-Host "`n== Tests =="
        dotnet test tests/DockManager.Core.Tests/DockManager.Core.Tests.csproj --configuration Release --no-build
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    if ($Publish) {
        Write-Host "`n== Publish (single-file, self-contained) =="
        dotnet publish src/DockManager.App/DockManager.App.csproj `
            --configuration Release `
            --runtime win-x64 `
            --self-contained true `
            -p:PublishSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -p:EnableCompressionInSingleFile=true `
            -p:DebugType=embedded `
            --output publish
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        Write-Host "`nOutput: $PSScriptRoot\publish\DockManager.exe"
    }

    Write-Host "`nBuild completed."
}
finally {
    Pop-Location
}
