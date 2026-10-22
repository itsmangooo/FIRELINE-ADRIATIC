# FIRELINE: ADRIATIC — Ordered implementation backlog

**Statuses:** Pending / In Progress / Blocked / Done / Verified. Do not mark game milestones Verified without build/run evidence. Documentation-only increments require applicable documentation checks. Every milestone ends in docs + Git checkpoint and a handoff summary. Initial status for all game milestones: **Pending**.

## Documentation workflow — 2026-10-22

- [x] Apply permanent evidence/showcase rules to AGENTS.md and preserve existing project guidance.
- [x] Add the nine README showcase sections, documentation directories and reusable templates.
- [x] Establish chronological changelog and performance history with no invented evidence.
- [x] Verify documentation structure/links and preservation of existing Unity files and prompts.
- [x] Commit and push the verified documentation increment to the existing remote (`7e544a1`; remote `main` matched local HEAD after push).

Evidence: [documentation workflow report](docs/reports/2026-10-22-documentation-workflow.md). This administrative increment does not complete M00.

## Evidence gates for every significant feature

- [ ] Capture applicable baseline before editing; record actual missing baselines.
- [ ] Verify implementation and run relevant tests with reproducible commands/artifacts.
- [ ] Add real visual captures, actual mathematics, two physics diagrams, equivalent numerical comparisons and gameplay/architecture flows where applicable.
- [ ] Measure rendering/simulation performance where possible; record limitations and concrete evidence follow-ups.
- [ ] Update README, CHANGELOG, this task list, DEVELOPMENT_STATUS and applicable asset/performance records.
- [ ] Commit implementation and evidence together, push through the existing branch workflow, and verify the remote revision.

Apply this checklist to each milestone using [the workflow](docs/DOCUMENTATION_WORKFLOW.md). These are recurring gates, not tasks that can be permanently checked once. Missing required evidence keeps the affected milestone open.

## M00 — Preproduction & Unity audit (start here)

**Status: In Progress.** [Implementation and evidence](docs/reports/2026-10-22-m00-foundation.md). M01 remains locked while M00 acceptance gaps remain.

- [x] Inspect existing Unity 6 HDRP project, package versions, CLI/editor tooling and Git branch; preserve original project.
- [x] Research and resolve Unity 6.6 / HDRP 17.6 / Netcode for Entities + Transport 6.6 versions; record build integration issues.
- [x] Verify the rendered HDRP release client and dedicated server with two separate client processes on direct-IP loopback; all three accepted smoke exits were 0.
- [ ] Inspect `AssetSources/`, download only approved licenses, convert minimum terrain/aircraft source assets; document provenance and missing cockpit parts.
- [x] Set up six modular assemblies, 15 passing EditMode cases and minimal CI/build instructions; document actual dependency boundaries.
- [x] Build Windows dedicated server and verify null-graphics/no-camera startup plus expected failure when clients are absent.
- [x] Finish release client build/runtime evidence and verify opposite-role assembly exclusion in both outputs; inspect actual release screenshot after rendered warm-up.
- [x] Add an initial calibration render scene and record named local hardware; preserve existing quality presets.
- [ ] Establish initial 60 FPS benchmark scene and named hardware/quality preset.

The calibration scene is not the representative populated/coastal/smoky benchmark required for performance acceptance. Aircraft/terrain asset integration, full profiling counters and Linux execution remain outstanding.

- [x] Record the initial 720p diagnostic sample with raw render intervals and named hardware; no 1080p/60 FPS pass claimed.
- [x] Map recorded slow intervals and source suspects in the [frame-loss register](docs/performance/FRAME_LOSS_REGISTER.md), verify the metrics and add rendered SVG timeline/flowchart.
- [ ] **Deferred at user request:** resume FL-01–FL-05 attribution and only then optimize with equivalent BEFORE/AFTER measurements. Continue M00 work independently; this deferral does not waive its benchmark acceptance gate.
- [ ] Collect representative 1080p baseline and five-minute runs, CPU/GPU execution counters, draw calls, VRAM, server tick duration and network bandwidth.
- [ ] Validate Linux dedicated-server build/run and the supplied closed-Editor batch/CI path on a configured worker.

Future evidence diagrams/flowcharts must be actual embedded SVG drawings with editable sources; mathematical formulas must use correctly formatted Markdown math or an SVG fallback. Explain graph axes, ticks, intervals, recording origin and annotations in plain language before each milestone's final commit. See [permanent rendering rules](docs/DOCUMENTATION_WORKFLOW.md#rendered-diagrams-and-mathematics).

