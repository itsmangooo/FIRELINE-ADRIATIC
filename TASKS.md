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

**Status: Verified — foundation scope, closed 2026-10-23.** [Acceptance and dependency closeout](docs/reports/2026-10-23-m00-closeout.md), [original foundation](docs/reports/2026-10-22-m00-foundation.md), [coastal/build evidence](docs/reports/2026-10-22-m00-coastal-foundation.md). Project opens, 20 tests pass, client/headless builds and direct-IP smoke are verified. Aircraft import is an open M03 dependency; production art/60 FPS qualification remains M04/M09 work. No licence or performance requirement is waived. M01 followed this closeout and is verified below.

- [x] Inspect existing Unity 6 HDRP project, package versions, CLI/editor tooling and Git branch; preserve original project.
- [x] Research and resolve Unity 6.6 / HDRP 17.6 / Netcode for Entities + Transport 6.6 versions; record build integration issues.
- [x] Verify the rendered HDRP release client and dedicated server with two separate client processes on direct-IP loopback; all three accepted smoke exits were 0.
- [x] Inspect `AssetSources/`; acquire five licensed 2K PBR surfaces and an actual Copernicus GLO-90 crop, convert/import terrain and retain provenance, pixel/hash/import checks and notices.
- [x] Set up six modular assemblies, 20 passing EditMode cases and minimal CI/build instructions; document actual dependency boundaries.
- [x] Build Windows dedicated server and verify null-graphics/no-camera startup plus expected failure when clients are absent.
- [x] Finish release client build/runtime evidence and verify opposite-role assembly exclusion in both outputs; inspect actual release screenshot after rendered warm-up.
- [x] Add an initial calibration render scene and record named local hardware; preserve existing quality presets.
- [x] Establish a separate initial coastal render benchmark scene with named hardware/existing quality presets and a five-minute release-run harness. Generated vegetation/building/static-fog proxies do not establish production S1–S6 acceptance or a 60 FPS pass.

The original box calibration scene is not a representative performance benchmark. The new coastal proxy scene advances render-test infrastructure; game-ready aircraft/actors, full profiling counters and production scenario acceptance remain outstanding. Actual terrain/material import is now verified.

- [x] Record the initial 720p diagnostic sample with raw render intervals and named hardware; no 1080p/60 FPS pass claimed.
- [x] Map recorded slow intervals and source suspects in the [frame-loss register](docs/performance/FRAME_LOSS_REGISTER.md), verify the metrics and add rendered SVG timeline/flowchart.
- [x] Complete the initial proxy scene's matched 1080p Balanced five-minute clear/static-fog pair, inspect actual release captures, verify raw CSV summaries and render/inspect SVG interval plots with reader guides. CPU/main/GPU timing, draw calls and Unity allocated bytes were available. [Measured results](docs/performance/2026-10-22-m00-coastal.md); p95 exceeds 16.67 ms, no production pass.
- [x] Validate Windows and Linux dedicated-server builds and the supplied closed-Editor batch/test path on this licensed local worker. Linux executed a five-second headless/clock negative test under WSL2 with a workspace-local Ubuntu Base root; it is not native Linux, Linux multiplayer, hosted CI or capacity qualification.

Future evidence diagrams/flowcharts must be actual embedded SVG drawings with editable sources; mathematical formulas must use correctly formatted Markdown math or an SVG fallback. Explain graph axes, ticks, intervals, recording origin and annotations in plain language before each milestone's final commit. See [permanent rendering rules](docs/DOCUMENTATION_WORKFLOW.md#rendered-diagrams-and-mathematics).

**Accept:** Project opens, tests run, one client release build and headless server build smoke launches; docs complete; no gameplay falsely marked complete.

## M01 — Headless fire/wind simulation

