# M03 isolated debug-flight verification

M03 is **In Progress**. This increment verifies a pure numerical solver in an opt-in dedicated-player harness; it does not implement a playable aircraft. [Equations, debug values, before/after diagrams and limitations](physics/debug-flight/COMPARISON.md), [actual verification report](reports/2026-10-24-m03-debug-flight.md).

Run from the existing repository root, project Editor closed:

```powershell
unity self-update --check --format json
unity test "$PWD" --editor-version 6000.6.7f1 --mode EditMode --filter Fireline.Tests --output Builds/M03Start/editmode.xml --timeout 900 -- -buildTarget Win64 -nographics -force-d3d11
$env:FIRELINE_BUILD_EVIDENCE='docs/reports/m03-flight-evidence/builds'
unity run "$PWD" --editor-version 6000.6.7f1 --timeout 1200 --log-file Builds/M03Start/server-build.log --no-tail -- -buildTarget Win64 -nographics -executeMethod Fireline.Authoring.FoundationBuild.BuildServer
./scripts/Test-M03Flight.ps1 -Port 8170
python scripts/analyze_m03_flight.py
python scripts/render_m03_flight.py
```

The Unity CLI skill requires the current CLI; the project editor/package versions stay fixed. Matplotlib is needed only for rendering report drawings, not solver/runtime/independent artifact checks. This worker's existing local plotting dependencies may be used with `$env:PYTHONPATH=(Join-Path $PWD Builds/AnalysisTools)`; that ignored directory is not a production dependency.

The runner starts **two distinct dedicated processes**, checks their actual exits and headless markers, then losslessly archives JSON/CSV and independently checks analytic vacuum motion, mass/CoG/inertia, all sampled attitudes/airspeeds/pressures, drag-power sign and exact same-runtime replay. Raw logs stay ignored under `Builds/M03Flight/`; only scoped FIRELINE markers are published. The proof uses `--fireline-flight-proof output.json`, has explicit failure exit **10**, and cannot share live save/load, fire/replication proof or smoke/benchmark exit modes. Client startup rejects this server-only option.

When the flight flag is absent, normal fire/replication startup remains active. Flight state is not persisted or sent to clients. The subsequent user-authorized temporary aircraft import is separate from this headless path; no asset is required by the numerical solver. Do not present these numerical trajectories or SVG plots as actual aircraft gameplay screenshots.

## Temporary aircraft exterior import and static preview

The user authorized this prototype on 2026-10-24 and plans replacement before launch. The source is already downloaded locally from the official listing; no sign-in or fresh download is needed on this machine. Keep the existing Editor closed and preserve unrelated edits. The extractor rejects a different archive or mismatched existing output. Run `CreatePreview` only when its generated prefab/scene are absent; it deliberately refuses overwriting them.

```powershell
python scripts/verify_cl415_archive.py AssetSources/Aircraft/CL415/cl415.zip
python scripts/extract_cl415_prototype.py
unity run "$PWD" --editor-version 6000.6.7f1 --timeout 900 --log-file Builds/CL415ImportStart/import.log --no-tail -- -buildTarget Win64 -force-d3d11 -executeMethod Fireline.Authoring.AircraftPrototypeAuthoring.CreatePreview
python scripts/verify_cl415_archive.py AssetSources/Aircraft/CL415/cl415.zip --imported --output docs/reports/m03-aircraft-evidence/source-verification.json
unity test "$PWD" --editor-version 6000.6.7f1 --mode EditMode --filter Fireline.Tests --output Builds/CL415ImportStart/editmode.xml --timeout 900 -- -buildTarget Win64 -nographics -force-d3d11 *> Builds/CL415ImportStart/test-cli.log
$env:FIRELINE_BUILD_EVIDENCE='docs/reports/m03-aircraft-evidence/builds'
unity run "$PWD" --editor-version 6000.6.7f1 --timeout 1200 --log-file Builds/CL415ImportStart/preview-build.log --no-tail -- -buildTarget Win64 -force-d3d11 -executeMethod Fireline.Authoring.FoundationBuild.BuildAircraftPreviewClient
unity run "$PWD" --editor-version 6000.6.7f1 --timeout 1200 --log-file Builds/CL415ImportStart/server-build.log --no-tail -- -buildTarget Win64 -nographics -executeMethod Fireline.Authoring.FoundationBuild.BuildServer
./scripts/Test-M00Smoke.ps1 -Port 8190 -ClientCount 1 -ClientSeconds 30 -Width 1920 -Height 1080 -ForceD3D11 -ClientRelativeExe Builds/WindowsAircraftPreview/FirelineAircraftPreview.exe -EvidenceRoot docs/reports/m03-aircraft-evidence -CapturePath docs/previews/m03-cl415-prototype/aircraft_after.png
$env:PYTHONPATH=(Join-Path $PWD Builds/AnalysisTools)
python scripts/render_m03_aircraft.py
```

