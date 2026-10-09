<#
.SYNOPSIS
    Builds the freeserf.net Android Release APK.

.DESCRIPTION
    Canonical build entry point for the Android port. Wraps `dotnet build` with
    all flags required on this machine (see Android.md): single MSBuild node,
    no node reuse, trimming/AOT disabled. The type-registration table
    (libxamarin-app.so) is force-regenerated on every build by the
    ForceFreshTypeRegistration target in FreeserfNet.Android.csproj, so the
    n_onResume/n_loadLibraries UnsatisfiedLinkError (Crash 2) cannot occur.

.PARAMETER Clean
    Delete FreeserfNet.Android\bin and FreeserfNet.Android\obj before building
    (full clean rebuild). Only needed when a full rebuild is desired; the
    default incremental build is already safe against Crash 2.

.PARAMETER FreeserfGameDataPath
    Optional path to a SPAE.PA game data file to bundle into the APK. Without
    it the APK ships without game data and the user imports it via the file
    picker on first start.

.EXAMPLE
    .\build-android.ps1
    .\build-android.ps1 -Clean
    .\build-android.ps1 -FreeserfGameDataPath "D:\freeserf.net\SPAE.PA"
#>
param(
    [switch]$Clean,
    [string]$FreeserfGameDataPath = ""
)

$ErrorActionPreference = "Stop"
$env:MSBUILDDISABLENODEREUSE = 1

$proj = Join-Path $PSScriptRoot "FreeserfNet.Android\FreeserfNet.Android.csproj"

if ($Clean) {
    Write-Host "Full clean: deleting FreeserfNet.Android\bin and obj..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force (Join-Path $PSScriptRoot "FreeserfNet.Android\bin") -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force (Join-Path $PSScriptRoot "FreeserfNet.Android\obj") -ErrorAction SilentlyContinue
}

$args = @("build", $proj, "-c", "Release", "-m:1", "-nodeReuse:false",
    "-p:PublishTrimmed=false", "-p:RunAOTCompilation=false")
if ($FreeserfGameDataPath) {
    $args += "-p:FreeserfGameDataPath=$FreeserfGameDataPath"
}

Write-Host "Building Android APK..." -ForegroundColor Cyan
& dotnet @args
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build FAILED (exit code $LASTEXITCODE)." -ForegroundColor Red
    exit $LASTEXITCODE
}

$apk = Join-Path $PSScriptRoot "FreeserfNet.Android\bin\Release\net10.0-android\net.freeserf.android-Signed.apk"
if (Test-Path $apk) {
    Write-Host "APK: $apk" -ForegroundColor Green
    Write-Host "Smoke test: .\test-android.ps1" -ForegroundColor Green
} else {
    Write-Host "Build succeeded but APK not found at expected path: $apk" -ForegroundColor Yellow
}
