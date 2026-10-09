<#
.SYNOPSIS
    Smoke-tests the freeserf.net Android APK on a connected device/emulator.

.DESCRIPTION
    Installs the APK, clears logcat, launches the app, waits, and greps logcat
    for the Crash 2 signature (UnsatisfiedLinkError / n_onResume /
    n_loadLibraries / FATAL). Exits non-zero if a crash is detected, so a
    broken APK is caught immediately after a build instead of later.

.PARAMETER Apk
    Path to the APK to install. Defaults to the Release build output.

.PARAMETER WaitSeconds
    How long to wait after launch before checking logcat (default 30).

.PARAMETER Adb
    Path to adb.exe. Defaults to "adb" (on PATH).

.EXAMPLE
    .\test-android.ps1
    .\test-android.ps1 -WaitSeconds 45
#>
param(
    [string]$Apk = "FreeserfNet.Android\bin\Release\net10.0-android\net.freeserf.android-Signed.apk",
    [int]$WaitSeconds = 30,
    [string]$Adb = "adb"
)

$ErrorActionPreference = "Stop"
$apk = Join-Path $PSScriptRoot $Apk
if (-not (Test-Path $apk)) {
    Write-Error "APK not found: $apk (build first with .\build-android.ps1)"
}

Write-Host "Installing $apk ..." -ForegroundColor Cyan
& $Adb install -r $apk
if ($LASTEXITCODE -ne 0) {
    Write-Error "adb install failed (exit code $LASTEXITCODE)."
}

& $Adb logcat -c
& $Adb shell am force-stop net.freeserf.android
& $Adb shell am start -n net.freeserf.android/crc64bcc776d209640335.MainActivity

Write-Host "Waiting $WaitSeconds s for app startup..." -ForegroundColor Cyan
Start-Sleep -Seconds $WaitSeconds

$log = & $Adb logcat -d
$crash = $log | Select-String -Pattern "UnsatisfiedLinkError|n_onResume|n_loadLibraries|FATAL UNHANDLED|AndroidRuntime.*FATAL"

if ($crash) {
    Write-Host "CRASH DETECTED (Crash 2 signature):" -ForegroundColor Red
    $crash | Select-Object -First 20 | ForEach-Object { Write-Host $_.Line }
    exit 1
}

Write-Host "No type-registration crash detected. App launched cleanly." -ForegroundColor Green
