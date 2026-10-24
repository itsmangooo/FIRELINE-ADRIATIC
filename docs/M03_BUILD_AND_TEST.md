# M03 isolated debug-flight verification

The latest increment also adds an explicit [live debug-aircraft / two-client proof](#live-debug-aircraft-and-two-client-network-proof). Older sections below describe their isolated proof scopes.

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

## Live debug aircraft and two-client network proof

This opt-in prototype adds one estimated server aircraft and read-only client replicas. The static mesh is still separate. No normal scene/assets/preset change or new package is required. [Actual behavior, SI clock equations, rendered SVGs, data, graph guide and limitations](physics/live-flight-host/COMPARISON.md).

From the existing repository root, Editor closed, use the latest CLI (verified `1.0.0-beta.14` for this capture) and retained Unity `6000.6.7f1` / HDRP `17.6.0` / Netcode `6.6.0`:

```powershell
New-Item -ItemType Directory -Force Builds/M03Authority | Out-Null
unity self-update --check --format json
unity test "$PWD" --editor-version 6000.6.7f1 --mode EditMode --filter Fireline.Tests --output Builds/M03Authority/editmode.xml --timeout 900 -- -buildTarget Win64 -nographics -force-d3d11 *> Builds/M03Authority/tests.log
$env:FIRELINE_BUILD_EVIDENCE='Builds/M03AuthorityReplay/evidence/builds'
unity run "$PWD" --editor-version 6000.6.7f1 --timeout 1200 --log-file Builds/M03Authority/server-build.log --no-tail -- -buildTarget Win64 -nographics -executeMethod Fireline.Authoring.FoundationBuild.BuildServer
unity run "$PWD" --editor-version 6000.6.7f1 --timeout 1800 --log-file Builds/M03Authority/client-build.log --no-tail -- -buildTarget Win64 -force-d3d11 -executeMethod Fireline.Authoring.FoundationBuild.BuildAircraftPreviewClient
./scripts/Test-M03FlightNetwork.ps1 -Port 8330 -EvidenceRoot Builds/M03AuthorityReplay/evidence
$env:PYTHONPATH=(Join-Path $PWD Builds/AnalysisTools)
python scripts/render_m03_flight_network.py --evidence Builds/M03AuthorityReplay/evidence --output Builds/M03AuthorityReplay/plots
```

Check each exit before proceeding; do not run builds concurrently against this checkout. `-force-d3d11` is a process override for the existing graphics-driver limitation, not a project API change. Raw Unity logs can contain credentials; do not publish them. Published [results](reports/2026-10-24-m03-live-flight-host.md) use `docs/reports/m03-flight-network-evidence/`; use fresh paths for later replays. The harness refuses to overwrite an existing process summary.

The harness starts one real dedicated player and two real client players with `-batchmode -nographics`: these are actual Netcode client worlds/UDP connections, **not rendered-client performance runs**. It waits for server readiness, starts owner, then observer three seconds later. `--fireline-flight-session debug` explicitly enables the mode on each role. `--fireline-flight-network-proof output.json` owns the proof lifecycle; clients additionally require `--fireline-connect 127.0.0.1 --fireline-flight-probe-role owner|observer`. That role flag only enables a local test script; it grants no server permission. Failure exits are 12 (server), 13 (client). Other proof/smoke/benchmark/persistence modes are mutually excluded; the accelerated numerical flags remain unchanged.

The owner sends valid controls, deliberate invalid packets, a >0.5 s silence and a real disconnect. The late observer tests denied claim/input and a forged pose, then observes the vacant seat. The server requires real input/ACK/rejection/disconnect/expiry outcomes and fails after 50 wall seconds if they never occur. Independent Python verification matches every received pose to its actual server publication, checks all recorded clock rows, and archives only scoped JSON/JSONL/CSV plus FIRELINE markers. No graphical binding, airborne flight loop, WAN/loss result, production authentication, flight persistence, bandwidth or capacity is implied.

For non-proof use, omit proof/role/output options and explicitly connect a debug-session client. It subscribes read-only; no keyboard/pad control path is installed yet. Normal fire/weather continues on the server alongside the aircraft. With no debug-session option, existing normal startup is retained.

The committed evidence can also be checked without Unity or the original ignored capture folder:

```powershell
python scripts/analyze_m03_flight_network.py --archive docs/reports/m03-flight-network-evidence --evidence Builds/M03AuthorityArchiveReplay
```

This verifies gzip raw hashes, process-result hash bindings, every client/server snapshot and host clock row, writing fresh results to the scratch destination. To reproduce the no-peer failure gate, launch the same dedicated binary with `-batchmode -nographics --fireline-flight-session debug --fireline-flight-network-proof <fresh-output.json>` and an unused `--fireline-port`, start no clients, wait at least 50 seconds, and require exit 12 with `passed=false` / `reason=network-proof-timeout`. Its trace is not a performance reference.
