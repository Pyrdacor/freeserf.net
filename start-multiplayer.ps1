<#
.SYNOPSIS
    Starts several instances of the game on this machine for manual multiplayer tests.

.DESCRIPTION
    Builds the desktop app (unless -NoBuild is given) and starts the given number
    of instances side by side. The working directory is the repository root, so
    the game data (SPAE.PA etc.) is found there.

    Then create a server in one instance (Multiplayer -> Create server) and join
    it from the others with the address "localhost".

    For automated tests see test-multiplayer.ps1.

.PARAMETER Count
    Number of instances (1-4, default 2).

.PARAMETER Width
    Window width (default 800). The height is 3/4 of it.

.PARAMETER NoBuild
    Skip building the app.

.PARAMETER Console
    Log to a console window for each instance.

.EXAMPLE
    .\start-multiplayer.ps1
    .\start-multiplayer.ps1 -Count 3 -Width 640 -Console
#>
param(
    [ValidateRange(1, 4)]
    [int]$Count = 2,
    [ValidateRange(640, 1920)]
    [int]$Width = 800,
    [switch]$NoBuild,
    [switch]$Console
)

$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$exe = Join-Path $root "FreeserfNet\bin\WindowsDebug\FreeserfNet.exe"
$height = [int]($Width * 3 / 4)

if (-not (Test-Path (Join-Path $root "SPAE.PA")) -and -not (Test-Path (Join-Path $root "SPAD.PA"))) {
    throw "Game data (SPAE.PA or SPAD.PA) not found in $root."
}

if (-not $NoBuild) {
    $buildOutput = dotnet build (Join-Path $root "FreeserfNet\FreeserfNet.csproj") -c WindowsDebug -v quiet -nologo
    if ($LASTEXITCODE -ne 0) {
        $buildOutput | Select-String " error " | ForEach-Object { Write-Host $_.Line }
        throw "Build failed."
    }
}

# The position given to the game is the one of the client area. So the size of
# the title bar and the window border is needed. The game uses physical pixels.
Add-Type @"
using System.Runtime.InteropServices;
public static class WindowFrame {
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int index);
    public static int[] Get() {
        SetProcessDPIAware();
        int border = GetSystemMetrics(32) + GetSystemMetrics(92); // SM_CXFRAME + SM_CXPADDEDBORDER
        int top = GetSystemMetrics(4) + GetSystemMetrics(33) + GetSystemMetrics(92); // SM_CYCAPTION + SM_CYFRAME + SM_CXPADDEDBORDER
        return new[] { border, top };
    }
}
"@
$frame = [WindowFrame]::Get()
$border = $frame[0]
$titleHeight = $frame[1]

for ($i = 0; $i -lt $Count; ++$i) {
    # Two windows per row, each with its title bar and border visible.
    $x = $border + ($i % 2) * ($Width + 2 * $border)
    $y = $titleHeight + [math]::Floor($i / 2) * ($height + $titleHeight + $border)
    $arguments = @("-r", "${Width}x$height", "-p", "$x,$y")

    if ($Console) {
        $arguments += "-c"
    }

    Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $root | Out-Null
}

Write-Host "Started $Count instance(s). Create a server in one and join with 'localhost' in the others."
