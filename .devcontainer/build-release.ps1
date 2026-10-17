param(
    [Parameter(Mandatory)]
    [ValidateSet("linux", "windows", "android", "all")]
    [string]$Target
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Invoke-DotNet {
    param([string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

$repositoryRoot = Split-Path $PSScriptRoot -Parent
$artifacts = Join-Path $repositoryRoot "artifacts"
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
$targets = if ($Target -eq "all") { @("linux", "windows", "android") } else { @($Target) }

Push-Location $repositoryRoot
try {
    foreach ($platform in $targets) {
        Write-Host "Building $platform release..."

        if ($platform -eq "android") {
            Invoke-DotNet @("build", "FreeserfNet.Android/FreeserfNet.Android.csproj",
                "-f", "net10.0-android", "-c", "Release",
                "-p:AndroidPackageFormat=apk", "-p:PublishTrimmed=true")

            $apkDirectory = Join-Path $repositoryRoot "FreeserfNet.Android/bin/Release/net10.0-android"
            $signedApk = Join-Path $apkDirectory "net.freeserf.android-Signed.apk"
            if (-not (Test-Path $signedApk)) {
                throw "Expected signed APK not found: $signedApk"
            }
            $destination = Join-Path $artifacts "android"
            New-Item -ItemType Directory -Force -Path $destination | Out-Null
            Copy-Item "$apkDirectory/*.apk" $destination
            Write-Host "APKs: $destination"
            continue
        }

        $configuration = if ($platform -eq "linux") { "LinuxRelease" } else { "WindowsRelease" }
        $constants = $platform.ToUpperInvariant()
        $rid = if ($platform -eq "linux") { "linux-x64" } else { "win-x64" }
        $staging = Join-Path ([System.IO.Path]::GetTempPath()) "freeserf-release-$([guid]::NewGuid())"
        New-Item -ItemType Directory -Path $staging | Out-Null
        try {
            Invoke-DotNet @("publish", "FreeserfNet/FreeserfNet.csproj",
                "-c", $configuration, "-p:DefineConstants=$constants",
                "-p:PublishSingleFile=true", "-r", $rid, "-o", $staging)

            Copy-Item "changelog.txt" $staging
            if ($platform -eq "linux") {
                Copy-Item "FreeserfNet/bass/linux-x64/*.so" $staging
                Get-ChildItem $staging -Filter "*.pdb" | Remove-Item
                $archive = Join-Path $artifacts "Freeserf.net-Linux.tar.gz"
                & tar -C $staging -czf $archive .
                if ($LASTEXITCODE -ne 0) {
                    throw "tar failed with exit code $LASTEXITCODE."
                }
            } else {
                Copy-Item "FreeserfNet/bass/win-x64/*.dll" $staging
                $archive = Join-Path $artifacts "Freeserf.net-Windows.zip"
                Compress-Archive -Path "$staging/*" -DestinationPath $archive -Force
            }
            Write-Host "Archive: $archive"
        } finally {
            Remove-Item -Recurse -Force $staging
        }
    }
} finally {
    Pop-Location
}
