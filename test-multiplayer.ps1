<#
.SYNOPSIS
    Runs an automated multiplayer game with a server and several clients on this machine.

.DESCRIPTION
    Builds the desktop app, starts one host and the given number of clients
    with the -m option (see Freeserf.Core/UI/MultiplayerTestDriver.cs) and
    lets them play for a while. Every instance creates a castle, a lumberjack
    and a road through the regular interface code paths. The windows are tiled
    so they can be watched.

    Afterwards the "[SyncCheck]" lines of all logs are compared. They contain
    a hash of the whole game state at each checkpoint tick. A tick that has
    different hashes on the host and a client means that the games diverged.

    Without AI players the games must never diverge. AI players only run on
    the server, so a client may miss an AI action at a checkpoint while the
    game state update is still on its way. Such a divergence must be fixed by
    the next checkpoint. The script exits non-zero if an instance crashed,
    logged errors or the games diverged (for longer than one checkpoint with AI).

    The game data (SPAE.PA etc.) must be in the repository root.

.PARAMETER Clients
    Number of clients (1-3, default 1).

.PARAMETER AI
    Number of AI players added by the host (default 0).

.PARAMETER Seconds
    How long the game runs before all instances are closed (default 120).

.PARAMETER NoBuild
    Skip building the app.

.PARAMETER Dump
    Write the members of all game objects at each checkpoint to
    logs\multiplayer\state-*.txt. Compare the files of the host and
    a client to see what diverged. This slows down the games.

.EXAMPLE
    .\test-multiplayer.ps1
    .\test-multiplayer.ps1 -Clients 2 -AI 1 -Seconds 300
#>
param(
    [ValidateRange(1, 3)]
    [int]$Clients = 1,
    [ValidateRange(0, 2)]
    [int]$AI = 0,
    [int]$Seconds = 120,
    [switch]$NoBuild,
    [switch]$Dump
)

$ErrorActionPreference = "Stop"

if ($Clients + $AI -gt 3) {
    throw "At most 3 clients and AI players are possible (4 players in total)."
}

$root = $PSScriptRoot
$exe = Join-Path $root "FreeserfNet\bin\WindowsDebug\FreeserfNet.exe"
$logDir = Join-Path $root "logs\multiplayer"

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

New-Item -ItemType Directory -Force $logDir | Out-Null
Get-ChildItem $logDir -Include "*.log","*.txt" -Recurse | Remove-Item

# Read by Freeserf.Core/UI/MultiplayerTestDriver.cs (inherited by the started processes).
$env:FREESERF_MPTEST_DUMP = if ($Dump) { "1" } else { "0" }

function Start-Instance([string]$Name, [string]$Mode, [int]$Index) {
    $x = ($Index % 2) * 650
    $y = [math]::Floor($Index / 2) * 520
    $log = Join-Path $logDir "$Name.log"
    $arguments = @("-m", $Mode, "-r", "640x480", "-p", "$x,$y")

    # The working directory is the root so the game data is found there.
    Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $root `
        -RedirectStandardOutput $log -RedirectStandardError (Join-Path $logDir "$Name.err.log") -PassThru
}

$processes = [ordered]@{}
$processes["host"] = Start-Instance "host" "host:${Clients}:$AI" 0
Start-Sleep -Seconds 3

for ($i = 1; $i -le $Clients; ++$i) {
    $processes["client$i"] = Start-Instance "client$i" "join:localhost" $i
    Start-Sleep -Seconds 1
}

Write-Host "Running for $Seconds seconds. Logs: $logDir"
Start-Sleep -Seconds $Seconds

$failed = $false

foreach ($name in $processes.Keys) {
    $process = $processes[$name]

    if ($process.HasExited) {
        Write-Host "$name exited early with code $($process.ExitCode)." -ForegroundColor Red
        $failed = $true
    }
    else {
        Stop-Process -Id $process.Id
    }
}

Start-Sleep -Seconds 1

# Collect the sync check hashes: tick -> instance -> hash
$hashes = @{}

foreach ($name in $processes.Keys) {
    $log = Join-Path $logDir "$name.log"

    foreach ($line in Select-String -Path $log -Pattern "\[SyncCheck\] tick=(\d+) time=\d+ hash=(\w+)") {
        $tick = [int]$line.Matches[0].Groups[1].Value
        if (-not $hashes.ContainsKey($tick)) { $hashes[$tick] = @{} }
        $hashes[$tick][$name] = $line.Matches[0].Groups[2].Value
    }

    # "bad animation" is a known render warning (a serf counter is negative for a moment).
    $warnings = @(Select-String -Path $log -Pattern "Error: \[UI\] bad animation")
    if ($warnings.Count -gt 0) {
        Write-Host "$name logged $($warnings.Count) bad animation warning(s)." -ForegroundColor Yellow
    }

    $errors = @(Select-String -Path $log -Pattern " Error: |Unhandled exception" | Where-Object { $_.Line -notmatch "\[UI\] bad animation" })
    if ($errors.Count -gt 0) {
        Write-Host "$name logged $($errors.Count) error(s):" -ForegroundColor Yellow
        $errors | Select-Object -First 10 | ForEach-Object { Write-Host "  $($_.Line)" }
        $failed = $true
    }

    foreach ($line in Select-String -Path $log -Pattern "\[MPTest\]") {
        Write-Host "$name $($line.Line)"
    }
}

$compared = 0
$diverged = 0
$consecutive = 0
$maxConsecutive = 0

foreach ($tick in ($hashes.Keys | Sort-Object)) {
    $entry = $hashes[$tick]

    if ($entry.Count -lt 2) { continue }

    ++$compared
    $distinct = @($entry.Values | Sort-Object -Unique)

    if ($distinct.Count -gt 1) {
        ++$diverged
        ++$consecutive
        $maxConsecutive = [math]::Max($maxConsecutive, $consecutive)
        if ($diverged -le 10) {
            $details = ($entry.GetEnumerator() | Sort-Object Key | ForEach-Object { "$($_.Key)=$($_.Value.Substring(0, 8))" }) -join " "
            Write-Host "Tick ${tick}: $details" -ForegroundColor Yellow
        }
    }
    else {
        $consecutive = 0
    }
}

Write-Host "Compared $compared ticks, $diverged diverged (at most $maxConsecutive in a row)."

foreach ($name in $processes.Keys) {
    $requests = @(Select-String -Path (Join-Path $logDir "$name.log") -Pattern "still out of sync").Count
    if ($requests -gt 0) {
        Write-Host "$name requested $requests game state update(s) because of a divergence."
    }
}

if ($compared -eq 0) {
    Write-Host "No ticks could be compared." -ForegroundColor Red
    $failed = $true
}

if (($AI -eq 0 -and $diverged -gt 0) -or $maxConsecutive -gt 1) {
    $failed = $true
}

if ($failed) {
    Write-Host "Multiplayer test FAILED." -ForegroundColor Red
    exit 1
}

Write-Host "Multiplayer test passed." -ForegroundColor Green
