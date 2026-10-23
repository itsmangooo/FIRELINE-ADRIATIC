<#
.SYNOPSIS
Verify three actual clients, late join, repair and exact checkpoint state across two separate server processes.
.DESCRIPTION
Uses existing current Windows builds. Optional UDP proxies impose real seeded 80 ms constant one-way delay and 5% datagram loss.
Writes fresh scoped evidence, hashes and real screenshots. Stops only processes started by this invocation.
.PARAMETER Impaired
Route every client through an independent loopback impairment socket; this is not a WAN/capacity qualification.
.PARAMETER Port
Server port; the next three ports are reserved for optional proxies.
.PARAMETER JitterMs
Optional per-direction jitter to reproduce NET-01; zero is the verified impairment. Jittered outputs use a separate case folder.
#>
param([switch]$Impaired, [ValidateRange(1024,65000)][int]$Port = 8070, [ValidateRange(0,80)][double]$JitterMs = 0)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$serverBinary = Join-Path $project 'Builds/WindowsServer/FirelineServer.exe'
$clientBinary = Join-Path $project 'Builds/WindowsClient/FirelineClient.exe'
$caseName = if ($Impaired -and $JitterMs -gt 0) { 'jittered' } elseif ($Impaired) { 'impaired' } else { 'direct' }
$raw = Join-Path $project ('Builds/M02Replication/' + $caseName + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
$published = Join-Path $project ('docs/reports/m02-evidence/' + $caseName)
$previews = Join-Path $project 'docs/previews/m02-replication'
New-Item -ItemType Directory -Force $raw,$published,$previews | Out-Null
foreach ($number in $Port..($Port+3)) {
    if (Get-NetUDPEndpoint -LocalPort $number -ErrorAction SilentlyContinue) { throw "UDP port $number is occupied." }
}
if (-not ('FirelineM02Windows' -as [type])) {
    # Expose a portion of every real client window without changing the requested 1280x720 viewport.
    Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class FirelineM02Windows {
 [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr w, IntPtr after, int x, int y, int width, int height, uint flags);
}
'@
}

function Wait-Condition([scriptblock]$Condition, [int]$Seconds, [string]$Failure) {
    # Poll an explicit condition with a bounded wall deadline; elapsed time never substitutes for a successful check.
    $deadline = [DateTime]::UtcNow.AddSeconds($Seconds)
    while (-not (& $Condition)) { if ([DateTime]::UtcNow -gt $deadline) { throw $Failure }; Start-Sleep -Milliseconds 250 }
}

function Has-Marker([string]$Path, [string]$Pattern) {
    # Read only an existing log for a specific marker; callers never publish raw Unity logs or command-line credentials.
    return (Test-Path -LiteralPath $Path) -and ((Get-Content -LiteralPath $Path -Raw) -match $Pattern)
}

$owned = @()
$results = @()
$worldPath = Join-Path $raw 'world.fls'
try {
    foreach ($phase in @('initial','restart')) {
        $phaseRaw = Join-Path $raw $phase; $phasePublished = Join-Path $published $phase
        New-Item -ItemType Directory -Force $phaseRaw,$phasePublished | Out-Null
        $stopProxy = Join-Path $phaseRaw 'stop-proxy'
        $proxyReport = Join-Path $phaseRaw 'proxy.json'
        $proxy = $null
        if ($Impaired) {
            $proxyArgs = @(('"'+(Join-Path $PSScriptRoot 'm02_udp_proxy.py')+'"'),'--server',$Port,'--ports',($Port+1),($Port+2),($Port+3),
                '--output',('"'+$proxyReport+'"'),'--stop-file',('"'+$stopProxy+'"'),'--seconds','115','--jitter-ms',$JitterMs)
            $proxy = Start-Process -FilePath (Get-Command python).Source -WorkingDirectory $project -ArgumentList $proxyArgs -WindowStyle Hidden -PassThru
            $owned += $proxy
            Wait-Condition { Test-Path ([IO.Path]::ChangeExtension($proxyReport,'.ready')) } 10 'UDP proxy did not bind.'
        }
        $serverLog = Join-Path $phaseRaw 'server.log'
        $serverProof = Join-Path $phaseRaw 'server.json'
        $serverArguments = @('-batchmode','-nographics','-logFile',('"'+$serverLog+'"'),'--fireline-bind','127.0.0.1','--fireline-port',$Port,
            '--fireline-min-clients','3','--fireline-replication-proof',('"'+$serverProof+'"'),'--fireline-world-save',('"'+$worldPath+'"'))
        if ($phase -eq 'restart') { $serverArguments += @('--fireline-world-load',('"'+$worldPath+'"')) }
        $server = Start-Process -FilePath $serverBinary -WorkingDirectory $project -WindowStyle Hidden -PassThru -ArgumentList $serverArguments
        $owned += $server
        Wait-Condition { (Has-Marker $serverLog 'FIRELINE_WORLD_READY') -and (Has-Marker $serverLog 'FIRELINE_SERVER_HEADLESS valid=True') } 30 'Server failed startup/headless checks.'
        $clients = @()
        foreach ($number in 1..3) {
            if ($phase -eq 'initial' -and $number -eq 3) {
                Wait-Condition { Has-Marker $serverLog 'FIRELINE_PROOF_EVENT waterKg=14700 tick=40' } 70 'Two-client scenario never reached suppression.'
            }
            $clientProof = Join-Path $phaseRaw "client-$number.json"
            $clientLog = Join-Path $phaseRaw "client-$number.log"
            $targetPort = if ($Impaired) { $Port + $number } else { $Port }
            $arguments = @('-force-d3d11','-logFile',('"'+$clientLog+'"'),'-screen-width','1280','-screen-height','720','-screen-fullscreen','0',
                '--fireline-connect','127.0.0.1','--fireline-port',$targetPort,'--fireline-replication-proof',('"'+$clientProof+'"'))
            $client = Start-Process -FilePath $clientBinary -WorkingDirectory $project -WindowStyle Normal -PassThru -ArgumentList $arguments
            $owned += $client; $clients += @{ process=$client; path=$clientProof; log=$clientLog; number=$number }
            # Isolate HDRP startup/handshake from the next player's GPU/resource initialization; all three still overlap during convergence.
            if ($phase -eq 'initial' -and $number -eq 1) {
                Wait-Condition { Has-Marker $clientLog 'FIRELINE_FIRE_APPLY' } 40 "Client $number did not receive its first baseline."
            }
        }
        $deadline = [DateTime]::UtcNow.AddSeconds(110)
        while (-not $server.HasExited -or @($clients | Where-Object { -not $_.process.HasExited }).Count -gt 0) {
            if ([DateTime]::UtcNow -gt $deadline) { throw 'M02 real-process verification timed out.' }
            foreach ($entry in $clients) {
                $entry.process.Refresh()
                if (-not $entry.process.HasExited -and $entry.process.MainWindowHandle -ne [IntPtr]::Zero) {
                    [void][FirelineM02Windows]::SetWindowPos($entry.process.MainWindowHandle,[IntPtr]::Zero,(($entry.number-1)*350),(($entry.number-1)*30),0,0,0x0015)
                }
            }
            Start-Sleep -Milliseconds 300
        }
        $server.WaitForExit()
        if ($server.ExitCode -ne 0) { throw "Server proof failed with exit $($server.ExitCode)." }
        $state = Get-Content -LiteralPath $serverProof -Raw | ConvertFrom-Json
        if (-not $state.passed -or $state.tick -ne 120 -or $state.charredCells -le 0 -or $state.waterKg -ne 14700 -or $state.peers.Count -ne 3) { throw 'Incomplete authoritative proof.' }
        $serverStateHash = (Get-FileHash ([IO.Path]::ChangeExtension($serverProof,'.fls')) -Algorithm SHA256).Hash
        if ((Get-FileHash $worldPath -Algorithm SHA256).Hash -ne $serverStateHash) { throw 'Saved checkpoint does not equal the final authority.' }
        if ($phase -eq 'restart' -and ($state.loadedChecksum -ne $results[0].checksum -or $state.checksum -ne $results[0].checksum)) { throw 'Separate server process did not restore identical state.' }
        $clientResults = @()
        foreach ($entry in $clients) {
            $entry.process.WaitForExit()
            $data = Get-Content -LiteralPath $entry.path -Raw | ConvertFrom-Json
            if ($entry.process.ExitCode -ne 0 -or -not $data.passed -or $data.checksum -ne $state.checksum -or $data.tick -ne 120) { throw "Client $($entry.number) failed convergence." }
            $hash = (Get-FileHash ([IO.Path]::ChangeExtension($entry.path,'.fls')) -Algorithm SHA256).Hash
            if ($hash -ne $serverStateHash) { throw 'Canonical client/server checkpoint bytes disagree.' }
            if ($phase -eq 'initial' -and $entry.number -eq 3 -and ($data.initialTick -lt 40 -or $data.initialCharredCells -le 0)) { throw 'Late join did not receive existing char.' }
            foreach ($suffix in @('.json','.fls','.metrics.json','.metrics.csv')) {
                $file = [IO.Path]::ChangeExtension($entry.path,$suffix)
                if (Test-Path $file) { Copy-Item -LiteralPath $file -Destination $phasePublished -Force }
            }
            if ($phase -eq 'initial') {
                Copy-Item -LiteralPath ([IO.Path]::ChangeExtension($entry.path,'.png')) -Destination (Join-Path $previews "$caseName-client-$($entry.number)_after.png") -Force
            }
            Get-Content -LiteralPath $entry.log | Where-Object { $_ -match '^FIRELINE_' } | Set-Content (Join-Path $phasePublished "client-$($entry.number)-markers.txt")
            $clientResults += @{ pid=$entry.process.Id; exitCode=$entry.process.ExitCode; number=$entry.number; networkId=$data.networkId; checksum=$data.checksum; canonicalSha256=$hash;
                baselines=$data.baselines; deltas=$data.deltas; repairs=$data.repairs; initialTick=$data.initialTick; initialCharredCells=$data.initialCharredCells }
        }
        if (@($clientResults.networkId | Sort-Object -Unique).Count -ne 3) { throw 'Client network identities are not distinct.' }
        if ($phase -eq 'initial' -and (($clientResults | Measure-Object deltas -Sum).Sum -lt 2 -or ($clientResults | Measure-Object repairs -Sum).Sum -lt 1)) { throw 'No actual deltas or injected-gap repair were verified.' }
        if ($proxy) {
            New-Item -ItemType File -Force $stopProxy | Out-Null
            $proxy.WaitForExit(10000) | Out-Null
            if (-not $proxy.HasExited -or $proxy.ExitCode -ne 0) { throw 'UDP proxy did not finish its counters.' }
            $proxyData = Get-Content $proxyReport -Raw | ConvertFrom-Json
            if (($proxyData.clients | Measure-Object randomDrops -Sum).Sum -le 0) { throw 'Impairment run recorded no actual packet drops.' }
            Copy-Item $proxyReport $phasePublished -Force
        }
        Copy-Item -LiteralPath $serverProof,([IO.Path]::ChangeExtension($serverProof,'.fls')) -Destination $phasePublished -Force
        Get-Content $serverLog | Where-Object { $_ -match '^FIRELINE_' } | Set-Content (Join-Path $phasePublished 'server-markers.txt')
        $results += @{ phase=$phase; passed=$true; serverPid=$server.Id; serverExitCode=$server.ExitCode; checksum=$state.checksum;
            restoredChecksum=$state.loadedChecksum; canonicalSha256=$serverStateHash; clients=$clientResults }
        "M02_PHASE_PASSED case=$caseName phase=$phase checksum=$($state.checksum)"
    }
    [ordered]@{ passed=$true; scenario=$caseName; scope='Three actual rendered Windows clients; initial late join and separate server restart; synthetic M02 world';
        phases=$results; rawLogs=[IO.Path]::GetRelativePath($project,$raw);
        runtimeHashes=@(foreach ($role in @('WindowsServer','WindowsClient')) { Get-ChildItem (Join-Path $project "Builds/$role") -Directory | Where-Object Name -like '*_Data' | ForEach-Object { Get-ChildItem -LiteralPath (Join-Path $_.FullName 'Managed') -File | Where-Object Name -like 'Fireline*.dll' } | ForEach-Object { @{role=$role;name=$_.Name;sha256=(Get-FileHash $_.FullName).Hash} } })
    } | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $published 'summary.json') -Encoding utf8
}
finally {
    foreach ($process in $owned) { if (-not $process.HasExited) { Stop-Process -Id $process.Id -ErrorAction SilentlyContinue } }
}
