# Development status — handoff

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