**Status: Verified — 2026-10-23.** Depends on verified M00. [Implementation/build/runtime evidence](docs/reports/2026-10-23-m01-fire-wind.md), [actual equations/comparisons and SVG plots](docs/physics/fire-wind/COMPARISON.md). 34 tests, two Windows and two Linux/WSL headless scenario processes, and a live two-client connection regression passed. Same-runtime replay is verified; the cross-OS gust precision difference is recorded. M02 owns actual client replication/restart recovery; no player-facing fire or field calibration is claimed.

- [x] Data model fixed chunk IDs, burn phases, fuel/wetness, deterministic seeded tests.
- [x] Fixed-step authoritative wind field, with gusts and changes; fuel/slope/wind influence spread.
- [x] Vegetation burn states, char and smoke hazard grid; bounded inactive sector simulation.
- [x] Simple water application to fire grid (manual test event only), state assertions.

**Accept:** Server-only fire simulation produces reproducible checksums and expected wind-driven direction change; no Unity HDRP graphics initialized on server.

## Post-M01 — Human-readable source documentation

**Status: Verified — 2026-10-23.** [Coverage/source-identity and 34-test evidence](docs/reports/2026-10-23-source-comments.md). User request: explain each project function in plain language, including inputs/outputs, important SI units, authority, side effects and non-obvious logic. Cover maintained C# and tooling scripts; preserve third-party/generated code and behavior. Verify the comment-only diff and relevant checks, then commit/push before M02 implementation.

## M02 — Two-client replication + persistence proof

**Status: Verified — bounded Windows proof, 2026-10-23.** [Live build/process/state/screenshot/measurement report](docs/reports/2026-10-23-m02-replication.md), [reproduction](docs/M02_BUILD_AND_TEST.md). 48/48 Unity tests and two successful current Windows builds; direct and 80 ms each-way/5% loss cases each passed two separate server processes with three real rendered clients per phase. Sixteen canonical state files agree. Cold/jittered connection startup remains NET-01 in M08/M09, not a claimed pass; no production/capacity qualification.

- [x] Bounded checkpoint codec, candidate restoration and local store: 41/41 Unity tests passed; [scope/evidence](docs/reports/2026-10-23-m02-checkpoints.md). Real process restart remains separate.
- [x] Connect two real processes; initial fire baseline + ordered revision deltas, stale-base gap repair and invalid-control rejection.
- [x] Late join client #3 gets identical burned world after convergence; initial char and canonical SHA-256 checked in direct/delayed-loss cases.
- [x] Snapshot/write restore after restart: no regrowth or repeat proof events; actual constant delay/UDP loss, heartbeat blackout and corrupt-load tests pass. Scores/inventory do not exist; their exactly-once persistence remains M07, not a completed score-duplication test.

**Accept:** 3-client smoke test, no conflicting fire states, video/log evidence and digest match.

## M03 — CL-415 flight & water physics

**Status: In Progress — isolated debug-flight core verified 2026-10-24; full milestone open.** M02 is verified. [Actual model, 57 tests, headless build/two-process proof and fire regression](docs/reports/2026-10-24-m03-debug-flight.md), [equations/BEFORE-AFTER SVGs](docs/physics/debug-flight/COMPARISON.md). `DEBUG_FLIGHT_ONLY_V1` retains estimated parameters. The user authorized temporary CL-415 prototype use on 2026-10-24; its separate static HDRP exterior is now imported. The model is not bound to the numerical solver; no playable flight is claimed. [Import/capture evidence](docs/previews/m03-cl415-prototype/COMPARISON.md).

- [x] Pure SI debug force/mass/CoG/inertia, quaternion 6DOF candidate stepping and bounded numerical inputs, with analytic and repeated real headless-player evidence. This does not complete the CL-415/telemetry/flight-loop tasks below.
- [x] Pure bounded debug runway-contact prerequisite: three-point gear support, CoG/rotating-point forces, Coulomb-limited braking/steering, finite surface and candidate rejection. **71 tests and two actual headless 19,800-sample runs pass**; not a live aircraft, terrain/airframe collision or landing-gameplay completion. [Equations, matched comparisons, diagrams and evidence](docs/physics/runway-contact/COMPARISON.md).
- [ ] Add complete airframe/terrain/obstacle collision and tested fixed-substep host integration; the debug rectangle/three tire points do not cover wingtip, belly, slope or swept impacts.
- [ ] Integrate a live server-owned aircraft and pilot permission/input handling, flight replication/correction, physical contact, test controls/cameras and actual gameplay telemetry; capture real screenshots and populated-build performance when introduced.
- [ ] Replace debug estimates with documented aircraft/variant config and calibrated curves; implement fuel consumption and actual variable-mass/resource behavior.

