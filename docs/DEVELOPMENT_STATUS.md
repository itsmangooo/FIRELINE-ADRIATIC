# Development status — handoff

**Document created:** 2026-10-22

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

- Windows local build/connection/render smoke is verified; Linux execution, WAN/NAT, packet-loss/reconnect and hosted CI remain unverified.
- No gameplay fire/wind/water/flight model, database instance, persistent world or game-ready aircraft/coastal assets exist. The tick clock is a scheduling utility only.
- Asset URLs may be indexed; actual download/import/rig readiness must be checked at integration time.
- No representative 1080p/60 FPS acceptance or 100-player benchmark exists. Diagnostic calibration-scene frame intervals cannot establish either.
- CL-415 realistic flight coefficients, cockpit and specific licensing details still need investigation/calibration.

## Immediate next action

Complete remaining M00 asset provenance/import and representative benchmark work before starting M01. Reproduce this foundation using [M00_BUILD_AND_TEST.md](M00_BUILD_AND_TEST.md): filtered live EditMode tests, sequential `FoundationBuild.BuildServer()` / `BuildClient()` and `pwsh -File scripts/Test-M00Smoke.ps1`. Runtime logs remain under `Builds/Smoke/20261022-185723/`; concise committed evidence is indexed in the [M00 report](reports/2026-10-22-m00-foundation.md). The connection harness establishes the process boundary; M01/M02 must implement and verify shared authoritative fire state separately.

## Update template for each checkpoint

- Branch & commit:
- Feature/diff:
- Assets integrated + credits:
- Test command(s):
- Results / screenshots / log paths:
- Performance device/settings/data:
- Known limitations:
- Next task:
