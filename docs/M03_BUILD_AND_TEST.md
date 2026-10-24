# M03 isolated debug-flight verification

Latest installed resource increment: [finite-fuel propulsion](#finite-fuel-propulsion). The following original numerical/view sections retain their historical scope and measured counts; current server fuel is finite by default in the explicit debug flight session.

Latest presentation increment: [engineering flight view/device proof](#engineering-flight-view-and-device-proof). Historical numerical and data-only sections retain their original scopes.

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

## Engineering flight view and device proof

This separate scene/client binds the temporary model to received poses, adds test controls/two external cameras/uGUI telemetry and verifies actual device events. It leaves the physical flight/contact solver unchanged. [Actual screenshots, presentation equations, controls, diagrams, raw plots and limitations](previews/m03-flight-view/COMPARISON.md).

Close the Editor; preserve existing edits and use fresh output destinations. Installed Unity 6000.6.7f1 / HDRP 17.6.0 / Input System 1.20.1 / Netcode 6.6.0 are retained. The current CLI is 1.0.0-beta.14. **Explicit Player subtarget is required for Client/test assemblies**; a retained Server subtarget excludes them. Device edge tests run in PlayMode using the installed Input System test fixture; pure simulation/presentation/scene checks run in EditMode.

```powershell
New-Item -ItemType Directory -Force Builds/M03FlightViewReplay | Out-Null
unity test "$PWD" --editor-version 6000.6.7f1 --mode EditMode --filter Fireline.Tests --output Builds/M03FlightViewReplay/editmode.xml --timeout 900 -- -buildTarget Win64 -standaloneBuildSubtarget Player -nographics -force-d3d11 *> Builds/M03FlightViewReplay/edit-tests.log
unity test "$PWD" --editor-version 6000.6.7f1 --mode PlayMode --filter Fireline.Tests --output Builds/M03FlightViewReplay/playmode.xml --timeout 900 -- -buildTarget Win64 -standaloneBuildSubtarget Player -nographics -force-d3d11 *> Builds/M03FlightViewReplay/play-tests.log
$env:FIRELINE_BUILD_EVIDENCE='Builds/M03FlightViewReplay/evidence/builds'
unity run "$PWD" --editor-version 6000.6.7f1 --timeout 1800 --log-file Builds/M03FlightViewReplay/client-build.log --no-tail -- -buildTarget Win64 -standaloneBuildSubtarget Player -force-d3d11 -executeMethod Fireline.Authoring.FoundationBuild.BuildFlightTestClient
unity run "$PWD" --editor-version 6000.6.7f1 --timeout 1200 --log-file Builds/M03FlightViewReplay/server-build.log --no-tail -- -buildTarget Win64 -standaloneBuildSubtarget Server -nographics -executeMethod Fireline.Authoring.FoundationBuild.BuildServer
./scripts/Test-M03FlightView.ps1 -Port 8360 -EvidenceRoot Builds/M03FlightViewReplay/evidence -CapturePath Builds/M03FlightViewReplay/after.png
$env:PYTHONPATH=(Join-Path $PWD Builds/AnalysisTools)
python scripts/render_m03_flight_view.py --evidence Builds/M03FlightViewReplay/evidence --output Builds/M03FlightViewReplay/plots
```

Check each exit before the next command; builds/tests share one checkout and must not run concurrently. Plotting uses matplotlib only for report exports; it is no runtime dependency. Keep raw Unity logs ignored because they may contain credentials. The official source [documents the Player/Server command-line switch](https://docs.unity.com/en-us/engine/6000.7/manual/unity-editor/command-line-arguments/editor).

The scene/materials are committed. Only when generating them from a state where they are absent, run `Fireline.Authoring.FlightTestAuthoring.CreateScene` through `unity run`. It refuses an edited existing test scene; only an exact, otherwise unmodified copy left by an interrupted first attempt may resume. It locates the aircraft by prefab asset identity, not its generated display name. Do not recreate the original project/preview/prefab.

The harness runs a new dedicated server, one **visible 1920×1080 Balanced D3D11** owner and a late headless observer from the new client binary. The owner enables `--fireline-flight-session debug --fireline-flight-view debug --fireline-flight-view-proof <output.json>` with capture/metrics paths and explicit direct IP. The proof queues isolated keyboard/gamepad state events through the normal reader/session/RPC path, tests logical focus loss, external-camera change, release/reclaim, and a real final disconnect. Observer checks occupied-seat denial and a forged pose, then sees a vacant seat. No native OS keystrokes or physical-controller hardware claim. Server/observer use the existing network-proof mode; owner returns failure **14** if any rendered/device/pose gate fails. A view option in the wrong scene returns **15**.

Observer admission waits for the rendered owner's confirmed occupied-seat CSV row, then one additional second; a fixed process-launch delay alone does not prove actual late join after a cold graphical startup. The independent checker requires the observer's actual first received tick to exceed the owner's. Every raw view row records the focus value actually consumed by that frame, along with exact receipt/sample clocks.

For manual use, start the dedicated binary with `--fireline-flight-session debug --fireline-port 7979`. Launch `Builds/WindowsFlightTest/FirelineFlightTest.exe -force-d3d11 --fireline-flight-session debug --fireline-flight-view debug --fireline-connect 127.0.0.1 --fireline-port 7979`. Omit proof options: it uses actual application focus/current devices and remains running. Enter claims the seat; use the on-screen/documented keyboard/pad mappings. Seat claim is server validated; any connected peer can request a vacant debug seat, without production qualifications/admission. Keep gear down on the runway; unsupported contact faults freeze/fail this prototype rather than resolving a crash. External cameras are not a verified cockpit.

[Accepted results and build identities](reports/2026-10-24-m03-flight-view.md) distinguish rendered-frame callbacks from physical/server ticks and preserve actual missing counters. The scene does not implement takeoff/landing acceptance, coast/cockpit/animation, aircraft calibration/fuel, water/suppression or 100-player/WAN qualification. M03 remains open.

Recheck the committed raw archives without Unity or the original ignored logs:

```powershell
python scripts/analyze_m03_flight_view.py --archive docs/reports/m03-flight-view-evidence --evidence Builds/M03ViewArchiveReplay --image docs/previews/m03-flight-view/after.png
```

Use a fresh destination; raw hash bindings, actual role result hashes, source snapshots, sampled focus, presentation math/float placement and FPS summaries are revalidated. The initial rejected attempt's callbacks remain a separate diagnostic archive/report and are not mixed into accepted view metrics.

## Finite-fuel propulsion

Current evidence: **102 EditMode + 3 PlayMode tests**, zero-error Windows dedicated/client builds (510/499 warnings), two 8,800-sample resource replays and the actual rendered/late-observer proof. [Actual results, provenance, measured limits and preservation](reports/2026-10-24-m03-fuel.md), [SI algorithms/BEFORE-AFTER SVGs/matched comparisons/graph guides](physics/aircraft-fuel/COMPARISON.md).

From the existing checkout with Editor closed, run sequentially and stop on any nonzero exit. Use fresh replay paths so published evidence remains immutable:

```powershell
New-Item -ItemType Directory -Force Builds/M03FuelReplay | Out-Null
unity self-update --check --format json
unity test "$PWD" --editor-version 6000.6.7f1 --mode EditMode --filter Fireline.Tests --output Builds/M03FuelReplay/editmode.xml --timeout 900 -- -buildTarget Win64 -standaloneBuildSubtarget Player -nographics -force-d3d11 *> Builds/M03FuelReplay/editmode-cli.log
$env:FIRELINE_BUILD_EVIDENCE='Builds/M03FuelReplay/builds'
unity run "$PWD" --editor-version 6000.6.7f1 --timeout 1200 --log-file Builds/M03FuelReplay/server-build.log --no-tail -- -buildTarget Win64 -nographics -executeMethod Fireline.Authoring.FoundationBuild.BuildServer
./scripts/Test-M03Fuel.ps1 -Port 8360 -EvidenceRoot Builds/M03FuelReplay/numerical
unity test "$PWD" --editor-version 6000.6.7f1 --mode PlayMode --filter Fireline.Tests --output Builds/M03FuelReplay/playmode.xml --timeout 900 -- -buildTarget Win64 -standaloneBuildSubtarget Player -nographics -force-d3d11 *> Builds/M03FuelReplay/playmode-cli.log
unity run "$PWD" --editor-version 6000.6.7f1 --timeout 1200 --log-file Builds/M03FuelReplay/client-build.log --no-tail -- -buildTarget Win64 -standaloneBuildSubtarget Player -force-d3d11 -executeMethod Fireline.Authoring.FoundationBuild.BuildFlightTestClient
./scripts/Test-M03FlightView.ps1 -Port 8361 -EvidenceRoot Builds/M03FuelReplay/runtime -CapturePath Builds/M03FuelReplay/after.png
$env:PYTHONPATH=(Join-Path $PWD Builds/AnalysisTools)
python scripts/render_m03_fuel.py --evidence Builds/M03FuelReplay --output Builds/M03FuelReplay/plots
```

The numerical harness uses the existing dedicated-only `--fireline-flight-proof` flag (failure exit 10); its JSON now includes the ten fuel cases plus the original nine flight cases. Two actual process exits, raw SHA bindings, all fuel/CoG/force data and exact physical replay must pass the independent checker. The old trajectory/polar archive must remain unchanged. The live/view harness still uses the existing flags and normal control/session path. A fresh `FlightAuthority` starts with 400 kg; no client refuel or arbitrary aircraft-config option exists. Empty fuel cannot supply thrust, while valid empty-tank controls may still be acknowledged. Inputs, seat/lease guards and reliable pose schema are unchanged.

Revalidate published archives without Unity or ignored raw logs:

```powershell
python scripts/analyze_m03_fuel.py
python scripts/analyze_m03_flight_view.py --archive docs/reports/m03-fuel-evidence/runtime --evidence Builds/M03FuelArchiveReplay --image docs/previews/m03-fuel/after.png
```

The first command recomputes the numerical summary beside the unchanged raw archives; the second writes to fresh scratch. No physical controller/native-focus, flight route, water, Linux/WAN/capacity or representative 60 FPS qualification is implied. D3D11 is per-process, all original quality assets remain unchanged, and the future Low/Medium/High/Ultra plan remains [planned](LARGE_MAP_AND_QUALITY_PLAN.md). Current M03 manual launch/control commands above still apply, with finite fuel now installed.