- [x] Acquire/audit the official original archive and import its exterior as a **user-authorized temporary prototype**: 144 named meshes, eleven HDRP materials, nine textures, orientation/dimensions audit and separate static inspection scene. Original notices retained. [Actual build/capture and limits](docs/previews/m03-cl415-prototype/COMPARISON.md). Source names do not establish working pivots/rig or a complete cockpit.
- [ ] Verify/rebase animated-part pivots and usable cockpit coverage; bind visuals to the actual server-owned flight state when live flight is implemented.
- [ ] Replace the temporary model with the user's intended commercial production asset before launch, or reconcile all contributor rights/notices; re-run import/animation/build/screenshots after replacement.

- [ ] Approved CL-415 asset, distinct movable parts inventory; flight model in SI, config curves+HUD telemetry.
- [ ] Takeoff, landing, wind response; payload/fuel mass; water scoop validity.
- [ ] Server-calculated droplet parcels, wind-drift coverage and graded fire reduction.

**Accept:** Repeatable high vs low drop + crosswind footprint tests, mass conservation, comparable clients observe same suppressed cells.

## M04 — HDRP coastal art + VFX

- [ ] Complete representative visual workload/counter coverage (VRAM/residency, GC, render-thread/triangles) as real assets/VFX become available; current generated proxies do not establish production acceptance. M01 adds server simulation tick duration; M02 adds gameplay traffic measurements.

- [ ] Import Poly Haven CC0 materials correctly into HDRP Terrain Lit/Lit and sky setup.
- [ ] Authored small coastal hills + sea + woodland + airstrip; bind burning/char states to visual material swaps.
- [ ] VFX Graph fire/smoke/sparks/water/steam driven by server states, with LOD/quality presets.

**Accept:** Styled playable scene + material/source credits, GPU frame capture under representative effects.

## M05 — Ground forces / first usable hose

- [ ] Rigged player/firefighter, driving fire engine, pump/tank/water supply, nozzle + hose constraint.
- [ ] Ground water action validated on server, fire grid responds; multiplayer co-op with aircraft.

**Accept:** Ground and aircraft users suppress same fire, correctly debit resources.

## M05A — Player Experience, Controls, UI/UX & Settings

**Status: Pending. Do not implement ahead of the active milestone.** [Complete specification/dependencies](docs/PLAYER_EXPERIENCE_SPEC.md), from [the supplied prompt](prompts/FIRELINE_ADRIATIC_M05A_Player_Experience.md). Integrates actual M01–M05 systems after M05; earlier basic test controls remain allowed. M06 server producers unblock the final operations screen bindings; full M05A and player-facing M06 acceptance require both sides.

