<#
.SYNOPSIS
Run actual dedicated/rendered-owner/late-observer processes for opt-in M03 real-time runway QA.
.DESCRIPTION
Uses normal control RPCs and existing presentation. Full raw logs stay ignored; no native keys or physical controller are exercised.
Choose fresh evidence/capture destinations; failed attempts remain available at their timestamped raw path.
#>
param([ValidateRange(1024,65535)][int]$Port=8390,
    [string]$EvidenceRoot='docs/reports/m03-realtime-evidence/runtime',
    [string]$CapturePath='docs/previews/m03-realtime-flight/after.png')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$server=Join-Path $project 'Builds/WindowsServer/FirelineServer.exe'
$client=Join-Path $project 'Builds/WindowsFlightTest/FirelineFlightTest.exe'
$raw=Join-Path $project ('Builds/M03Realtime/'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
$evidence=Join-Path $project $EvidenceRoot; $capture=Join-Path $project $CapturePath
if(Test-Path (Join-Path $evidence 'process-summary.json')){throw 'Choose a fresh evidence directory.'}
if(Get-NetUDPEndpoint -LocalPort $Port -ErrorAction SilentlyContinue){throw 'QA port is occupied.'}
New-Item -ItemType Directory -Force $raw,$evidence,(Split-Path $capture -Parent) | Out-Null
$processes=@(); $records=@()
try{
    foreach($role in @('server','owner','observer')){
        if($role -ne 'server'){
            $marker=if($role -eq 'owner'){'^FIRELINE_FLIGHT_SESSION ready=True'}else{'^FIRELINE_ROUTE_OWNER identity='}
            $ready=$false
            foreach($attempt in 1..90){
                if((Test-Path (Join-Path $raw 'server.log')) -and (Select-String -Path (Join-Path $raw 'server.log') -Pattern $marker -Quiet)){$ready=$true;break}
                Start-Sleep -Milliseconds 500
            }
            if(-not $ready){throw "Server did not confirm readiness/seat before $role."}
            if($role -eq 'observer'){Start-Sleep -Seconds 2}
        }
        $proof=Join-Path $raw ($role+'.json'); $log=Join-Path $raw ($role+'.log')
        $arguments=@('-logFile',('"'+$log+'"'),'--fireline-port',$Port,'--fireline-flight-session','debug',
            '--fireline-flight-route-proof',('"'+$proof+'"'))
        $exe=$client; $style='Hidden'
        if($role -eq 'server'){$exe=$server; $arguments+=@('-batchmode','-nographics')}
        else{
            $arguments+=@('--fireline-connect','127.0.0.1','--fireline-flight-route-role',$role)
            if($role -eq 'observer'){$arguments+=@('-batchmode','-nographics')}
            else{
                # A rendered game is required for actual frame inspection; hidden windows can stop Windows presentation.
                $style='Normal'
                $arguments+=@('-force-d3d11','-screen-width','1920','-screen-height','1080','-screen-fullscreen','0',
                    '--fireline-quality','Balanced','--fireline-flight-view','debug','--fireline-capture',('"'+$capture+'"'),
                    '--fireline-metrics',('"'+(Join-Path $raw 'metrics.json')+'"'))
            }
        }
        $process=Start-Process $exe -WorkingDirectory $project -WindowStyle $style -PassThru -ArgumentList $arguments
        $processes+=@{role=$role;process=$process;proof=$proof;log=$log}
    }
    $deadline=[DateTime]::UtcNow.AddSeconds(190)
    foreach($entry in $processes){
        while(-not $entry.process.WaitForExit(500)){if([DateTime]::UtcNow -gt $deadline){throw 'Live route timed out.'}}
        @(Get-Content $entry.log | Where-Object {$_ -match '^FIRELINE_'}) | Set-Content (Join-Path $evidence ($entry.role+'-markers.txt'))
        if($entry.process.ExitCode -ne 0){throw "Actual $($entry.role) exit $($entry.process.ExitCode); raw attempt retained at $raw."}
        if(Select-String -Path $entry.log -Pattern '^(NullReferenceException|InvalidOperationException|ArgumentException|Exception:)' -Quiet){throw 'Unhandled player exception.'}
        $data=Get-Content -LiteralPath $entry.proof -Raw | ConvertFrom-Json
        if(-not $data.passed){throw 'Role result failed.'}
        $records+=@{role=$entry.role;pid=$entry.process.Id;exitCode=$entry.process.ExitCode;resultSha256=(Get-FileHash -LiteralPath $entry.proof).Hash}
    }
    $serverDll=@(Get-ChildItem Builds/WindowsServer/FirelineServer_Data/Managed -Filter 'Fireline*.dll')
    $clientDll=@(Get-ChildItem Builds/WindowsFlightTest/FirelineFlightTest_Data/Managed -Filter 'Fireline*.dll')
    if($serverDll.Count -ne 3 -or $clientDll.Count -ne 3 -or $serverDll.Name -contains 'Fireline.Client.dll' -or $clientDll.Name -contains 'Fireline.Server.dll'){throw 'Role isolation failed.'}
    @{passed=$true;rawLogs=[IO.Path]::GetRelativePath($project,$raw);runs=$records;port=$Port;capture=$CapturePath;
        serverRuntimeHashes=@($serverDll | ForEach-Object {@{name=$_.Name;sha256=(Get-FileHash $_.FullName).Hash}});
        clientRuntimeHashes=@($clientDll | ForEach-Object {@{name=$_.Name;sha256=(Get-FileHash $_.FullName).Hash}})
    } | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $evidence 'process-summary.json') -Encoding utf8
    python (Join-Path $PSScriptRoot 'analyze_m03_realtime.py') --capture $raw --evidence $evidence --image $capture
    if($LASTEXITCODE -ne 0){throw 'Independent live route verification failed.'}
    'M03_REALTIME_ROUTE_PASSED'
}finally{
    foreach($entry in $processes){if(-not $entry.process.HasExited){Stop-Process -Id $entry.process.Id}}
}
