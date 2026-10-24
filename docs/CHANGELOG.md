# Changelog

## 2026-10-24 — M03 independent debug-flight core verified increment

- Added pure double-precision SI force/moment, payload/CoG/inertia and quaternion 6DOF stepping with numerical guards; all parameters explicitly synthetic, not CL-415 calibration.
- 57/57 Unity tests, fresh Windows headless build (0 errors / 505 warnings), two real debug-proof processes and unchanged fire late-join/repair/restart regression pass. Raw trajectories/CSV replay exactly within each format; the independent cross-format decimal tolerance is disclosed.
- Added actual BEFORE/AFTER SVGs, recorded coefficient/payload/convergence plots, elapsed-Step measurements and plain-language graph guides. [Evidence and limits](reports/2026-10-24-m03-debug-flight.md). M03 stays In Progress; aircraft import remains licence-blocked. No visual/gameplay/aircraft-network, optimization, 60 FPS or capacity claim.

## 2026-10-23 — M02 live replication and checkpoint proof verified

- Implemented targeted bounded baselines/revision deltas, guarded fragment assembly, exact ACK/gap repair, heartbeat/timeouts and invalid-control rejection. Clients display authoritative state without stepping/mutating fire. Explicit checkpoint load/save now runs in the actual dedicated lifecycle.
- 48/48 Unity tests, current Windows release client/headless builds and sixteen accepted direct/delayed-loss/restart process exits pass; independent validation confirms all 16 canonical state files. Blackout/corrupt-load negative exits and save preservation pass separately.
- Added actual before/after screenshots, rendered editable SVG architecture/interval graphs, raw callback/UDP evidence, explanations of graph ticks/intervals and marked hitches. [Full report and limitations](reports/2026-10-23-m02-replication.md). Cold/jittered startup attempts failed and remain NET-01; constant 80 ms each-way/5% loss passed. No optimization, 60 FPS, 100-player, public/Linux-network, M05A or production-persistence claim. M03 is next with its existing asset-rights gate.

Record meaningful development changes in dated entries, newest first. Link implementation evidence and disclose limitations. Plans and templates are not completed game features.

## 2026-10-23 — M02 bounded checkpoint helper increment

- Added explicit bounded binary snapshot encoding, SHA-256 corruption detection, semantic/canonical validation and independent world restoration retaining char, wind, revisions, scheduling and kg balances.
- Added a single-writer local file store with bounded reads and flushed temporary-file replacement; no silent fresh-world fallback. Seven new tests and the full 41/41 Unity suite pass.
- [Actual flow, tests, format limits and remaining work](reports/2026-10-23-m02-checkpoints.md). M02 remains In Progress: no normal server lifecycle wiring, live baseline/delta protocol, three-client convergence, packet-loss recovery or process-restart proof yet. No physics/render/performance improvement claimed.

## 2026-10-23 — Human-readable source explanations

- Explained all 144 project C# function/property declarations and all 14 existing tooling scripts, with purpose, units, ownership, side effects and limits. Vendor/generated code is excluded.
- Added reproducible offline source-identity/coverage audits: unchanged C# tokens in five symbol configurations, Python ASTs, PowerShell tokens and shell tokens; 34/34 Unity tests pass.
- [Report and verification artifacts](reports/2026-10-23-source-comments.md). No visual, mathematical or runtime behavior changed; new screenshots/physics/performance captures are not applicable. M02 is now active.

## 2026-10-23 — M01 authoritative fire/wind implementation

- Added bounded pure C# fire/fuel/wind/moisture/slope/manual-water/hazard/char behavior, stable IDs and detached snapshot/latest-step delta contracts; a 5 Hz dedicated adapter runs the same model.
- All 34 Unity tests passed. Windows and Linux/WSL dedicated builds and two independent headless 9-case runs per platform passed; digests and accounting were independently recomputed. Existing two-client connection smoke also passed while the server advanced the real fire model.
- Closed M01; queued the user-requested whole-codebase comment pass before M02 implementation. The gust case has a recorded 1.11e-16 cross-OS heat difference; same-platform replay and all cross-OS phase grids match in these fixtures.
- Added actual SI/proxy equations, BEFORE/AFTER SVG flows, raw numerical comparisons, final-grid/time-series plots and plain-language graph guides. [Results and limitations](reports/2026-10-23-m01-fire-wind.md). Parameters remain uncalibrated gameplay approximations; no client fire/VFX, replication, restart persistence, 60 FPS or capacity pass is claimed.

## 2026-10-23 — M00 foundation closed; M01 activated

- Closed M00 against committed project-open/test/build/headless/direct-IP evidence; [acceptance/dependency report](reports/2026-10-23-m00-closeout.md).
- Kept aircraft licensing/import in M03, art/counter coverage in M04 and final performance qualification/deferred optimization in M09. No issue is relabeled passed. M01 is active; M05A remains Pending.

## 2026-10-23 — M05A player-experience roadmap integration

- Added M05A after M05 and before M06 without renumbering: controls/interactions, functional UI/HUD, persistent settings, accessibility and tutorials. [Complete requirements and dependencies](PLAYER_EXPERIENCE_SPEC.md).
- Defined real M01–M05 mechanics and staged M06 producer/UI bindings; M09 retains release qualification. Earlier diagnostic controls do not complete M05A.
- Updated roadmap/design/architecture/prompt navigation/status. Documentation only, M05A remains Pending; active M00 closeout/M01 work continues. [Verification and scope](reports/2026-10-23-m05a-roadmap.md).