- [ ] First-person/optional third-person movement, contextual interactions, equipment/inventory, tool pickup/carry/use, vehicle entry/exit, trucks/pumps/hoses, aircraft/cockpit controls and real assignment/radio interfaces.
- [ ] Unity Input System contexts for on-foot, flight, vehicles, equipment, tactical map/command and UI; keyboard/mouse/controller, saved rebinding, sensitivity/dead zones, inversion, hold/toggle, future HOTAS/joystick profiles and server-permitted simplified/advanced flight.
- [ ] Functional Main Menu, Server Browser, Connection/Lobby, Unit/Duty, Profile, Aircraft Selection/Assignment, Equipment/Vehicle Interaction, Operational Map, Incident Information, Aircraft/Cockpit HUD, Firefighter HUD, Dispatch/Command, AAR and Settings with a consistent responsive design.
- [ ] Persistent Low/Performance, Medium/Balanced, High, Ultra and Custom graphics settings; supported display/FPS/VSync/render-scale/texture/shadow/vegetation/terrain/fog/smoke/reflection/AA/upscaling effects, measured mappings and authoritative smoke visibility parity.
- [ ] Persistent audio/radio/microphone, controls and gameplay/accessibility settings: HUD, subtitles, scaling, colorblind overlays, shake, assistance, units and hints.
- [ ] Playable movement/interactions/tools, pumps/hoses, driving, aircraft startup/flight, scoop/drop and dispatch/map tutorials using normal simulation/validation paths.
- [ ] Document controls/contexts, player flow, UI hierarchy, interaction states, settings/presets and tutorials with editable diagram sources and inspected embedded SVG exports.
- [ ] Verify normal-control completion of an actual operational loop without debug commands, server-validated multiplayer, saved/reloaded bindings/settings, real setting effects, common-resolution/accessibility navigation, actual screenshots/UI comparisons and reference-hardware performance.

**Accept:** All required systems/bindings/evidence work against actual gameplay. M06-dependent screens/training remain open until real services exist. Placeholder interfaces and arbitrary preset values do not count. M09 uses this finished experience for final qualification.

## M06 — Operations and regional incidents

**Dependencies:** M01–M05 services and M05A command/UI contracts. Final player-facing acceptance includes actual M05A operations bindings; server work can unblock them without declaring M05A complete prematurely.

- [ ] Zemunik staging, fictional units, in-game qualifications, commander, dispatcher and role leases.
- [ ] Procedural incident creation and multiple simultaneous active fires.
- [ ] AAR, useful contribution data, shift summaries, optional training mode.

**Accept:** Dispatch two incidents, assign units and complete one independently without world reset.

## M07 — PostgreSQL and permanent world

- [ ] Schema migrations, player/unit permissions, world/incident/weather snapshots.
- [ ] Backups, crash recovery, idempotent water/drop events, admin audit.

**Accept:** Kill server, restart and restore burns, players and active incident exactly once.

## M08 — Region streaming & 100-user scalability

- [ ] **NET-01:** reproduce/fix the observed cold and jittered connection startup stalls with UTP/handshake diagnostics, bounded startup failure/retry behavior and the original failing network conditions; qualify again for public hosting in M09. M02 constant-delay/loss replication passed; jittered startup did not.
- [ ] Replace/qualify the bounded reliable-RPC prototype with relevant/adaptive production state delivery; measure traffic and head-of-line effects under representative load.

- [ ] Validate dynamic/adversarial active-sector membership scheduling and refine the prototype budget/estimated fuel-weather calibration against documented reference scenarios; do not extend fixed-membership fixture claims to production interest/load behavior.

- [ ] Geographic compression and streaming sectors, floating origin if necessary.
- [ ] Relevancy interest management and adaptive network updates.
- [ ] Load tests 4/16/32/64/100 **realistic** simulated clients and multiple fires/9 aircraft.

**Accept:** Verified benchmark results with server hardware, tick p95, traffic and RAM; no '100-player ready' claim if failing.

## M09 — Modding, host packaging, 60 FPS and release

- [ ] **Deferred at user request:** resume FL-01–FL-05 and M02 FL-06 attribution and only then optimize with equivalent BEFORE/AFTER measurements. Continue independent milestones; deferred optimization does not waive final performance acceptance.
- [ ] Collect representative 1080p baseline and five-minute runs, CPU/GPU execution counters, draw calls, VRAM, server tick duration and network bandwidth.
- [ ] Add missing VRAM/residency, GC, render-thread/triangle counters and actual dedicated-server tick-duration/transport-byte measurements; populate representative scenarios as their licensed assets/gameplay dependencies become available. Current proxies and clock tick counts cannot substitute for those gates.

**Dependencies:** Completed M05A controls/UI/settings/tutorials and M06–M08 operations/persistence/scale evidence. M09 qualifies and packages these systems rather than substituting release placeholders.

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
