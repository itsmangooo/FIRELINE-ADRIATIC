<#
.SYNOPSIS
Run two separate actual dedicated players for the isolated numerical runway-contact prerequisite.
.DESCRIPTION
No live pilot, graphics or flight replication is exercised. Raw logs remain ignored; only FIRELINE markers and checked artifacts are published.
.PARAMETER EvidenceRoot
Repository-relative output folder; select a fresh folder for later replays to preserve published evidence.
#>
param([ValidateRange(1024,65535)][int]$Port=8210,
    [string]$EvidenceRoot='docs/reports/m03-runway-evidence')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$binary=Join-Path $project 'Builds/WindowsServer/FirelineServer.exe'
$raw=Join-Path $project ('Builds/M03Runway/'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
$evidence=Join-Path $project $EvidenceRoot
New-Item -ItemType Directory -Force $raw,$evidence | Out-Null
if(Get-NetUDPEndpoint -LocalPort $Port -ErrorAction SilentlyContinue){throw 'Proof UDP port is occupied.'}
$records=@()
foreach($run in 1..2){
    $log=Join-Path $raw "run-$run.log"; $proof=Join-Path $raw "run-$run.json"
    $process=Start-Process $binary -WorkingDirectory $project -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode','-nographics','-logFile',('"'+$log+'"'),
        '--fireline-port',$Port,'--fireline-runway-proof',('"'+$proof+'"'))
    try{
        $deadline=[DateTime]::UtcNow.AddSeconds(120)
        while(-not $process.WaitForExit(500)) {if([DateTime]::UtcNow -gt $deadline){throw 'Dedicated runway proof timed out.'}}
        if($process.ExitCode -ne 0){throw "Runway proof failed with actual exit $($process.ExitCode)."}
        $data=Get-Content $proof -Raw | ConvertFrom-Json
        if(-not $data.passed -or -not $data.headless -or $data.profile -ne 'DEBUG_RUNWAY_CONTACT_V1' -or $data.cases.Count -ne 13){throw 'Missing runway proof state.'}
        $markers=@(Get-Content $log | Where-Object {$_ -match '^FIRELINE_'})
        if(-not ($markers -match '^FIRELINE_RUNWAY_PROOF passed=True headless=True')){throw 'No actual dedicated proof marker.'}
        $markers | Set-Content (Join-Path $evidence "run-$run-markers.txt")
        $records+=@{pid=$process.Id;exitCode=$process.ExitCode;run=$run;resultSha256=(Get-FileHash $proof).Hash;csvSha256=(Get-FileHash ([IO.Path]::ChangeExtension($proof,'.csv'))).Hash}
    }finally{if(-not $process.HasExited){Stop-Process -Id $process.Id}}
}
$processor=Get-CimInstance Win32_Processor | Select-Object -First 1
$os=Get-CimInstance Win32_OperatingSystem
$assemblies=@(Get-ChildItem -LiteralPath (Join-Path $project 'Builds/WindowsServer/FirelineServer_Data/Managed') -File | Where-Object Name -like 'Fireline*.dll')
if($assemblies.Count -ne 3 -or $assemblies.Name -contains 'Fireline.Client.dll'){throw 'Dedicated role assembly boundary failed.'}
@{passed=$true;rawLogs=[IO.Path]::GetRelativePath($project,$raw);runs=$records;
    hardware=@{cpu=$processor.Name;os=$os.Caption;build=$os.BuildNumber;ramMiB=[math]::Round($os.TotalVisibleMemorySize/1024)};
    runtimeHashes=@($assemblies | ForEach-Object {@{name=$_.Name;sha256=(Get-FileHash $_.FullName).Hash}})
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $evidence 'process-summary.json') -Encoding utf8
python (Join-Path $PSScriptRoot 'analyze_m03_runway.py') --capture $raw --evidence $evidence
if($LASTEXITCODE -ne 0){throw 'Independent runway artifact verification failed.'}
'M03_RUNWAY_PROCESSES_PASSED'
