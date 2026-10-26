# Development status — handoff

## Latest increment — 2026-10-26 real-time rendered flight

**M03 remains In Progress.** Two actual local dedicated/rendered-owner/late-observer runway-hop executions pass takeoff/landing/settling, source/replica/display agreement and explicit release/vacancy/disconnect cleanup. **119 EditMode + 3 PlayMode tests**, zero-error Windows server/client builds (510/499 warnings), exact unchanged numerical replays. Accepted run: **16,669 server substeps, 1,213 exact received poses, 3,950 reconstructed display frames, 3,957 actual instrument texts**. [Actual results, rejected first attempt, raw evidence and limits](reports/2026-10-26-m03-realtime-flight.md), [real screenshots, equations, SVGs and graph guide](previews/m03-realtime-flight/COMPARISON.md).

Automated calm-air straight hop only: no production autopilot, manual hardware/focus, crosswind/payload/impairment, general collision/calibration/animation/cockpit, refueling/water or representative FPS/capacity acceptance. M03 remains open; M04/M05A Pending. FL-11 and prior hitches remain for later optimization. User configuration and assets preserved. Earlier entries below retain their historical results and next steps; this latest entry supersedes their pending automated calm-air route task.

## Latest increment — 2026-10-26 M03 authority runway-route proof

**M03 remains In Progress.** New explicit headless numerical proof completes a calm-air takeoff/approach/landing/stop through real server seat/input/lease/fuel/contact authority: **13,943 accepted 5 ms substeps, 3,486 guarded commands, 69.715 physical seconds**, exact replay in two actual processes, five airborne and five settled seconds. **113 EditMode + 3 PlayMode tests**, fresh Windows server **0 errors / 510 warnings**. **55 C# files / 434 documented members**. All force/contact/fuel kernels and normal gameplay remain unchanged. [Actual equations, four rendered SVGs, raw data, independent tire/fuel/gate checks and limits](reports/2026-10-26-m03-authority-route.md).

This is accelerated numerical QA, not a normal gameplay autopilot or real-time/rendered/network/manual flight qualification. Retained older failed routes remain historical failures. Next: actual network-controlled owner/observer takeoff/landing, then impaired/crosswind cases and collision/calibration/cockpit/animation/water gates. M03 stays open, M04/M05A Pending. No graphical/preset/asset/package change or new FPS claim; FL-10/HOST-01/NET-01 optimization remains deferred. Existing user configuration preserved.

## Latest increment — 2026-10-25 M03 sampled-wind instruments

**M03 remains In Progress.** Protocol v2 now carries actual server-sampled horizontal wind; the read-only engineering panel shows air/ground/vertical speed, CoG datum altitude, attitude/heading, fuel/water and debug mass/CoG. Corrected the old 3D ground-speed label. Matching rebuilt players required; no force/integrator/asset/package change. **110 EditMode + 3 PlayMode tests**, Windows server/client builds **0 errors** (510/499 warnings), two actual 8,800-sample numerical replays with unchanged flight/fuel trajectories and three actual live process exits **0**. **404** received poses, **1,715** model frames and **1,721** actual visible instrument strings independently checked; **406** wind publications reconstruct exactly. [Full evidence and limitations](reports/2026-10-25-m03-flight-instruments.md), [actual screenshots, equations and rendered diagrams](previews/m03-flight-instruments/COMPARISON.md).

Two offline control strategies **failed** the finite-runway landing/stop gate (exit 2); both raw CSVs/results are retained. No actual Unity airborne qualification. Next: investigate controlled takeoff/trim/landing and finite-runway/mesh limits, then qualify live flight before water work. Cockpit/animation, calibration/refueling, general collision, prediction/correction and impaired networking remain open. M04/M05A remain Pending; future map/quality plan unchanged. FL-10 records 184.623/172.498 ms client gaps and six host catch-up overruns for later profiling. **58.738 average / 17.840 1% low FPS** is diagnostic only; no optimization or representative 60 FPS claim. User sky/configs preserved. Historical entries below retain their original scopes.

## Latest increment — 2026-10-24 M03 finite debug fuel