## 2026-10-22 — M00 coastal import, Linux startup and batch verification

- Acquired five audited Poly Haven CC0 surface sets and a real Copernicus GLO-90 crop, with exact source/derivative hashes, full mask/import verification and required distribution notices. Added a separate coastal render scene with explicitly generated vegetation/building/sea/fog proxies and actual [visual evidence](previews/m00-coastal/COMPARISON.md).
- Added a longer release benchmark path, 30 rendered seconds of warm-up, existing-preset selection, optional CPU/GPU/draw-call sampling and a bounded recording lifetime. [Implemented diagrams, mathematical conversion, numerical checks and limits](reports/2026-10-22-m00-coastal-foundation.md).
- Rejected an invisible-fog capture, corrected volumetric depth/camera overrides, then completed a matched 1080p Balanced clear/static-fog pair with actual release captures. Both averaged 60.00 FPS, with p95 16.994/16.988 ms: the strict 16.67 ms gate is not passed. [Raw counters, SVG timeline and graph explanation](performance/2026-10-22-m00-coastal.md). Optimization remains deferred.
- Verified 20 EditMode tests, the closed-Editor batch path, Windows client/server builds, Linux x64 server cross-build and Linux negative startup under workspace-local Ubuntu/WSL2. The Windows server/two-client smoke also passed on the final rebuilt client. Full builds retain nonzero warning counts; the reports record them.
- Unity auto-installed the required Linux 1.1.0 SDK/toolchain dependencies; existing render/network package versions and original scenes/presets remain unchanged. No hosted CI, native Linux qualification, production 60 FPS pass, game-ready aircraft or gameplay fire/wind model is claimed.
- M00 remains open: the official CL-415 archive was acquired after sign-in, but conflicting listing/archive licence notices block import. [Audit](../AssetSources/Aircraft/CL415/AUDIT.md). Remaining representative evidence is tracked separately. Optimization remains deferred and M01 has not started.

## 2026-10-22 — Frame-loss register and rendered documentation rules

- Analyzed the existing 896 M00 callback intervals: three exceed 20 ms; the largest is 83.47170 ms at sample #2. Added a [location register](performance/FRAME_LOSS_REGISTER.md), every-sample flags, recomputed metrics, a reproducible analyzer, and actual SVG/PNG timeline and flowchart.
- Marked screenshot/readback, instrumentation, resource-sharing and startup paths for later profiling; their contribution to individual hitches remains unproven. Optimization is deferred at the user's request. No runtime, scene, package or physical model changed; previous build/test evidence is unchanged and M00 stays open.
- Updated permanent AGENTS/workflow/template guidance: render actual SVG diagrams and flowcharts, retain editable sources, and correctly typeset Markdown mathematics with vector fallbacks when needed. Added a plain-language graph guide and required axes/tick/interval explanations before future final milestone commits.
- Verified metrics against the original JSON, rejection of inconsistent evidence, local links, SVG structure, plot/flowchart appearance and authored whitespace. See the register for exact commands and limits.

## 2026-10-22 — M00 Unity client/server foundation

- Added six modular C# assemblies, a pure fixed clock, validated direct-IP startup and role-specific foundation scenes/build entry points.
- Resolved Netcode for Entities/Entities/Transport 6.6.0 alongside existing Unity 6000.6.7f1 / HDRP 17.6.0; retained project scenes, presets and Editor version. Unity-generated serialization/build-setting changes are documented in the report.
- Verified 15 EditMode cases, Windows release client and dedicated-server builds, opposite-role assembly exclusion, a no-client failure case, and one headless server with two distinct rendered clients exiting successfully.
- Added actual [visual comparison](previews/m00-foundation/COMPARISON.md), [clock documentation](physics/foundation-clock/COMPARISON.md), [limited 720p diagnostics](performance/2026-10-22-m00-calibration.md), raw samples and build/runtime evidence. Rejected hidden/unready render captures rather than publishing them as valid results.
- M00 remains open for licensed aircraft/coastal asset integration and representative performance acceptance. No future gameplay milestone or 100-player/60 FPS claim is made. [Full report](reports/2026-10-22-m00-foundation.md).

## 2026-10-22 — Permanent documentation and GitHub showcase workflow

- Checkpointed the existing Unity project and planning pack locally at `8577e4d` before documentation edits; the existing GitHub repository was confirmed empty.
- Extended [AGENTS.md](../AGENTS.md) with permanent README, screenshot, mathematics, BEFORE/AFTER diagram, gameplay, performance and completion requirements.
- Expanded [README](../README.md) into the requested nine-section showcase while retaining original pack guidance and reference material.
- Added the [workflow guide](DOCUMENTATION_WORKFLOW.md), [templates](templates/README.md), [visual](previews/README.md), [physics](physics/README.md) and [performance](performance/README.md) evidence directories and [performance history](PERFORMANCE_HISTORY.md).
- Updated task/status tracking and added Unity/IDE cache exclusions. Unity assets, scripts, scenes, package configuration and project settings were not changed by the workflow increment.
- Validation and scope are recorded in the [documentation report](reports/2026-10-22-documentation-workflow.md). No gameplay, physics, screenshot, FPS or multiplayer-capacity claim is made; all game milestones remain pending.
