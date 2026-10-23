<#
.SYNOPSIS
Verify the current Windows headless simulation in two independent dedicated-server processes.
.DESCRIPTION
Requires the Editor closed, a role-isolated existing build, and free loopback port 7992.
Checks fresh nine-case outputs, 600-step coverage, zero exits and matching digests; retains first raw run plus both hashes.
Only this invocation's process is stopped on timeout/failure. Timings are fixture evidence, not full-host capacity.
.PARAMETER EvidenceRoot
Repository-relative report destination; raw logs and the second full result remain under ignored Builds.
#>
param([string]$EvidenceRoot = 'docs/reports/m01-evidence')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$editorStatus = unity status --format json | ConvertFrom-Json
if (@($editorStatus.data.instances | Where-Object { $_.projectPath -eq $projectRoot }).Count -gt 0) {
    throw 'Close this project Editor before recording headless CPU proof timings.'
}
$binary = Join-Path $projectRoot 'Builds/WindowsServer/FirelineServer.exe'
$managed = Join-Path (Split-Path $binary) 'FirelineServer_Data/Managed'
if (-not (Test-Path $binary)) { throw 'Build the current dedicated server first.' }
if (Test-Path (Join-Path $managed 'Fireline.Client.dll')) { throw 'Client assembly leaked into server.' }
if (Get-NetUDPEndpoint -LocalPort 7992 -ErrorAction SilentlyContinue) { throw 'Proof port 7992 is occupied.' }
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$raw = Join-Path $projectRoot "Builds/M01Proof/$stamp"
$evidence = Join-Path $projectRoot $EvidenceRoot
New-Item -ItemType Directory -Force $raw, $evidence | Out-Null
$runs = @()
# Start a fresh process for each replay; matching final digests test same-runtime repeatability.
foreach ($iteration in 1..2) {
    $resultPath = Join-Path $raw "proof-$iteration.json"
    $log = Join-Path $raw "proof-$iteration.log"
    $arguments = @('-batchmode', '-nographics', '-logFile', ('"' + $log + '"'),
        '--fireline-bind', '127.0.0.1', '--fireline-port', '7992', '--fireline-sim-proof', ('"' + $resultPath + '"'))
    $started = [DateTime]::UtcNow
    $process = Start-Process -FilePath $binary -WorkingDirectory $projectRoot -ArgumentList $arguments -WindowStyle Hidden -PassThru
    try {
        while (-not $process.HasExited) {
            if ([DateTime]::UtcNow -gt $started.AddSeconds(300)) { throw 'Headless simulation proof timed out.' }
            Start-Sleep -Milliseconds 250
        }
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) { throw "Simulation proof failed: exit $($process.ExitCode). See $log" }
        if (-not (Test-Path $resultPath) -or (Get-Item $resultPath).LastWriteTimeUtc -lt $started) { throw 'Proof data missing/stale.' }
        $data = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
        $markers = @(Get-Content -LiteralPath $log | Where-Object { $_ -match '^FIRELINE_' })
        if (-not $data.passed -or $data.cases.Count -ne 9 -or -not ($markers -match 'PROOF passed=True headless=True')) { throw 'Incomplete/invalid headless proof.' }
        # Require the complete scenario duration before comparing state digests; timing samples may differ between runs.
        $checksums = @{}
        foreach ($case in $data.cases) {
            if ($case.samples.Count -ne 600 -or $case.finalState.Tick -ne 600) { throw 'Missing simulation steps.' }
            $checksums[$case.scenario] = $case.checksum
        }
        $markers | Set-Content (Join-Path $evidence "proof-$iteration-markers.txt") -Encoding utf8
        if ($iteration -eq 1) {
            Copy-Item -LiteralPath $resultPath -Destination (Join-Path $evidence 'simulation.json') -Force
            Copy-Item -LiteralPath ([IO.Path]::ChangeExtension($resultPath, '.csv')) -Destination (Join-Path $evidence 'simulation.csv') -Force
        }
        else {
            foreach ($name in $checksums.Keys) {
                if ($runs[0].checksums[$name] -ne $checksums[$name]) { throw "Cross-process mismatch: $name" }
            }
        }
        $runs += @{ iteration = $iteration; pid = $process.Id; exitCode = $process.ExitCode; checksums = $checksums;
            rawResultSha256 = (Get-FileHash $resultPath -Algorithm SHA256).Hash }
        "SIMULATION_PROOF_COMPLETED iteration=$iteration cases=9 steps=600 headless=True"
    }
    finally { if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force } }
}
# Record DLL hashes as well as the launcher hash so this evidence identifies the actual simulation code.
[ordered]@{ utc = [DateTime]::UtcNow.ToString('o'); passed = $true; runs = $runs;
    binarySha256 = (Get-FileHash $binary -Algorithm SHA256).Hash;
    runtimeHashes = @(Get-ChildItem $managed -Filter 'Fireline*.dll' | ForEach-Object {
        @{name=$_.Name;sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash} });
    rawLogs = [IO.Path]::GetRelativePath($projectRoot, $raw);
    scope = 'Two independent Windows dedicated-server processes, same seeded synthetic simulation; no client state protocol or capacity claim.'
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $evidence 'headless-proof-summary.json') -Encoding utf8