**M03 remains In Progress.** Installed server-owned accepted-step fuel debit, starvation-limited thrust, mass/CoG/inertia refresh and fixed-body-position/velocity-preserving rebase. Default 400 kg fuel / estimated 0.25 kg/s full-throttle rate; no idle/startup/refuel or water transfer. **102 EditMode + 3 PlayMode tests pass**, zero-error Windows server/client builds (510/499 warnings), two actual dedicated numerical replays with **8,800 resource samples each** match exactly and preserve all nine original flight trajectories. [Actual equations, BEFORE/AFTER SVGs, matched results and graph guides](physics/aircraft-fuel/COMPARISON.md), [all evidence/hashes/limits](reports/2026-10-24-m03-fuel.md), [reproduction](M03_BUILD_AND_TEST.md#finite-fuel-propulsion).

Rebuilt rendered owner + late observer + dedicated all exit 0: **403 received fuel-bearing poses match server publications**, **1,724 display frames** independently reconstruct with actual mass-derived visual offsets; 442 inputs acknowledged, focus/lease/seat/camera/disconnect checks pass. Live consumption **0.647945 kg**, final **399.352055 kg** fuel / zero water; no fault. Actual chase/inspection captures and four inspected SVG drawings are retained. Source comments: **51 C# files / 416 members**. Five configuration hashes/user sky/model/scenes/presets/packages are preserved.

**FL-09** records #499 **182.220802 ms** at **8.433306–8.615527 s**, #242 **160.174206 ms** at **4.017848–4.178022 s**. Average/1% low **59.303/25.811 FPS**, p95/p99 **17.182/17.808 ms** in the short 1080p Balanced synthetic runway diagnostic. Host startup separately discards 14.1929 ms wall time; no rendered-loss/full-host inference. Optimization remains deferred and no representative 60 FPS/Ultra claim is made.

Future requirements now recorded: supplied ~30 × 4 km map audit/bounded HDRP texture/lighting sector in M04, Low/Medium/High/Ultra/Custom view-distance/animation/effect settings in M05A, full streaming in M08 and measured Ultra 1080p/60 FPS on i5-9400F / 16 GB / GTX 1660-class hardware in M09. [Playability gates and delivery plan](LARGE_MAP_AND_QUALITY_PLAN.md). No map/preset implementation is claimed. Next M03: configuration/calibration, real gear/CoG/cockpit/animation and general collision, actual takeoff/route/landing, prediction/impaired networking, scoop/drop/trajectory/conservation/shared suppression. Earlier dry/frozen-fuel statements below retain their historical scope; M04/M05A remain Pending.

## Latest increment — 2026-10-24 M03 engineering flight view

**M03 remains In Progress.** Added a separate synthetic-runway client with bound temporary model, explicit debug CoG/visual offset, 100 ms receipt-time interpolation (no extrapolation), keyboard/gamepad controls, two external cameras and engineering HUD. **94 EditMode + 3 PlayMode tests pass**, final Windows client/dedicated builds **0 errors** (499/505 warnings). Actual rendered owner and confirmed later observer pass all process exits and independent checks: **401 source poses** match server publications; **1,723 displayed frames** independently reconstruct to ≤4.03e-15 m / 1.11e-16 quaternion component difference; actual float-anchor error 0 m. 439 input acknowledgements, logical focus/lease fallback, camera change, seat release/reclaim and final disconnect release verified. [Full results, initial failed trace attempt and hashes](reports/2026-10-24-m03-flight-view.md), [actual screenshots, equations/diagrams, controls and graph guide](previews/m03-flight-view/COMPARISON.md), [commands](M03_BUILD_AND_TEST.md#engineering-flight-view-and-device-proof).

**FL-08** retains accepted callback #499 at **178.722903 ms**, recording **8.450635–8.629358 s**, plus #243 at 165.068999 ms and the initial trace-rejected attempt's **418.062896 ms at #1**. Accepted 1080p Balanced diagnostic: average/1% low **59.263/25.650 FPS**, p95/p99 **17.394/17.924 ms**. No representative 60 FPS, optimization, dropped-presentation or capacity claim. [Exact deferred locations](performance/FRAME_LOSS_REGISTER.md#m03-engineering-view-callbacks--2026-10-24). Source comments cover **48 C# files / 405 members**; existing user sky/configuration and static import are preserved.

Next M03 dependencies: real gear/CoG/pivot/cockpit/animation and general collision, documented airframe/fuel configuration, airborne takeoff/route/landing checks, prediction/correction/impaired networking, then scoop/drop/trajectory/conservation/shared suppression. The current proof taxis 3.601 m northward on a synthetic rectangle with fixed dry mass. No fresh Linux, physical-controller hardware, native OS focus event, coastal populated gameplay or water qualification. M04/M05A remain Pending; earlier M03 boundaries below are historical.

## Latest increment — 2026-10-24 M03 live debug-authority foundation

**M03 remains In Progress.** Implemented one opt-in server aircraft with 5 ms bounded physical substeps, actual authoritative wind/fire-tick sampling, transport-derived seat ownership, epoch/sequence/tick/range/rate/lease input guards, disconnect/fault release and bounded complete-pose RPC/ACK replicas. **89/89 tests**, final Windows dedicated/client builds (**0 errors**, 505/501 warnings), actual dedicated + owner + late-observer exits **0**. All **154 received snapshots** match recorded server publications; 134 owner inputs accepted, deliberate invalid/nonowner/forged-pose cases rejected, two lease expirations and one pilot disconnect release observed. A no-peer actual server correctly exits **12** after its 50 s failure deadline. [Full evidence, hashes and limits](reports/2026-10-24-m03-live-flight-host.md), [actual math/BEFORE-AFTER SVGs/plots and graph guide](physics/live-flight-host/COMPARISON.md), [commands](M03_BUILD_AND_TEST.md#live-debug-aircraft-and-two-client-network-proof).

**HOST-01** records callback #2 at **51.6976 ms** and its **13.2569 ms** excess-time discard; adapter execution max **6.3483 ms at #1**. Headless observations are not rendered frame loss, full host CPU or capacity. [Exact locations and deferred attribution](performance/FRAME_LOSS_REGISTER.md#m03-live-host-catch-up-observation--2026-10-24). FL-06/FL-07 and NET-01 remain unresolved; optimization remains deferred. Human source-comment audit passes **40 C# files / 343 members**; user sky edit and five configuration hashes preserved.

Next: correct visual/CoG binding, test controls/cameras/telemetry and interpolation/correction, impaired-network flight tests, broader collision/calibrated fuel/mass behavior and takeoff/landing route, then water scoop/drop/dispersion/conservation/suppression. The imported mesh is still static. No fresh Linux, graphical flight, representative FPS, WAN or production permission/persistence claim. M04/M05A remain Pending. Earlier M03 boundary statements below are historical.

## Latest increment — 2026-10-24 M03 runway-contact prerequisite

**M03 remains In Progress.** Added the isolated pure debug contact adapter: three spring/damper tire points, CoG/rotating-point velocity, bounded combined friction, main-wheel braking, nose steering, finite runway bounds and fail-before-return current/candidate checks. **71/71 tests**, current Windows dedicated build (0 errors / 505 warnings), two actual headless processes with **19,800 samples each and exact physical replay**, plus exact legacy free-flight and fire physical regression pass. [Actual algorithms, SI equations, BEFORE/AFTER SVGs, numerical comparisons, timing and graph guides](physics/runway-contact/COMPARISON.md), [commands](M03_BUILD_AND_TEST.md#runway-contact-prerequisite).

This is a contact dependency, not installed gameplay. No fresh client/Linux build or aircraft network/real-time host test is claimed; static model imagery remains the preceding increment. Next: live server-owned aircraft state with bounded fixed substeps, pilot/seat/input authorization and replication, then bind model/controls/cameras/telemetry. General airframe/terrain collision, aircraft calibration, fuel burn and scoop/drop conservation remain open. No new graphics/performance qualification or optimization; FL-06/FL-07 and NET-01 remain unchanged. M04/M05A stay pending. Historical entries below retain their original evidence scope.

## Latest increment — 2026-10-24 M03 aircraft prototype

M03 remains **In Progress**. The user authorized temporary use of the downloaded official CL-415 model and plans replacement before launch. **60/60 Unity tests**, fresh Windows preview/server builds (0 errors; 501/505 warnings), inspected 1080p capture and real client/server smoke pass; server packages no aircraft content. A 206.632 ms callback gap at sample #2 is recorded as FL-07 with optimization deferred. The separate static inspection prefab now preserves 144 named meshes / 35,799 triangles with eleven HDRP materials and nine source textures. Original source bytes/notices remain intact; a verified object-to-group derivative preserves parts that Unity's direct OBJ import merged. [Actual build, capture, performance and limitations](previews/m03-cl415-prototype/COMPARISON.md). This supersedes the import hold in historical entries below for the prototype only; it does not certify production rights or aerodynamic calibration.

The numerical solver remains independent. Next: live dedicated aircraft state, pilot/input permissions, replication, physical runway contact, controls/cameras/telemetry, calibrated aircraft configuration and later scoop/drop integration. M05A remains Pending. Existing NET-01 and performance hitches remain recorded for later investigation.

## Current progress — 2026-10-24 M03

**M00/M01/M02 Verified; M03 In Progress.** Implemented the independent `DEBUG_FLIGHT_ONLY_V1` numerical flight core without importing the disputed CL-415 content. Pure SI lift/drag/thrust/gravity, estimated controls/stall/flap/gear response, centerline mass/CoG/inertia, gyro-coupled quaternion stepping and validation are verified by **57/57 Unity tests**, a successful current Windows dedicated build (0 errors / 505 warnings), and two separate headless player processes with exact serialized replay. The corrected full runner passes independent analytic/state/raw-file checks. Current server + existing M02 clients also passed three-client fire late-join/repair/restart regression with unchanged fire digest. [Complete report, actual data, failed initialization attempt and graph guides](reports/2026-10-24-m03-debug-flight.md), [reproduce](M03_BUILD_AND_TEST.md), [actual equations/diagrams](physics/debug-flight/COMPARISON.md).

M03 is not complete: no live/networked aircraft, collisions/runway, playable controls/cameras/telemetry, fuel burn, scoop/drop integration or aircraft visual evidence exists. All debug coefficients are estimates; the existing aircraft licence/import gate remains open. No client/Linux build or performance/capacity acceptance is claimed. Next independent piece is a live authority/contact harness with regression evidence while the asset gate is resolved; M05A remains Pending. M00/M02 hitch records and NET-01 remain deferred as documented. Historical activation/closeout entries below retain their original scope.

## Current closeout — 2026-10-23 M02

**M00/M01/source comments/M02 Verified; M03 next with aircraft licence/import blocked.** M02 implements actual reliable baselines, exact-base revision deltas/repair, read-only three-client state display, heartbeat/disconnect and explicit five-second local checkpoint load/save. **48/48 Unity tests**, fresh Windows server/client builds (0 errors, 505/501 warnings), direct and constant 80 ms each-way/5% UDP-loss initial/restart cases passed. Four dedicated processes plus twelve rendered client processes exited 0; all 16 canonical checkpoint files match tick 120/digest `7ba53d73d78a059a`, with existing char and 14,700 kg water. Negative blackout exits 9 and corrupt-load exits 6 were verified separately. [Full evidence, failed attempts, graph reader guide and limits](reports/2026-10-23-m02-replication.md), [commands](M02_BUILD_AND_TEST.md), [screenshots](previews/m02-replication/COMPARISON.md).

Observed cold/jittered startup failures remain **NET-01 (M08/M09)**; the accepted delay/loss case has zero jitter. No fresh Linux replication, WAN/public security, generalized event journal, playable controls/VFX, 60 FPS or capacity qualification is claimed. Current callback hitches, including direct #602 at 1193.136 ms, are marked in [FL-06](performance/FRAME_LOSS_REGISTER.md#m02-measured-callback-overruns--2026-10-23); optimization remains deferred. Project settings/scene and user sky edit are preserved. Historical "M02 pending" entries below describe earlier checkpoints and are superseded by this entry. M05A remains Pending.

Next: resolve the existing [CL-415 licence/source conflict](../AssetSources/Aircraft/CL415/AUDIT.md) for M03; do not import the disputed archive. M07 retains PostgreSQL/crash/exactly-once recovery; M08/M09 retain production traffic/startup/scalability/hosting/performance qualification.

**Document created:** 2026-10-22

## M01 implementation — 2026-10-23

Implemented pure bounded fire/wind/suppression/hazard/char state and the server's 5 Hz adapter. **34/34 Unity tests passed**, Windows dedicated build succeeded with 0 errors/506 warnings, and two independent Windows headless runs matched all nine scenario digests. The live direct-IP regression passed with two existing M00 clients and 349 actual fire steps. [Full evidence/scope](reports/2026-10-23-m01-fire-wind.md), [equations/SVG plots/reader guide](physics/fire-wind/COMPARISON.md), [reproduction](M01_BUILD_AND_TEST.md). Linux/WSL build and two headless replays also passed. Eight cross-OS digests match; gust heat differs by at most 1.11e-16 with identical phase grids, so clients must consume authoritative state. M01 is now Verified. The user-requested function-comment pass is verified (144 C# declarations, 14 tooling scripts, identical executable code, 34 tests); M02 serialization/transport/restart work is now active. At that earlier checkpoint, client fire replication and process-restart integration were unimplemented; M02 local checkpoint helpers are described below. M05A stays Pending.

## Historical activation checkpoint — 2026-10-23

**M00 and M01 Verified; source-comment pass Verified; M02 In Progress.** The user authorized automatic progression after verified completion. [Foundation closeout](reports/2026-10-23-m00-closeout.md) closes M00 against its actual foundation acceptance criteria using the committed build/test/runtime evidence. Aircraft licensing/import remains unchecked in M03; art/counter coverage in M04 and production qualification/deferred optimization in M09. M05A remains Pending. These assignments change scheduling, not evidence requirements or pass results. Historical open-M00 statements below are superseded by this entry.

## Roadmap update — 2026-10-23 M05A

Added **M05A — Player Experience, Controls, UI/UX & Settings** after M05 and before M06. [Scope/dependencies](PLAYER_EXPERIENCE_SPEC.md), [documentation verification](reports/2026-10-23-m05a-roadmap.md). M05A remains Pending; no controls/UI/settings/tutorials were implemented. Real M01–M05 mechanics are prerequisites; final operations screens bind to M06 services. Active foundation closeout/M01 work continues independently.

## Current follow-up — 2026-10-22 coastal import and cross-platform foundation

- Continued from clean `main` at `db4ab83`; Unity 6000.6.7f1 / HDRP 17.6.0 and Netcode/Entities/Transport 6.6.0 remain unchanged. Unity automatically added the Linux 1.1.0 SDK/toolchain dependencies required by its installed support module. [Latest report and dependency audit](reports/2026-10-22-m00-coastal-foundation.md).
- Imported five licensed 2K Poly Haven PBR sets and a Copernicus GLO-90 6 km Petrčane DSM crop into a separate coastal benchmark scene. Source/derived hashes, all mask pixels and import flags passed validation; native Unity height checkpoints match within 0.002 m. [Manifest](ASSET_MANIFEST.md), [notices](../THIRD_PARTY_ASSETS.md), [actual release image](previews/m00-coastal/COMPARISON.md).
- All **20 EditMode cases passed** in the local closed-Editor path and again after the final recorder fix. Windows client/server and Linux x64 Mono server builds succeeded with zero errors; their nonzero warning counts are reported explicitly. The Windows server/two-rendered-client smoke passed on the rebuilt client. [Final markers and executable/runtime hashes](reports/m00-coastal-evidence/final/smoke-summary.json).
- Linux executed a five-second negative headless/clock test under the existing WSL2 kernel with workspace-local Ubuntu Base 24.04.5: null graphics, zero cameras, 300 ticks, expected exit 4 because there were no clients. Both full server inventories exclude coastal content/project materials and both server outputs exclude the client assembly. [Linux scope/hashes](reports/m00-coastal-evidence/linux/startup-summary.json). Native Linux hosting, Linux two-client networking, WAN and capacity remain unverified.
- Added explicit existing-preset selection and a separate five-minute coastal benchmark recorder/runner, with actual rendered warm-up and optional CPU/GPU/draw-call counters. Ordinary scene inspection does not accumulate samples. New data does not establish that the earlier 83.47 ms hitch was fixed; optimization remains deferred.
- Corrected and visually verified the static fog fixture, reran all 20 tests, rebuilt both Windows clients and passed another two-client connection smoke. Two five-minute 1080p Balanced runs completed: clear/static fog averaged 60.00/60.00 FPS, 1% lows 57.93/58.11, p95 16.994/16.988 ms. Both exceed the strict 16.67 ms target. [Current evidence, raw counters, SVG chart and graph reader guide](performance/2026-10-22-m00-coastal.md). VRAM/GC/server tick duration/bandwidth and representative gameplay remain outstanding; no optimization or production pass.
- **M00 remains open.** The user signed into Sketchfab and the official original CL-415 OBJ archive was acquired. Its embedded GPL notice and separate texture credit conflict with the CC BY 4.0 listing. [Licensing/technical archive audit](../AssetSources/Aircraft/CL415/AUDIT.md). Clarify contributor rights/notices or audit an authorized replacement before Unity import; scale/pivots/cockpit/HDRP checks remain outstanding. M01 has not started.

## Latest follow-up — 2026-10-22 frame-loss locations (optimization deferred)

- Analyzed the existing M00 evidence at clean `main` checkpoint `7e11ca9`; no new runtime/build or scene change. All 896 intervals were marked and the six recorded summary fields matched the original JSON.
- Three intervals exceed 20 ms: #1 22.02620 ms, #2 83.47170 ms, #216 20.18150 ms. The largest occurs at reconstructed sample-window time 0.022026–0.105498 s. [Register, SVG timeline/flowchart, exact commands and derived data](performance/FRAME_LOSS_REGISTER.md).
- Screenshot/readback, per-callback instrumentation, shared-machine load and cold startup are documented investigation paths. No cause has been attributed to an individual sample; optimization is deferred at the user's request. The accepted full-run performance results below remain unchanged.
- Recorded permanent guidance for actual rendered SVG diagrams/flowcharts and correctly typeset Markdown formulas, with editable sources and fallback images where necessary. The register explains the first graph's axes, tick spacing, intervals, target lines and zoom; future final milestone commits must include such reader guidance.
- Verification: `python scripts/analyze_frame_intervals.py --plot` (Matplotlib 3.11.2 in ignored tools directory); original metrics agreement, negative mismatch rejection, SVG XML/local links and visual review. Runtime screenshots/physics comparisons/new builds are not applicable to this offline evidence change. M00 remains In Progress, with future gameplay milestones pending.

## Current checkpoint — 2026-10-22 M00 foundation (In Progress)

- Starting branch/checkpoint: clean `main` at `f93ad75`; the existing Unity project was preserved.
- Live Editor: Unity 6000.6.7f1, HDRP 17.6.0 active, CLI 1.0.0-beta.14 current. Added Netcode for Entities 6.6.0; Entities/Transport 6.6.0 resolved with Package Manager.
- Implemented six modular C# assemblies, explicit one-role-per-process IPv4 bootstrap, endpoint validation, pure fixed tick clock, separate foundation scenes, build entry points and a three-process smoke runner with runtime capture/diagnostic metrics.
- Tests: **15 EditMode foundation cases passed**, 0 failed. [Results](reports/m00-evidence/editmode-tests.json).
- Windows dedicated-server build: Succeeded, zero errors, 515 warnings, Server subtarget, no serialized HDRP assets or project materials. Server output contains Server/Shared/Simulation assemblies and excludes Client. Its no-client negative smoke correctly returned exit 4 with null graphics, no cameras and advancing clock ticks.
- Windows release client build: Succeeded, zero errors; latest cached report took 24.41 s and reported zero new warnings. The first full HDRP build took 702.71 s and reported 507 warnings. [Latest report](reports/m00-evidence/client-build.json); historical full-build warnings are retained separately.
- Accepted smoke: dedicated server PID 11808 and rendered clients 6708/11820, distinct NetworkIds 1/2, server peak connections=2 and 4,799 fixed clock ticks. All three exits=0. [Markers and runtime assembly/executable hashes](reports/m00-evidence/smoke-summary.json). Opposite-role FIRELINE assembly exclusion passed in both outputs.
- Actual [BEFORE/AFTER visual evidence](previews/m00-foundation/COMPARISON.md) was inspected. Generated calibration geometry is visible in the release client; no Adriatic art integration is claimed.
- Initial diagnostic: 896 real-render intervals over 15 s after five rendered seconds of warm-up, 720p High Fidelity, two clients on i5-9400F / GTX 1660 SUPER: average 59.73 FPS, 1% low 38.23, p95 interval 17.36 ms. [Conditions/raw data](performance/2026-10-22-m00-calibration.md). No representative 1080p/60 FPS acceptance.
- All M01–M09 tasks remain pending. M00 stays open for aircraft/coastal asset provenance/import and representative benchmark evidence.
- Detailed implementation, package references, known build issues and verification limits: [M00 report](reports/2026-10-22-m00-foundation.md). Commands: [build and test guide](M00_BUILD_AND_TEST.md).
- Implementation/evidence commit `3d006ae1aecb8ba15a850d66c7a5aae96fc0c943` was pushed to existing remote `main` and verified against `git ls-remote`; this publication record follows in a documentation-only checkpoint.

## Previous checkpoint — 2026-10-22 documentation workflow

- Local filesystem inspected: existing Unity 6000.6.7f1 project, declared HDRP 17.6.0, starter scene/settings/tutorial scripts. No FIRELINE gameplay implementation found in the source inventory.
- Existing remote: `https://github.com/itsmangooo/FIRELINE-ADRIATIC`, confirmed empty before publication. Local Git was initialized on `main`; baseline checkpoint `8577e4d` preserves existing project/planning files.
- Added permanent AGENTS rules, README showcase structure, evidence templates/directories, changelog, performance history and Git cache exclusions. See [workflow report](reports/2026-10-22-documentation-workflow.md).
- Documentation validation: passed required README headings, 58 local links across 31 Markdown files, balanced code fences, retained original principles/reference sections, whitespace, cache exclusions and preservation of Unity files/prompts. Workflow commit `7e544a1` was pushed to existing remote `main`; `git ls-remote` matched local HEAD. This publication record is a subsequent documentation-only checkpoint.
- That checkpoint was documentation-only and had no runtime evidence; the subsequent M00 work above supersedes its inspection limits.

## What exists in this pack

- Complete planning specifications, verified/candidate asset source URLs, simulation/gameplay/network/persistence/performance designs.
- Ordered tasks and copy-ready Codex prompts.
- Architecture/design diagrams (Mermaid).

## Remaining acceptance gaps

- Windows local build/connection/render smoke and the scoped Linux WSL negative startup are verified; native Linux hosting, Linux multiplayer, WAN/NAT, packet-loss/reconnect and hosted CI remain unverified.
- No gameplay fire/wind/water/flight model, database instance, persistent world or game-ready aircraft exists. Actual coastal DSM/PBR derivatives are imported; generated vegetation/building/sea/fog proxies are benchmark content. The tick clock is a scheduling utility only.
- CL-415 authentication/download is resolved; inconsistent listing/archive licence notices and separate texture authorship block Unity import. Resolve that record or audit an authorized replacement; then validate pivots, scale, materials, missing parts and screenshots.
- No representative 1080p/60 FPS acceptance or 100-player benchmark exists. Diagnostic calibration-scene frame intervals cannot establish either.
- CL-415 realistic flight coefficients, cockpit and specific licensing details still need investigation/calibration.

## Immediate next action

Continue M02 by wiring validated checkpoints into the real server lifecycle, then implement baseline/delta transport, late join and restart validation. The [CL-415 licence/import gate](../AssetSources/Aircraft/CL415/AUDIT.md) remains an M03 prerequisite, not a pure fire-simulation dependency. Reproduce the current foundation using [M00_BUILD_AND_TEST.md](M00_BUILD_AND_TEST.md). Latest Windows regression logs remain under `Builds/Smoke/20261022-214232/`; [current source/runtime evidence](reports/2026-10-22-m00-coastal-foundation.md) supersedes earlier captures. The connection harness establishes the process boundary; M01/M02 must implement and verify shared authoritative fire state separately.

## Update template for each checkpoint

- Branch & commit:
- Feature/diff:
- Assets integrated + credits:
- Test command(s):
- Results / screenshots / log paths:
- Performance device/settings/data:
- Known limitations:
- Next task:

## Source explanations — 2026-10-23

[Comment-pass evidence](reports/2026-10-23-source-comments.md): project C# functions/properties and tooling entry points are documented; source identity and 34/34 Unity tests pass. This is documentation/audit tooling, with no new runtime or performance claims. M02 starts with bounded snapshot encoding/validation and restoration; its real transport/three-client/restart acceptance remains open.

## M02 local checkpoint increment — 2026-10-23

Bounded little-endian snapshots, SHA-256 corruption detection, semantic/canonical validation, independent world restoration and a single-writer file store are implemented. [Report and rendered flow](reports/2026-10-23-m02-checkpoints.md). Seven supplementary .NET test methods and the full **41/41 Unity EditMode regression pass**, including real local file replacement checks. Normal server startup/save scheduling, baseline/delta delivery, three-client convergence, delayed/lost packets and actual process restart remain pending. M02 is In Progress. No physics/render/performance acceptance is inferred.
