<#
.SYNOPSIS
Record sequential 1080p release-player coastal proxy workloads with the project Editor closed.
.DESCRIPTION
Runs requested existing presets/scenarios, allows 30 rendered seconds of warm-up, and validates fresh samples/captures.
Keeps failed production qualification distinct from a completed recording; no optimization or capacity is inferred.
.PARAMETER Preset
Existing Unity quality tiers to measure; presets are never edited by this script.
.PARAMETER Scenario
Clear or static smoky overdraw workload. Smoky is rejected for the volumetrics-disabled Performant preset.
.PARAMETER Seconds
Recording duration in seconds after warm-up, between 300 and 900.
#>
param(
    [ValidateSet('Balanced', 'Performant', 'High Fidelity')][string[]]$Preset = @('Balanced'),
    [ValidateSet('clear', 'smoky')][string[]]$Scenario = @('clear', 'smoky'),
    [ValidateRange(300, 900)][int]$Seconds = 300
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
# This existing tier has volumetrics disabled. A clear-looking "smoky" result
# would be invalid evidence; leave the user's presets intact and fail explicitly.
if ($Scenario -contains 'smoky' -and $Preset -contains 'Performant') {
    throw 'The existing Performant preset disables volumetrics. Run its clear case separately; use Balanced or High Fidelity for the static smoky fixture.'
}
$binary = Join-Path $projectRoot 'Builds/WindowsBenchmark/FirelineBenchmark.exe'
if (-not (Test-Path -LiteralPath $binary)) { throw 'Build FoundationBuild.BuildBenchmarkClient() first.' }
$editor = unity status --format json | ConvertFrom-Json
if (@($editor.data.instances | Where-Object { $_.projectPath -eq $projectRoot }).Count -gt 0) {
    throw 'Close the project Editor before collecting isolated baseline runs.'
}
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$logs = Join-Path $projectRoot "Builds/CoastalBenchmark/$stamp"
$evidence = Join-Path $projectRoot "docs/reports/m00-coastal-evidence/benchmarks/$stamp"
$captures = Join-Path $projectRoot 'docs/previews/m00-coastal'
New-Item -ItemType Directory -Force $logs, $evidence, $captures | Out-Null
$records = @()
# Each preset/scenario runs alone so concurrent game clients do not contaminate these render measurements.
foreach ($quality in $Preset) {
    foreach ($case in $Scenario) {
        $name = $quality.Replace(' ', '-').ToLowerInvariant() + '-' + $case
        $metrics = Join-Path $evidence "$name.json"
        $capture = Join-Path $captures "$name.png"
        $log = Join-Path $logs "$name.log"
        $started = [DateTime]::UtcNow
        $arguments = @('-logFile', ('"' + $log + '"'), '-screen-width', '1920', '-screen-height', '1080',
            '-screen-fullscreen', '0', '--fireline-benchmark-seconds', $Seconds,
            '--fireline-benchmark-output', ('"' + $metrics + '"'), '--fireline-benchmark-scenario', $case,
            '--fireline-quality', ('"' + $quality + '"'), '--fireline-capture', ('"' + $capture + '"'))
        $player = Start-Process -FilePath $binary -WorkingDirectory $projectRoot -ArgumentList $arguments -WindowStyle Normal -PassThru
        try {
            $deadline = [DateTime]::UtcNow.AddSeconds($Seconds + 150)
            while (-not $player.HasExited) {
                if ([DateTime]::UtcNow -gt $deadline) { throw "$name timed out without completing actual rendered sampling." }
                Start-Sleep -Seconds 1
            }
            $player.WaitForExit()
            if ($player.ExitCode -ne 0) { throw "$name player failed with exit $($player.ExitCode)." }
            $data = Get-Content -LiteralPath $metrics -Raw | ConvertFrom-Json
            if ($data.width -ne 1920 -or $data.height -ne 1080 -or $data.seconds -lt $Seconds - 0.1 -or $data.samples -lt 100) {
                throw "$name has insufficient duration, invalid resolution or missing render samples."
            }
            if ($data.quality -ne $quality -or $data.scenario -ne $case) { throw "$name configuration mismatch." }
            if (-not (Test-Path $capture) -or (Get-Item $capture).LastWriteTimeUtc -lt $started) { throw "$name capture missing/stale." }
            $markers = @(Get-Content -LiteralPath $log | Where-Object { $_ -match '^FIRELINE_' })
            if (-not ($markers -match 'FIRELINE_BENCHMARK_COMPLETE')) { throw "$name completion marker missing." }
            # Preserve only completed, validated runs; missing counters stay unavailable in the player CSV.
            $records += [ordered]@{ name = $name; exitCode = $player.ExitCode; processId = $player.Id;
                metrics = [IO.Path]::GetRelativePath($projectRoot, $metrics); capture = [IO.Path]::GetRelativePath($projectRoot, $capture);
                markers = $markers; averageFps = $data.averageFps; p95FrameMs = $data.p95FrameMs }
            $records | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $evidence 'runs.json') -Encoding utf8
            "BENCHMARK_COMPLETED $name samples=$($data.samples) seconds=$($data.seconds) average=$($data.averageFps) p95=$($data.p95FrameMs)"
        }
        finally { if (-not $player.HasExited) { Stop-Process -Id $player.Id -Force } }
    }
}
[ordered]@{ binary = (Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash;
    assemblies = @(Get-ChildItem (Join-Path (Split-Path $binary) 'FirelineBenchmark_Data/Managed') -Filter 'Fireline*.dll' |
        ForEach-Object { @{ name = $_.Name; sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } });
    runs = $records; limitations = 'Initial populated coastal proxy workload, not S1-S6 production acceptance or multiplayer capacity. No optimizations applied.'
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $evidence 'run-summary.json') -Encoding utf8
