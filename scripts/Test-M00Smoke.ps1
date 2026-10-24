<#
.SYNOPSIS
Run one local dedicated server and one or two visible rendered clients to verify connection and capture paths.
.DESCRIPTION
Requires existing role-isolated builds and a free UDP port. Writes fresh markers, screenshot, raw metrics and hashes.
Stops only processes started by this invocation on failure. Connection success does not prove shared gameplay state.
.PARAMETER Port
Loopback UDP port to reserve for this run.
.PARAMETER ClientSeconds
Rendered duration in seconds before each client checks success; server allows another 60 seconds.
.PARAMETER EvidenceRoot
Repository-relative destination for markers, diagnostic metrics and process summary.
.PARAMETER CapturePath
Repository-relative PNG destination for the first client's actual rendered frame.
.PARAMETER ClientRelativeExe
Repository-relative rendered client executable; permits isolated inspection scenes.
.PARAMETER ClientCount
Number of local clients required by the server; defaults to the original two-client proof.
.PARAMETER ForceD3D11
Use the verified diagnostic graphics API without changing the project's API configuration.
.PARAMETER Width
Requested render width; actual dimensions are checked in the saved metrics.
.PARAMETER Height
Requested render height; actual dimensions are checked in the saved metrics.
#>
param(
    [ValidateRange(1024, 65535)][int]$Port = 7979,
    [ValidateRange(10, 120)][int]$ClientSeconds = 20,
    [string]$EvidenceRoot = 'docs/reports/m00-evidence',
    [string]$CapturePath = 'docs/previews/m00-foundation/foundation_after.png',
    [string]$ClientRelativeExe = 'Builds/WindowsClient/FirelineClient.exe',
    [ValidateRange(1,2)][int]$ClientCount = 2,
    [switch]$ForceD3D11,
    [ValidateRange(640,3840)][int]$Width = 1280,
    [ValidateRange(360,2160)][int]$Height = 720
)
$ErrorActionPreference = 'Stop'
if (-not ('FirelineM00WindowPlacement' -as [type])) {
    # SetWindowPos is a native window-placement helper: keep both clients exposed without resizing their render area.
    Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class FirelineM00WindowPlacement
{
    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
'@
}
$projectRoot = Split-Path $PSScriptRoot -Parent
$serverExe = Join-Path $projectRoot 'Builds/WindowsServer/FirelineServer.exe'
$clientExe = Join-Path $projectRoot $ClientRelativeExe
$runDirectory = Join-Path $projectRoot ('Builds/Smoke/' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
$evidenceDirectory = Join-Path $projectRoot $EvidenceRoot
$capture = Join-Path $projectRoot $CapturePath
$metrics = Join-Path $evidenceDirectory 'client-diagnostic-metrics.json'
New-Item -ItemType Directory -Force $runDirectory, $evidenceDirectory | Out-Null
foreach ($binary in @($serverExe, $clientExe)) {
    if (-not (Test-Path -LiteralPath $binary)) { throw "Missing build: $binary" }
}
$serverAssemblies = @(Get-ChildItem (Join-Path $projectRoot 'Builds/WindowsServer/FirelineServer_Data/Managed') -Filter 'Fireline*.dll')
$clientManaged = Join-Path (Split-Path $clientExe -Parent) (([IO.Path]::GetFileNameWithoutExtension($clientExe)) + '_Data/Managed')
$clientAssemblies = @(Get-ChildItem $clientManaged -Filter 'Fireline*.dll')
if ($serverAssemblies.Name -contains 'Fireline.Client.dll' -or $clientAssemblies.Name -contains 'Fireline.Server.dll') {
    throw 'Client/server assembly isolation failed.'
}
if ($serverAssemblies.Name -notcontains 'Fireline.Server.dll' -or $clientAssemblies.Name -notcontains 'Fireline.Client.dll') {
    throw 'Expected role assembly is missing.'
}
if (Get-NetUDPEndpoint -LocalPort $Port -ErrorAction SilentlyContinue) {
    throw "UDP port $Port is already in use. Choose another -Port."
}

$processes = @()
$records = @()
$runStartedUtc = [DateTime]::UtcNow
try {
    $serverLog = Join-Path $runDirectory 'server.log'
    $serverSeconds = $ClientSeconds + 60
    $server = Start-Process -FilePath $serverExe -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -ArgumentList @(
        '-batchmode', '-nographics', '-logFile', ('"' + $serverLog + '"'),
        '--fireline-bind', '127.0.0.1', '--fireline-port', $Port,
        '--fireline-smoke-seconds', $serverSeconds, '--fireline-min-clients', $ClientCount)
    $processes += $server
    $deadline = [DateTime]::UtcNow.AddSeconds(25)
    do {
        if ($server.HasExited) { throw "Server exited during startup: $($server.ExitCode)" }
        $ready = (Test-Path $serverLog) -and ((Get-Content $serverLog -Raw) -match 'FIRELINE_SERVER_HEADLESS valid=True')
        if (-not $ready) { Start-Sleep -Milliseconds 250 }
    } until ($ready -or [DateTime]::UtcNow -gt $deadline)
    if (-not $ready) { throw 'Server startup timed out.' }

    foreach ($number in 1..$ClientCount) {
        $clientLog = Join-Path $runDirectory "client-$number.log"
        $clientArgs = @('-logFile', ('"' + $clientLog + '"'), '-screen-width', $Width, '-screen-height', $Height,
            '-screen-fullscreen', '0', '--fireline-connect', '127.0.0.1', '--fireline-port', $Port,
            '--fireline-smoke-seconds', $ClientSeconds)
        if ($ForceD3D11) { $clientArgs += '-force-d3d11' }
        if ($number -eq 1) { $clientArgs += @('--fireline-capture', ('"' + $capture + '"'), '--fireline-metrics', ('"' + $metrics + '"')) }
        # These are interactive game clients under visual QA. Hidden windows can skip
        # presentation completely on Windows, invalidating screenshots and frame samples.
        $client = Start-Process -FilePath $clientExe -WorkingDirectory $projectRoot -WindowStyle Normal -PassThru -ArgumentList $clientArgs
        $processes += $client
        $records += @{ role = "client-$number"; process = $client; log = $clientLog }
    }
    $records += @{ role = 'server'; process = $server; log = $serverLog }
    $deadline = [DateTime]::UtcNow.AddSeconds($serverSeconds + 60)
    $placedWindows = @{}
    while (@($processes | Where-Object { -not $_.HasExited }).Count -gt 0) {
        if ([DateTime]::UtcNow -gt $deadline) { throw 'Smoke process timeout.' }
        $windowIndex = 0
        foreach ($record in $records | Where-Object { $_.role -like 'client-*' }) {
            $record.process.Refresh()
            if (-not $placedWindows.ContainsKey($record.process.Id) -and $record.process.MainWindowHandle -ne [IntPtr]::Zero) {
                # Keep part of each window exposed; preserve the requested client resolution.
                $placed = [FirelineM00WindowPlacement]::SetWindowPos($record.process.MainWindowHandle, [IntPtr]::Zero, ($windowIndex * 640), 20, 0, 0, 0x0015)
                if ($placed) { $placedWindows[$record.process.Id] = $true }
            }
            $windowIndex++
        }
        Start-Sleep -Milliseconds 500
    }

    # Validate every exit/marker and publish only FIRELINE diagnostics, avoiding unrelated raw-log contents.
    $results = foreach ($record in $records) {
        $record.process.WaitForExit()
        $logText = Get-Content -LiteralPath $record.log -Raw
        $markers = @(Get-Content -LiteralPath $record.log | Where-Object { $_ -match '^FIRELINE_' })
        $markers | Set-Content -LiteralPath (Join-Path $evidenceDirectory ($record.role + '-smoke.txt'))
        if ($record.process.ExitCode -ne 0 -or $logText -notmatch 'SMOKE passed=True' -or
            $logText -match '(?m)^(NullReferenceException|InvalidOperationException|ArgumentException|Exception:)') {
            throw "Smoke failed for $($record.role); exit $($record.process.ExitCode); log $($record.log)"
        }
        if ($record.role -eq 'client-1' -and ($logText -notmatch 'FIRELINE_CAPTURE_SAVED' -or $logText -notmatch 'FIRELINE_METRICS_SAVED')) {
            throw 'Client capture or metrics marker missing.'
        }
        @{ role = $record.role; pid = $record.process.Id; exitCode = $record.process.ExitCode; markers = $markers }
    }
    foreach ($artifact in @($capture, $metrics)) {
        if (-not (Test-Path -LiteralPath $artifact) -or (Get-Item $artifact).LastWriteTimeUtc -lt $runStartedUtc) {
            throw "Fresh runtime artifact missing: $artifact"
        }
    }
    # Bind the run to executable AND managed DLL hashes; the Unity launcher alone does not identify C# changes.
    $summary = @{
        utc = [DateTime]::UtcNow.ToString('o'); scope = "One local dedicated server and $ClientCount rendered client processes; connection handshake only"
        clientExecutable = $ClientRelativeExe; requestedWidth = $Width; requestedHeight = $Height; forcedD3D11 = [bool]$ForceD3D11
        port = $Port; passed = $true; processes = @($results)
        serverSha256 = (Get-FileHash $serverExe -Algorithm SHA256).Hash
        clientSha256 = (Get-FileHash $clientExe -Algorithm SHA256).Hash
        serverRuntimeHashes = @($serverAssemblies | ForEach-Object { @{ name = $_.Name; sha256 = (Get-FileHash $_.FullName -Algorithm SHA256).Hash } })
        clientRuntimeHashes = @($clientAssemblies | ForEach-Object { @{ name = $_.Name; sha256 = (Get-FileHash $_.FullName -Algorithm SHA256).Hash } })
        rawLogs = $runDirectory.Substring($projectRoot.Length + 1)
    }
    $summary | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $evidenceDirectory 'smoke-summary.json')
    $summary | ConvertTo-Json -Depth 6
}
finally {
    # Stop only processes launched by this invocation if a test fails or times out.
    foreach ($process in $processes) {
        if (-not $process.HasExited) { Stop-Process -Id $process.Id -ErrorAction SilentlyContinue }
    }
}