**Accept:** Project opens, tests run, one client release build and headless server build smoke launches; docs complete; no gameplay falsely marked complete.

## M01 — Headless fire/wind simulation

- [ ] Data model fixed chunk IDs, burn phases, fuel/wetness, deterministic seeded tests.
- [ ] Fixed-step authoritative wind field, with gusts and changes; fuel/slope/wind influence spread.
- [ ] Vegetation burn states, char and smoke hazard grid; bounded inactive sector simulation.
- [ ] Simple water application to fire grid (manual test event only), state assertions.

**Accept:** Server-only fire simulation produces reproducible checksums and expected wind-driven direction change; no Unity HDRP graphics initialized on server.

## M02 — Two-client replication + persistence proof

- [ ] Connect two real processes; initial fire baseline + ordered revision deltas.
- [ ] Late join client #3 gets identical burned world after convergence.
- [ ] Snapshot/write restore after restart: no regrowth/score duplication; test packet delay/loss.

**Accept:** 3-client smoke test, no conflicting fire states, video/log evidence and digest match.

## M03 — CL-415 flight & water physics

- [ ] Approved CL-415 asset, distinct movable parts inventory; flight model in SI, config curves+HUD telemetry.
- [ ] Takeoff, landing, wind response; payload/fuel mass; water scoop validity.
- [ ] Server-calculated droplet parcels, wind-drift coverage and graded fire reduction.

**Accept:** Repeatable high vs low drop + crosswind footprint tests, mass conservation, comparable clients observe same suppressed cells.

## M04 — HDRP coastal art + VFX

- [ ] Import Poly Haven CC0 materials correctly into HDRP Terrain Lit/Lit and sky setup.
- [ ] Authored small coastal hills + sea + woodland + airstrip; bind burning/char states to visual material swaps.
- [ ] VFX Graph fire/smoke/sparks/water/steam driven by server states, with LOD/quality presets.

**Accept:** Styled playable scene + material/source credits, GPU frame capture under representative effects.

## M05 — Ground forces / first usable hose

- [ ] Rigged player/firefighter, driving fire engine, pump/tank/water supply, nozzle + hose constraint.
- [ ] Ground water action validated on server, fire grid responds; multiplayer co-op with aircraft.

**Accept:** Ground and aircraft users suppress same fire, correctly debit resources.

## M06 — Operations and regional incidents

- [ ] Zemunik staging, fictional units, in-game qualifications, commander, dispatcher and role leases.
- [ ] Procedural incident creation and multiple simultaneous active fires.
- [ ] AAR, useful contribution data, shift summaries, optional training mode.

**Accept:** Dispatch two incidents, assign units and complete one independently without world reset.

## M07 — PostgreSQL and permanent world

- [ ] Schema migrations, player/unit permissions, world/incident/weather snapshots.
- [ ] Backups, crash recovery, idempotent water/drop events, admin audit.

**Accept:** Kill server, restart and restore burns, players and active incident exactly once.

## M08 — Region streaming & 100-user scalability

- [ ] Geographic compression and streaming sectors, floating origin if necessary.
- [ ] Relevancy interest management and adaptive network updates.
- [ ] Load tests 4/16/32/64/100 **realistic** simulated clients and multiple fires/9 aircraft.

**Accept:** Verified benchmark results with server hardware, tick p95, traffic and RAM; no '100-player ready' claim if failing.

## M09 — Modding, host packaging, 60 FPS and release

- [ ] Versioned data/bundle content pipeline and server mod validation.
- [ ] Windows/Linux server packaging, operator docs/config and backups.
- [ ] Client Balanced preset achieves p95 frame ≤16.67ms at 1080p on named reference PC in S3/S4 scenes, with multiple fires and smoke; report p99 and exceptions.
- [ ] AAR UI, radio/communication, tutorial, Steam/demo/marketing material with true gameplay only.

**Accept:** Self-hosted dedicated server startup + mods validation + 60 FPS evidence + demo loop. If not, leave unfinished.

## Rules for task execution

- Start with M00 then M01. Dependent milestones unlock after their prerequisites are verified.
- Record exact test commands and artifacts in `docs/DEVELOPMENT_STATUS.md`.
- Commit and push **verified** work at safe checkpoints; never force-push, modify previous RANKS projects or embed secrets.
- Update assets manifest when any actual asset is downloaded or changes status.
