<#
.SYNOPSIS
Verify the M03 rendered client, synthetic keyboard/gamepad controls, actual server authority and a late headless observer.
.DESCRIPTION
The visible client is required for visual QA/capture. Synthetic Input System events do not send native OS keys.
Raw Unity logs remain ignored; only scoped markers, checked traces, screenshots and role hashes are published.
.PARAMETER EvidenceRoot
Fresh repository-relative destination; never overwrite an accepted proof.
.PARAMETER CapturePath
Actual chase PNG destination; the second external view uses the paired .inspection.png filename.
#>
param([ValidateRange(1024,65535)][int]$Port=8350,
    [string]$EvidenceRoot='docs/reports/m03-flight-view-evidence',
    [string]$CapturePath='docs/previews/m03-flight-view/after.png')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$server=Join-Path $project 'Builds/WindowsServer/FirelineServer.exe'
$client=Join-Path $project 'Builds/WindowsFlightTest/FirelineFlightTest.exe'
$raw=Join-Path $project ('Builds/M03FlightView/'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
$evidence=Join-Path $project $EvidenceRoot
$capture=Join-Path $project $CapturePath
if(Test-Path (Join-Path $evidence 'process-summary.json')){throw 'Select a fresh evidence directory.'}
New-Item -ItemType Directory -Force $raw,$evidence,(Split-Path $capture -Parent) | Out-Null
if(Get-NetUDPEndpoint -LocalPort $Port -ErrorAction SilentlyContinue){throw 'Proof UDP port is occupied.'}
$processes=@();$records=@()
try{
    foreach($role in @('server','view-owner','observer')){
        if($role -eq 'view-owner'){
            $ready=$false
            foreach($attempt in 1..60){
                if((Test-Path (Join-Path $raw 'server.log')) -and (Select-String -Path (Join-Path $raw 'server.log') -Pattern '^FIRELINE_FLIGHT_SESSION ready=True' -Quiet)){$ready=$true;break}
                Start-Sleep -Milliseconds 500
            }
            if(-not $ready){throw 'Dedicated flight host did not report readiness.'}
        }
        if($role -eq 'observer'){
            $owned=$false
            foreach($attempt in 1..60){
                $trace=Join-Path $raw 'view-owner.csv'
                if((Test-Path $trace) -and (Select-String -Path $trace -Pattern '^\S+,\S+,\d+,\d+,[1-9]\d*,' -Quiet)){$owned=$true;break}
                Start-Sleep -Milliseconds 500
            }
            if(-not $owned){throw 'Rendered owner did not confirm its occupied seat before observer admission.'}
            Start-Sleep -Seconds 1
        }
        $log=Join-Path $raw ($role+'.log');$proof=Join-Path $raw ($role+'.json')
        $arguments=@('-logFile',('"'+$log+'"'),'--fireline-port',$Port,'--fireline-flight-session','debug')
        $exe=$client;$style='Hidden'
        if($role -eq 'server'){$exe=$server;$arguments+=@('-batchmode','-nographics','--fireline-flight-network-proof',('"'+$proof+'"'))}
        elseif($role -eq 'observer'){$arguments+=@('-batchmode','-nographics','--fireline-connect','127.0.0.1','--fireline-flight-network-proof',('"'+$proof+'"'),'--fireline-flight-probe-role','observer')}
        else{
            # This is an interactive game under visual QA; hidden windows can stop actual presentation on Windows.
            $style='Normal'
            $arguments+=@('-force-d3d11','-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','--fireline-connect','127.0.0.1',
                '--fireline-quality','Balanced','--fireline-flight-view','debug','--fireline-flight-view-proof',('"'+$proof+'"'),
                '--fireline-capture',('"'+$capture+'"'),'--fireline-metrics',('"'+(Join-Path $raw 'metrics.json')+'"'))
        }
        $process=Start-Process $exe -WorkingDirectory $project -WindowStyle $style -PassThru -ArgumentList $arguments
        $processes+=@{role=$role;process=$process;proof=$proof;log=$log}
    }
    $deadline=[DateTime]::UtcNow.AddSeconds(100)
    foreach($entry in $processes){
        while(-not $entry.process.WaitForExit(500)){if([DateTime]::UtcNow -gt $deadline){throw 'Rendered flight proof timed out.'}}
        $markers=@(Get-Content $entry.log | Where-Object {$_ -match '^FIRELINE_'})
        $markers | Set-Content (Join-Path $evidence ($entry.role+'-markers.txt'))
        if($entry.process.ExitCode -ne 0){throw "Flight $($entry.role) failed with actual exit $($entry.process.ExitCode); inspect scoped markers and ignored raw logs."}
        if(Select-String -Path $entry.log -Pattern '^(NullReferenceException|InvalidOperationException|ArgumentException|Exception:)' -Quiet){throw "Unhandled exception in $($entry.role); inspect ignored raw log."}
        $data=Get-Content $entry.proof -Raw | ConvertFrom-Json
        if(-not $data.passed){throw "Failed $($entry.role) proof state."}
        $records+=@{role=$entry.role;pid=$entry.process.Id;exitCode=$entry.process.ExitCode;resultSha256=(Get-FileHash $entry.proof).Hash}
    }
    $serverDll=@(Get-ChildItem Builds/WindowsServer/FirelineServer_Data/Managed -Filter 'Fireline*.dll')
    $clientDll=@(Get-ChildItem Builds/WindowsFlightTest/FirelineFlightTest_Data/Managed -Filter 'Fireline*.dll')
    if($serverDll.Count -ne 3 -or $clientDll.Count -ne 3 -or $serverDll.Name -contains 'Fireline.Client.dll' -or $clientDll.Name -contains 'Fireline.Server.dll'){throw 'Role assembly isolation failed.'}
    @{passed=$true;rawLogs=[IO.Path]::GetRelativePath($project,$raw);runs=$records;port=$Port;capture=$CapturePath;
        serverRuntimeHashes=@($serverDll | ForEach-Object {@{name=$_.Name;sha256=(Get-FileHash $_.FullName).Hash}});
        clientRuntimeHashes=@($clientDll | ForEach-Object {@{name=$_.Name;sha256=(Get-FileHash $_.FullName).Hash}})
    } | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $evidence 'process-summary.json') -Encoding utf8
    Copy-Item (Join-Path $raw 'metrics.json') (Join-Path $evidence 'metrics.json')
    Copy-Item (Join-Path $raw 'metrics.csv') (Join-Path $evidence 'metrics.csv')
    python (Join-Path $PSScriptRoot 'analyze_m03_flight_view.py') --capture $raw --evidence $evidence --image $capture
    if($LASTEXITCODE -ne 0){throw 'Independent rendered flight evidence check failed.'}
    'M03_FLIGHT_VIEW_PROCESSES_PASSED'
}finally{foreach($entry in $processes){if(-not $entry.process.HasExited){Stop-Process -Id $entry.process.Id}}}