Use fresh evidence directories for later independent measurements rather than overwriting published captures. `-force-d3d11` is a per-process diagnostic override following the earlier D3D12 device-removal problem; project API settings are preserved. The new inspection build keeps M00/M02 clients and the coastal benchmark binaries/scenes separate. Its `Legal/` folder contains the contributor notice, source audit and project credits. Runtime proof is an actual render/connection smoke, **not flight authority/controls/landing verification**. [Capture, geometry, diagrams, numerical scope and performance guide](previews/m03-cl415-prototype/COMPARISON.md).

## Runway-contact prerequisite

This pure numerical increment does not change scenes/assets or install live aircraft. Current 71-test and dedicated-build evidence: [full comparison and limitations](physics/runway-contact/COMPARISON.md). The imported aircraft remains static; no fresh client/Linux or flight-network evidence is implied.

Run from the existing checkout with Editor closed and fresh evidence locations for later independent captures:

```powershell
unity self-update --check --format json
unity test "$PWD" --editor-version 6000.6.7f1 --mode EditMode --filter Fireline.Tests --output Builds/M03RunwayStart/editmode.xml --timeout 900 -- -buildTarget Win64 -nographics -force-d3d11 *> Builds/M03RunwayStart/tests.log
$env:FIRELINE_BUILD_EVIDENCE='docs/reports/m03-runway-evidence/builds'
unity run "$PWD" --editor-version 6000.6.7f1 --timeout 1200 --log-file Builds/M03RunwayStart/server-build.log --no-tail -- -buildTarget Win64 -nographics -executeMethod Fireline.Authoring.FoundationBuild.BuildServer
./scripts/Test-M03Runway.ps1 -Port 8210
python scripts/analyze_m03_runway.py
$env:PYTHONPATH=(Join-Path $PWD Builds/AnalysisTools)
python scripts/render_m03_runway.py
```

The runner launches **two separate dedicated processes** with `--fireline-runway-proof output.json`, requires actual exit 0/headless markers and independently checks every trajectory/force/CSV channel before accepting exact physical replay. Failure exit is **11**. The option rejects blank/duplicate paths and concurrent flight/fire/replication proof, save/load, smoke or benchmark exit modes; client bootstrap rejects server proof flags. No contact code runs during ordinary fire startup unless a future server adapter explicitly calls it. Raw logs stay ignored in `Builds/M03Runway/`; raw JSON/CSV are retained losslessly gzip-compressed, SHA-256 bound to process records, under the new evidence directory. The archive verifier needs no Unity or plotting dependency; the diagram generator needs Matplotlib only.

Legacy proof modes were also run in the new binary with separate output/log paths under `Builds/M03RunwayStart/free-flight-regression/` and `fire-regression/`, ports 8220/8230, using `--fireline-flight-proof` / `--fireline-sim-proof` respectively. Both actual exits were 0. All nine free-flight physical cases match the original gzip artifact exactly; all nine M01 fire final states/digests/physical samples match `m01-evidence/simulation.json`, excluding elapsed `stepMs`. Fresh raw regression artifacts, scoped markers, reference paths and hashes are in [runway evidence](reports/m03-runway-evidence/). These are isolated numerical regressions, not new M02 network or client tests.

For later captures, preserve the published evidence by selecting fresh outputs. This path override was also exercised successfully in two additional actual headless processes; those utility-check runs remain ignored and are not mixed into the published timing statistics:

```powershell
./scripts/Test-M03Runway.ps1 -Port 8240 -EvidenceRoot Builds/M03RunwayReplay/evidence
python scripts/analyze_m03_runway.py --evidence Builds/M03RunwayReplay/evidence
python scripts/render_m03_runway.py --evidence Builds/M03RunwayReplay/evidence --output Builds/M03RunwayReplay/plots
```
