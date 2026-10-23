<#
.SYNOPSIS
Verify real heartbeat loss/disconnection and refusal of a corrupt startup checkpoint.
.DESCRIPTION
Uses only task-owned Windows processes and loopback sockets. Expected nonzero player exits are negative-test successes, not successful gameplay runs.
#>
param([int]$Port = 8080)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$raw=Join-Path $project ('Builds/M02Negative/'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
$evidence=Join-Path $project 'docs/reports/m02-evidence/negative'
New-Item -ItemType Directory -Force $raw,$evidence | Out-Null
$serverExe=Join-Path $project 'Builds/WindowsServer/FirelineServer.exe'
$clientExe=Join-Path $project 'Builds/WindowsClient/FirelineClient.exe'
$owned=@()

function Wait-Marker([string]$Path,[string]$Marker,[int]$Seconds) {
    # Require an actual log marker within the deadline; never infer completion from sleeping alone.
    $end=[DateTime]::UtcNow.AddSeconds($Seconds)
    while (-not ((Test-Path $Path) -and ((Get-Content $Path -Raw) -match $Marker))) {
        if([DateTime]::UtcNow -gt $end){throw "Missing marker $Marker"}; Start-Sleep -Milliseconds 250
    }
}

try {
    $serverLog=Join-Path $raw 'server.log'; $clientLog=Join-Path $raw 'client.log'; $clientProof=Join-Path $raw 'client.json'
    $blackout=Join-Path $raw 'blackout'; $stop=Join-Path $raw 'stop'; $proxyReport=Join-Path $raw 'proxy.json'
    $proxyArgs=@(('"'+(Join-Path $PSScriptRoot 'm02_udp_proxy.py')+'"'),'--server',$Port,'--ports',($Port+1),'--delay-ms','0','--jitter-ms','0','--loss','0',
        '--output',('"'+$proxyReport+'"'),'--stop-file',('"'+$stop+'"'),'--blackout-file',('"'+$blackout+'"'),'--seconds','100')
    $proxy=Start-Process (Get-Command python).Source -WorkingDirectory $project -ArgumentList $proxyArgs -WindowStyle Hidden -PassThru; $owned+=$proxy
    $server=Start-Process $serverExe -WorkingDirectory $project -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode','-nographics','-logFile',('"'+$serverLog+'"'),'--fireline-port',$Port)
    $owned+=$server; Wait-Marker $serverLog 'FIRELINE_WORLD_READY' 30
    $client=Start-Process $clientExe -WorkingDirectory $project -WindowStyle Normal -PassThru -ArgumentList @('-force-d3d11','-logFile',('"'+$clientLog+'"'),'-screen-width','1280','-screen-height','720','-screen-fullscreen','0',
        '--fireline-connect','127.0.0.1','--fireline-port',($Port+1),'--fireline-replication-proof',('"'+$clientProof+'"'))
    $owned+=$client; Wait-Marker $clientLog 'FIRELINE_FIRE_APPLY' 40
    New-Item -ItemType File $blackout | Out-Null
    Wait-Marker $serverLog 'FIRELINE_FIRE_TIMEOUT' 35
    if(-not $client.WaitForExit(35000)){throw 'Silent client never exited.'}
    $data=Get-Content $clientProof -Raw | ConvertFrom-Json
    if($client.ExitCode -ne 9 -or $data.passed -or $data.reason -notin @('heartbeat-timeout','disconnected')){throw 'Invalid disconnect result.'}
    New-Item -ItemType File $stop | Out-Null; [void]$proxy.WaitForExit(10000)
    Copy-Item $clientProof,$proxyReport $evidence -Force
    foreach($entry in @(@{path=$serverLog;name='server'},@{path=$clientLog;name='client'})) {
        Get-Content $entry.path | Where-Object {$_ -match '^FIRELINE_'} | Set-Content (Join-Path $evidence ($entry.name+'-markers.txt'))
    }
    Stop-Process -Id $server.Id
    # A separate fresh process must reject corrupt input before WORLD_READY or any checkpoint overwrite.
    $original=Join-Path $project 'docs/reports/m02-evidence/direct/initial/server.fls'
    $valid=Join-Path $raw 'preserved.fls'; Copy-Item $original $valid
    $hash=(Get-FileHash $valid).Hash; $corrupt=Join-Path $raw 'corrupt.fls'
    $bytes=[IO.File]::ReadAllBytes($valid); $bytes[70]=$bytes[70] -bxor 1; [IO.File]::WriteAllBytes($corrupt,$bytes)
    $badLog=Join-Path $raw 'corrupt.log'
    $bad=Start-Process $serverExe -WorkingDirectory $project -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode','-nographics','-logFile',('"'+$badLog+'"'),
        '--fireline-port',$Port,'--fireline-world-load',('"'+$corrupt+'"'),'--fireline-world-save',('"'+$valid+'"'))
    $owned+=$bad
    if(-not $bad.WaitForExit(45000) -or $bad.ExitCode -ne 6){throw 'Corrupt checkpoint startup did not fail with exit 6.'}
    if((Get-FileHash $valid).Hash -ne $hash -or (Get-Content $badLog -Raw) -match 'FIRELINE_WORLD_READY'){throw 'Bad checkpoint reset or overwrote state.'}
    Get-Content $badLog | Where-Object {$_ -match '^FIRELINE_'} | Set-Content (Join-Path $evidence 'corrupt-markers.txt')
    [ordered]@{passed=$true;disconnectExpectedExit=9;disconnectActualExit=$client.ExitCode;disconnectReason=$data.reason;
        corruptExpectedExit=6;corruptActualExit=$bad.ExitCode;previousCheckpointPreserved=$true;rawLogs=[IO.Path]::GetRelativePath($project,$raw)
    } | ConvertTo-Json | Set-Content (Join-Path $evidence 'summary.json') -Encoding utf8
    'M02_NEGATIVE_CASES_PASSED'
}
finally {foreach($process in $owned){if(-not $process.HasExited){Stop-Process -Id $process.Id -ErrorAction SilentlyContinue}}}
