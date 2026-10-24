<#
.SYNOPSIS
Run the actual dedicated debug-flight solver twice and retain only scoped, credential-free evidence.
.DESCRIPTION
Requires a fresh server build. This is an isolated numerical proof, not playable flight or aircraft network replication.
#>
param([ValidateRange(1024,65535)][int]$Port=8170)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$binary=Join-Path $project 'Builds/WindowsServer/FirelineServer.exe'
$raw=Join-Path $project ('Builds/M03Flight/'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
$evidence=Join-Path $project 'docs/reports/m03-flight-evidence'
New-Item -ItemType Directory -Force $raw,$evidence | Out-Null
if(Get-NetUDPEndpoint -LocalPort $Port -ErrorAction SilentlyContinue){throw 'Proof UDP port is occupied.'}
$records=@()
foreach($run in 1..2){
    $log=Join-Path $raw "run-$run.log"; $proof=Join-Path $raw "run-$run.json"
    $process=Start-Process $binary -WorkingDirectory $project -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode','-nographics','-logFile',('"'+$log+'"'),
        '--fireline-port',$Port,'--fireline-flight-proof',('"'+$proof+'"'))
    try{
        if(-not $process.WaitForExit(90000)){throw 'Dedicated flight proof timed out.'}
        if($process.ExitCode -ne 0){throw "Flight proof failed with actual exit $($process.ExitCode)."}
        $data=Get-Content $proof -Raw | ConvertFrom-Json
        if(-not $data.passed -or -not $data.headless -or $data.profile -ne 'DEBUG_FLIGHT_ONLY_V1' -or $data.cases.Count -ne 9){throw 'Missing flight proof state.'}
        $markers=@(Get-Content $log | Where-Object {$_ -match '^FIRELINE_'})
        if(-not ($markers -match '^FIRELINE_FLIGHT_PROOF passed=True headless=True')){throw 'No actual dedicated proof marker.'}
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
python (Join-Path $PSScriptRoot 'analyze_m03_flight.py') --capture $raw
if($LASTEXITCODE -ne 0){throw 'Independent flight artifact verification failed.'}
'M03_FLIGHT_PROCESSES_PASSED'
