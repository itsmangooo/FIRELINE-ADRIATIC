# FIRELINE: ADRIATIC

## Project Overview

**Status:** M00 Unity foundation in progress. The existing HDRP project now has modular C# assemblies, direct-IP client/server startup, foundation tests and build entry points. Flight, fire, wind, water, dispatch and persistence remain planned. The original documentation pack and project guidance are preserved; proposed mechanics and numeric budgets remain design targets pending testing.

A persistent, self-hosted, cooperative Croatian/Herzegovinian aerial and ground firefighting simulator. Unity 6 HDRP, C#, up to 100 players per server (target, **not demonstrated**), up to 9 aircraft (design cap), server-authoritative fire/wind/water/combustion, modding, 60 FPS client target.

## Current Development Status

Unity **6000.6.7f1** / HDRP **17.6.0** are retained. Netcode for Entities, Entities and Transport **6.6.0** are resolved. **15 EditMode tests pass.** Windows release client and dedicated-server builds succeeded with zero errors; one headless server and two distinct rendered clients passed local connection smoke, each exiting with code 0. The server includes no serialized HDRP assets or project materials. See the [M00 evidence report](docs/reports/2026-10-22-m00-foundation.md). M00 remains in progress for assets and representative profiling; M01–M09 remain pending.

## Latest Development Updates

- **2026-10-22 — Frame-loss locations recorded; optimization deferred:** marked all 896 diagnostic intervals, including the 83.47 ms hitch at sample #2, and separated source suspects from proven findings. Added rendered SVG timeline/flowchart and permanent SVG/math formatting rules. [Investigation register](docs/performance/FRAME_LOSS_REGISTER.md).
- **2026-10-22 — M00 foundation verified increment:** six assembly boundaries, direct-IP bootstrap, 15 passing tests, two successful Windows release builds and an accepted headless-server/two-rendered-client smoke. Added actual captures and limited calibration diagnostics. [Implementation, commands and verification](docs/reports/2026-10-22-m00-foundation.md).
- **2026-10-22 — Permanent documentation workflow:** added project-wide evidence rules, README showcase sections, visual/physics/gameplay/performance templates, a changelog and performance history. Existing Unity content and configuration were checkpointed before documentation edits. No game feature was added. See the [workflow report](docs/reports/2026-10-22-documentation-workflow.md) and [changelog](docs/CHANGELOG.md).

## Visual Progress — Before & After

M00 adds a separate calibration scene containing generated boxes and a neutral HDRP material. The starter scene remains intact. These are actual Unity captures; camera/workload differences mean they are not a matched performance comparison. [Capture conditions and provenance](docs/previews/m00-foundation/COMPARISON.md).

| BEFORE — starter Game View | AFTER — Windows release client |
|---|---|
| ![Starter Game View](docs/previews/m00-foundation/starter_before.png) | ![M00 release client](docs/previews/m00-foundation/foundation_after.png) |

Capture reports and compressed images belong in [docs/previews/](docs/previews/README.md). The [visual report template](docs/templates/VISUAL_CHANGE.md) records revisions, capture conditions, assets and measured or unmeasured performance impact.

## Physics & Simulation Improvements

The [M00 fixed tick clock comparison](docs/physics/foundation-clock/COMPARISON.md) documents the pure scheduling utility, its equations and unit assertions. Flight/fire/wind/water physics remain unimplemented. [Simulation specification](docs/SIMULATION_SPEC.md) and [aircraft specifications](docs/AIRCRAFT_SPECIFICATIONS.md) describe proposed models. Implementation comparisons belong in [docs/physics/](docs/physics/README.md).

## Gameplay Systems

Dispatch, aerial sorties, ground operations, persistent incidents and cooperative suppression are **planned**, not playable systems. See [game design](docs/GAME_DESIGN.md). Each implemented change will summarize player impact here and link a [gameplay/architecture report](docs/templates/GAMEPLAY_ARCHITECTURE_CHANGE.md) covering behavior, authority and tests.

