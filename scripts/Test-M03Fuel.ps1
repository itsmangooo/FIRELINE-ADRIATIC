<#
.SYNOPSIS
Run two actual dedicated-player numerical proofs for finite fuel and preserve legacy flight regression evidence.
.PARAMETER EvidenceRoot
Fresh destination; published artifacts are never overwritten by this harness.
#>
param([ValidateRange(1024,65535)][int]$Port=8360,
    [string]$EvidenceRoot='docs/reports/m03-fuel-evidence/numerical')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$binary=Join-Path $project 'Builds/WindowsServer/FirelineServer.exe'
$raw=Join-Path $project ('Builds/M03Fuel/'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
$evidence=Join-Path $project $EvidenceRoot
if(Test-Path (Join-Path $evidence 'process-summary.json')){throw 'Choose a fresh evidence directory.'}
if(Get-NetUDPEndpoint -LocalPort $Port -ErrorAction SilentlyContinue){throw 'Proof port is occupied.'}
New-Item -ItemType Directory -Force $raw,$evidence | Out-Null
$records=@()
foreach($run in 1..2){
    $log=Join-Path $raw "run-$run.log"; $proof=Join-Path $raw "run-$run.json"
    $process=Start-Process $binary -WorkingDirectory $project -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode','-nographics','-logFile',('"'+$log+'"'),
        '--fireline-port',$Port,'--fireline-flight-proof',('"'+$proof+'"'))
    try{
        $deadline=[DateTime]::UtcNow.AddSeconds(120)
        while(-not $process.WaitForExit(500)){if([DateTime]::UtcNow -gt $deadline){throw 'Fuel numerical proof timed out.'}}
        if($process.ExitCode -ne 0){throw "Fuel proof actual exit $($process.ExitCode)."}
        $data=Get-Content $proof -Raw | ConvertFrom-Json
        if(-not $data.passed -or -not $data.headless -or $data.fuel.profile -ne 'DEBUG_FUEL_V1' -or $data.fuel.cases.Count -ne 10){throw 'Missing fuel proof data.'}
        @(Get-Content $log | Where-Object {$_ -match '^FIRELINE_'}) | Set-Content (Join-Path $evidence "run-$run-markers.txt")
        $records+=@{run=$run;pid=$process.Id;exitCode=$process.ExitCode;resultSha256=(Get-FileHash $proof).Hash}
    }finally{if(-not $process.HasExited){Stop-Process -Id $process.Id}}
}
@{passed=$true;rawLogs=[IO.Path]::GetRelativePath($project,$raw);runs=$records;
    runtimeHashes=@(Get-ChildItem Builds/WindowsServer/FirelineServer_Data/Managed -Filter 'Fireline*.dll' | ForEach-Object {@{name=$_.Name;sha256=(Get-FileHash $_.FullName).Hash}})
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $evidence 'process-summary.json') -Encoding utf8
python (Join-Path $PSScriptRoot 'analyze_m03_fuel.py') --capture $raw --evidence $evidence
if($LASTEXITCODE -ne 0){throw 'Independent fuel accounting or regression check failed.'}
'M03_FUEL_PROCESSES_PASSED'
