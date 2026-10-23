<#
.SYNOPSIS
Run foundation unit tests and client/server builds from a closed project Editor.
.DESCRIPTION
Writes fresh test/build evidence, verifies headless asset isolation, and optionally runs the process smoke test.
Temporarily sets the evidence environment variable and restores it even on failure; never closes the user's Editor.
.PARAMETER Smoke
Run the local two-client connection/render test after successful builds.
.PARAMETER LinuxServer
Also cross-build the Linux server; this switch does not execute that player.
.PARAMETER EvidenceRoot
Repository-relative destination for reports; existing report names may be replaced by this run.
#>
param([switch]$Smoke, [switch]$LinuxServer, [string]$EvidenceRoot = 'docs/reports/m00-evidence')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
$previousEvidence = $env:FIRELINE_BUILD_EVIDENCE
$env:FIRELINE_BUILD_EVIDENCE = $EvidenceRoot
try {
    # Batch Unity cannot share an open project's asset database. Live-editor commands
    # are documented separately so this script never closes someone's Editor.
    $status = unity status --format json | ConvertFrom-Json
    if (@($status.data.instances | Where-Object { $_.projectPath -eq $projectRoot }).Count -gt 0) {
        throw 'Close this project in Unity before the batch build, or use the documented live-editor commands.'
    }
    New-Item -ItemType Directory -Force Builds/CI | Out-Null
    unity test $projectRoot --editor-version 6000.6.7f1 --mode EditMode --filter Fireline.Tests --output Builds/CI/editmode.xml --timeout 600
    if ($LASTEXITCODE -ne 0) { throw 'EditMode tests failed or did not complete.' }
    [xml]$testReport = Get-Content Builds/CI/editmode.xml -Raw
    if ([int]$testReport.'test-run'.total -lt 20 -or [int]$testReport.'test-run'.failed -ne 0) {
        throw 'Test report is empty, incomplete or failing.'
    }
    New-Item -ItemType Directory -Force $EvidenceRoot | Out-Null
    Copy-Item -LiteralPath Builds/CI/editmode.xml -Destination (Join-Path $EvidenceRoot 'editmode.xml') -Force
    # Build roles sequentially, then require a fresh successful report and a nonempty server asset inventory.
    foreach ($role in @('Server', 'Client')) {
        $started = [DateTime]::UtcNow
        unity run $projectRoot --editor-version 6000.6.7f1 --timeout 1800 --log-file "Builds/CI/$role-build.log" --no-tail -- -executeMethod "Fireline.Authoring.FoundationBuild.Build$role"
        if ($LASTEXITCODE -ne 0) { throw "$role build failed." }
        $reportPath = "$EvidenceRoot/$($role.ToLowerInvariant())-build.json"
        $report = Get-Content $reportPath -Raw | ConvertFrom-Json
        if ((Get-Item $reportPath).LastWriteTimeUtc -lt $started -or $report.result -ne 'Succeeded' -or $report.errors -ne 0) {
            throw "$role build report is stale or failing."
        }
        if ($role -eq 'Server' -and $report.containsVisualAssets) { throw 'Server includes visual assets.' }
        if ($role -eq 'Server' -and (Get-Item "$EvidenceRoot/server-packed-assets.txt").Length -eq 0) {
            throw 'The incremental server report omitted its asset inventory; obtain a clean build report before certifying asset isolation.'
        }
    }
    # Cross-compilation is a build check only; the separate WSL script verifies Linux execution.
    if ($LinuxServer) {
        $started = [DateTime]::UtcNow
        unity run $projectRoot --editor-version 6000.6.7f1 --timeout 1800 --log-file Builds/CI/LinuxServer-build.log --no-tail -- -buildTarget Linux64 -executeMethod Fireline.Authoring.FoundationBuild.BuildLinuxServer
        if ($LASTEXITCODE -ne 0) { throw 'Linux dedicated-server build failed.' }
        $linuxPath = "$EvidenceRoot/linux-server-build.json"
        $linux = Get-Content $linuxPath -Raw | ConvertFrom-Json
        if ((Get-Item $linuxPath).LastWriteTimeUtc -lt $started -or $linux.result -ne 'Succeeded' -or $linux.errors -ne 0 -or $linux.containsVisualAssets) {
            throw 'Linux server evidence is stale, failing or contains visual assets.'
        }
    }
    if ($Smoke) { & "$PSScriptRoot/Test-M00Smoke.ps1" -EvidenceRoot $EvidenceRoot -CapturePath 'docs/previews/m00-coastal/foundation_regression.png' }
}
finally { $env:FIRELINE_BUILD_EVIDENCE = $previousEvidence; Pop-Location }
