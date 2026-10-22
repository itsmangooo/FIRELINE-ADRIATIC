# Development status — handoff

**Document created:** 2026-10-22

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

Resolve the [CL-415 licence/import gate](../AssetSources/Aircraft/CL415/AUDIT.md) and remaining representative measurement tasks before closing M00 and starting M01. Reproduce the current foundation using [M00_BUILD_AND_TEST.md](M00_BUILD_AND_TEST.md). Latest Windows regression logs remain under `Builds/Smoke/20261022-214232/`; [current source/runtime evidence](reports/2026-10-22-m00-coastal-foundation.md) supersedes earlier captures. The connection harness establishes the process boundary; M01/M02 must implement and verify shared authoritative fire state separately.

## Update template for each checkpoint

- Branch & commit:
- Feature/diff:
- Assets integrated + credits:
- Test command(s):
- Results / screenshots / log paths:
- Performance device/settings/data:
- Known limitations:
- Next task:
