<#
.SYNOPSIS
Run one actual dedicated server and two client players for the opt-in debug-aircraft authority proof.
.DESCRIPTION
Clients use real Netcode worlds and UDP but deliberately run without graphics. This is not a rendering, WAN or capacity benchmark.
Raw Unity logs stay ignored; only selected FIRELINE markers and checked JSON/CSV artifacts are published.
.PARAMETER EvidenceRoot
Fresh repository-relative destination; existing proof evidence is never overwritten.
#>
param([ValidateRange(1024,65535)][int]$Port=8230,
    [string]$EvidenceRoot='docs/reports/m03-flight-network-evidence')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$server=Join-Path $project 'Builds/WindowsServer/FirelineServer.exe'
$client=Join-Path $project 'Builds/WindowsAircraftPreview/FirelineAircraftPreview.exe'
$raw=Join-Path $project ('Builds/M03FlightNetwork/'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
$evidence=Join-Path $project $EvidenceRoot
if(Test-Path (Join-Path $evidence 'process-summary.json')){throw 'Use a fresh evidence directory; published proofs are immutable.'}
New-Item -ItemType Directory -Force $raw,$evidence | Out-Null
if(Get-NetUDPEndpoint -LocalPort $Port -ErrorAction SilentlyContinue){throw 'Proof UDP port is occupied.'}
$processes=@(); $records=@()
try{
    foreach($role in @('server','owner','observer')){
        if($role -eq 'owner'){
            $ready=$false
            foreach($attempt in 1..60){
                if((Test-Path (Join-Path $raw 'server.log')) -and (Select-String -Path (Join-Path $raw 'server.log') -Pattern '^FIRELINE_FLIGHT_SESSION ready=True' -Quiet)){$ready=$true;break}
                Start-Sleep -Milliseconds 500
            }
            if(-not $ready){throw 'Dedicated server did not report flight readiness.'}
        }
        if($role -eq 'observer'){Start-Sleep -Seconds 3}
        $log=Join-Path $raw ($role+'.log'); $proof=Join-Path $raw ($role+'.json')
        $arguments=@('-batchmode','-nographics','-logFile',('"'+$log+'"'),'--fireline-port',$Port,
            '--fireline-flight-session','debug','--fireline-flight-network-proof',('"'+$proof+'"'))
        $exe=$server
        if($role -ne 'server'){$exe=$client; $arguments+=@('--fireline-connect','127.0.0.1','--fireline-flight-probe-role',$role)}
        $process=Start-Process $exe -WorkingDirectory $project -WindowStyle Hidden -PassThru -ArgumentList $arguments
        $processes+=@{role=$role;process=$process;proof=$proof;log=$log}
    }
    $deadline=[DateTime]::UtcNow.AddSeconds(85)
    foreach($entry in $processes){
        while(-not $entry.process.WaitForExit(500)) {if([DateTime]::UtcNow -gt $deadline){throw 'Flight network process timed out.'}}
        $exit=$entry.process.ExitCode
        $markers=@(Get-Content $entry.log | Where-Object {$_ -match '^FIRELINE_'})
        $markers | Set-Content (Join-Path $evidence ($entry.role+'-markers.txt'))
        if($exit -ne 0){throw "Flight $($entry.role) failed with actual exit $exit. Inspect selected markers; raw logs may contain credentials."}
        $data=Get-Content $entry.proof -Raw | ConvertFrom-Json
        if(-not $data.passed){throw "Flight proof result failed: $($entry.role)."}
        $records+=@{role=$entry.role;pid=$entry.process.Id;exitCode=$exit;resultSha256=(Get-FileHash $entry.proof).Hash}
    }
    $processor=Get-CimInstance Win32_Processor | Select-Object -First 1
    $os=Get-CimInstance Win32_OperatingSystem
    $assemblies=@(Get-ChildItem -LiteralPath (Join-Path $project 'Builds/WindowsServer/FirelineServer_Data/Managed') -File | Where-Object Name -like 'Fireline*.dll')
    if($assemblies.Count -ne 3 -or $assemblies.Name -contains 'Fireline.Client.dll'){throw 'Dedicated role assembly boundary failed.'}
    $clientAssemblies=@(Get-ChildItem -LiteralPath (Join-Path $project 'Builds/WindowsAircraftPreview/FirelineAircraftPreview_Data/Managed') -File | Where-Object Name -like 'Fireline*.dll')
    if($clientAssemblies.Count -ne 3 -or $clientAssemblies.Name -contains 'Fireline.Server.dll'){throw 'Client role assembly boundary failed.'}
    @{passed=$true;rawLogs=[IO.Path]::GetRelativePath($project,$raw);runs=$records;
        hardware=@{cpu=$processor.Name;os=$os.Caption;build=$os.BuildNumber;ramMiB=[math]::Round($os.TotalVisibleMemorySize/1024)};
        binaries=@(@{role='server';sha256=(Get-FileHash $server).Hash},@{role='client';sha256=(Get-FileHash $client).Hash});
        runtimeHashes=@($assemblies | ForEach-Object {@{name=$_.Name;sha256=(Get-FileHash $_.FullName).Hash}})
        clientRuntimeHashes=@($clientAssemblies | ForEach-Object {@{name=$_.Name;sha256=(Get-FileHash $_.FullName).Hash}})
    } | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $evidence 'process-summary.json') -Encoding utf8
    python (Join-Path $PSScriptRoot 'analyze_m03_flight_network.py') --capture $raw --evidence $evidence
    if($LASTEXITCODE -ne 0){throw 'Independent flight-network artifact verification failed.'}
    'M03_FLIGHT_NETWORK_PROCESSES_PASSED'
}finally{foreach($entry in $processes){if(-not $entry.process.HasExited){Stop-Process -Id $entry.process.Id}}}