## Architecture & Technical Diagrams

[M00's implemented architecture](docs/reports/2026-10-22-m00-foundation.md) separates Client, Server, Shared, Simulation, Authoring and Tests. Role-specific builds use separate scenes and assembly constraints; the broader [technical architecture](docs/TECHNICAL_ARCHITECTURE.md), [network protocol](docs/NETWORK_ARCHITECTURE.md) and [gameplay diagrams](docs/DIAGRAMS.md) remain proposed beyond that foundation. The documentation workflow follows this process:

```mermaid
flowchart LR
    A[Inspect and checkpoint] --> B[Capture applicable baseline]
    B --> C[Implement and verify]
    C --> D[Collect applicable evidence]
    D --> E[Update showcase and status]
    E --> F{Required evidence complete?}
    F -->|Yes| G[Commit and push verified milestone]
    F -->|No| H[Track gaps and keep milestone open]
    H --> D
```

## Performance Benchmarks

The initial **720p High Fidelity calibration diagnostic** on an i5-9400F / GTX 1660 SUPER averaged **59.73 FPS**, with **38.23 FPS 1% low** and **17.36 ms p95 frame interval** over 15 seconds while two clients ran. [Raw data, method and limits](docs/performance/2026-10-22-m00-calibration.md). This is not representative 1080p/60 FPS acceptance or an optimization comparison. No 100-player capacity is demonstrated. See [performance plan](docs/PERFORMANCE_60FPS.md) and [history](docs/PERFORMANCE_HISTORY.md).

The [frame-loss register](docs/performance/FRAME_LOSS_REGISTER.md) marks the three intervals above 20 ms, source locations to investigate and deferred attribution tasks. Causes remain unproven; optimization is deferred. Its [rendered vector timeline](docs/performance/m00-frame-loss/frame-interval-timeline.svg) uses the existing data, with no new benchmark claim.

## Development Roadmap

| Milestone | Planned outcome | Status |
|---|---|---|
| M00 | Unity/tooling audit, assemblies, build and benchmark foundation | In Progress |
| M01 | Authoritative fire and wind | Pending |
| M02 | Multi-process replication and persistence proof | Pending |
| M03 | CL-415 flight and water physics | Pending |
| M04 | HDRP coastal art and VFX | Pending |
| M05 | Ground crews and working hose | Pending |
| M06 | Operations and regional incidents | Pending |
| M07 | PostgreSQL and permanent world | Pending |
| M08 | Streaming and measured 100-user scalability | Pending |
| M09 | Modding, hosting, performance and release | Pending |

See [TASKS.md](TASKS.md) for dependencies and acceptance criteria. Every milestone must follow [AGENTS.md](AGENTS.md) and the [documentation workflow](docs/DOCUMENTATION_WORKFLOW.md), with missing evidence tracked before completion.

## Usage with existing Unity project

The instructions below are retained for the original development pack. This repository already contains the Unity project; continue in place without re-extracting or recreating it.

1. Back up or commit the Unity project. Extract this pack **into the project root** (next to `Assets/`, `Packages/`, `ProjectSettings/`). Merge existing `AGENTS.md`/`README.md` rather than blindly overwriting them.
2. Open `docs/GAME_VISION.md` and `docs/GAME_DESIGN.md` first. Read the architectural constraints in `docs/TECHNICAL_ARCHITECTURE.md`, `docs/NETWORK_ARCHITECTURE.md` and `docs/PERFORMANCE_60FPS.md`.
3. Use `prompts/00_KICKOFF.md` as the **first Codex message**. It tells Codex to inspect and document the current project, not recreate it.
4. Use one subsequent prompt per implementation stage. Check actual build/test evidence, commit, push, update `TASKS.md` and `docs/DEVELOPMENT_STATUS.md`.
5. Download licensed assets from the verified/flagged links in `docs/ASSET_MANIFEST.md`; asset files are deliberately not included in this archive.

## Core docs

| Document | Purpose |
|---|---|
| `docs/GAME_VISION.md` | World, audience, game pillars and success criteria |
| `docs/GAME_DESIGN.md` | Player roles, incidents, gameplay flows, station/base operation, scoring |
| `docs/TECHNICAL_ARCHITECTURE.md` | Unity package decisions, boundaries, server/client contracts |
| `docs/SIMULATION_SPEC.md` | Server fire, wind, drops, water/vegetation and deterministic testing |
| `docs/AIRCRAFT_SPECIFICATIONS.md` | Aircraft reference data and calibrated physics approach |
| `docs/ASSET_MANIFEST.md` | Asset URLs, source download workflow, statuses, formats |
| `docs/ASSET_LICENSES.md` | Licensing obligations and audit checklist |
| `docs/ART_DIRECTION.md` | Croatian environment, HDRP rendering, VFX and asset consistency |
| `docs/NETWORK_ARCHITECTURE.md` | 100-player target, interest management, synchronization, 9 aircraft |
| `docs/DEDICATED_SERVER.md` | Self-hosting, persistence, security, server operations |
| `docs/DATABASE_SCHEMA.md` | PostgreSQL entity model, example schema and ER diagram |
| `docs/MODDING_SPEC.md` | Safe, versioned community content |
| `docs/PERFORMANCE_60FPS.md` | Client GPU/CPU frame budget, presets, QA and stress plan |
| `docs/MARKETING_PLAN.md` | Naming, positioning, community, release communications |
| `docs/DIAGRAMS.md` | Mermaid gameplay and system flows |
| `docs/DEVELOPMENT_STATUS.md` | Honest status and implementation handoff |
| [Documentation workflow](docs/DOCUMENTATION_WORKFLOW.md) | Permanent evidence requirements and publishing gate |
| [Changelog](docs/CHANGELOG.md) | Chronological development updates |
| [Performance history](docs/PERFORMANCE_HISTORY.md) | Measured runs and explicitly outstanding benchmarks |
| [Report templates](docs/templates/README.md) | Visual, physics, gameplay/architecture and performance reports |
| `TASKS.md` | Ordered milestones and objective acceptance criteria |
| `AGENTS.md` | Rules for Codex / AI coding agents |
| `prompts/*.md` | Copy-ready Codex instructions per milestone |

## Operating principles

- No Unity project recreation; don't delete existing code or modify project version without approval.
- Fire **state** and wind and water impact are server-owned. Client-only flame/smoke/spark/water particles may differ, but smoke density used for gameplay and ignitions must come from server state.
- Real geography can be **regionally compressed** for travel; local runways, physics, aircraft, firefighting geometry remain in meters at correct scale.
- HDRP is for visually capable Windows clients; Linux/Windows dedicated servers are **headless**.
- 60 FPS means measured **≤16.67 ms/frame at 1080p on named hardware**, not 60 FPS in an empty Unity Editor scene.
- 100-player concurrency is a *hypothesis* until realistic network, physics, wildfire, and bandwidth load tests pass.
- Community servers must remain independently hostable; avoid mandatory proprietary cloud dependencies.

## Authoritative source links

- De Havilland CL-415: https://dehavilland.com/aerial-firefighters/
- Air Tractor AT-802F: https://airtractor.com/wp-content/uploads/2022/09/Air-Tractor_802F-Brochure_2022.pdf
- Poly Haven CC0: https://polyhaven.com/license
- Unity Dedicated Server guidance: https://docs.unity3d.com/2022.3/Documentation/Manual/dedicated-server-introduction.html
- Unity 100+ multiplayer sample (URP, not HDRP): https://unity.com/demos/megacity-competitive-action-sample
- Unity profiler guidance: https://docs.unity3d.com/2022.2/Documentation/Manual/profiler-profiling-applications.html
