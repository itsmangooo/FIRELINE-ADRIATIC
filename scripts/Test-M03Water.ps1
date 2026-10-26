<#
.SYNOPSIS
Verify the isolated M03 water release prerequisite in two actual dedicated-player processes.
.DESCRIPTION
Requires a current server build. Preserves complete raw flight/fuel/authority/water outputs and independently checks each boundary.
This does not launch a rendered client, install a live release request, scoop water or qualify production performance.
#>
param([ValidateRange(1024,65535)][int]$Port=8392,
    [string]$EvidenceRoot='docs/reports/m03-water-evidence')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if(Test-Path (Join-Path $project ($EvidenceRoot+'/verification.json'))){throw 'Choose a fresh evidence directory.'}
& (Join-Path $PSScriptRoot 'Test-M03Fuel.ps1') -Port $Port -EvidenceRoot ($EvidenceRoot+'/numerical')
python (Join-Path $PSScriptRoot 'analyze_m03_water.py') --numerical ($EvidenceRoot+'/numerical') --output ($EvidenceRoot+'/verification.json')
if($LASTEXITCODE -ne 0){throw 'Water conservation/trajectory/coverage proof failed.'}
python (Join-Path $PSScriptRoot 'analyze_m03_route.py') --numerical ($EvidenceRoot+'/numerical') --output ($EvidenceRoot+'/route-verification.json')
if($LASTEXITCODE -ne 0){throw 'Original authority route regression failed.'}
'M03_WATER_NUMERICAL_PASSED'
