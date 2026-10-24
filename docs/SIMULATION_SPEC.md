# Simulation specification — Fire, wind, water and environmental destruction

## M03 debug runway-contact boundary — 2026-10-24

The optional pure contact adapter now adds finite-plane three-point support, combined friction/braking/steering and current/candidate envelope validation to the unchanged flight core. All gear values are uncalibrated estimates independent of the static imported mesh. [Actual equations, SI constants, comparisons, raw evidence and limitations](physics/runway-contact/COMPARISON.md). It is verified in an isolated dedicated-player harness, not installed live flight/contact, general airframe/terrain collision or pilot/network gameplay. Historical paragraphs below describe their original increments.

## M03 numerical flight increment — 2026-10-24

An isolated pure C# free-body flight solver now exists; [actual equations, SI units, estimated constants, diagrams and numerical checks](physics/debug-flight/COMPARISON.md). It evaluates explicit wind samples and is exercised with the existing seeded `WindField` in a dedicated debug proof. No live shared aircraft/fire clock, water trajectory/coverage, scoop, fuel burn or collision integration is implemented. M02 already verifies live fire replication/local restart; the historical M01 scope paragraph below describes that earlier checkpoint.

## Current M01 prototype — implementation boundary

The [implementation comparison](physics/fire-wind/COMPARISON.md) records the actual bounded 5 Hz synthetic-grid model, seeded horizontal wind, six named phases, normalized heat/intensity/hazard, SI fuel/water and irreversible vegetation char. Its estimated coefficients are not a calibrated field forecast or the full proposed fidelity below. M01 tests and headless runs establish deterministic gameplay behavior; aircraft/drop physics, real map descriptors, refined thermodynamics, networking and persistence remain later work. The kickoff phrase “7 burn stages” conflicts with the six explicitly named states below; implementation follows those six names rather than inventing another phase.

## Absolute authority invariant

**Dedicated server calculates the state of all gameplay-affecting fire, wind, water coverage, structures and vegetation.** Clients can render different individual sparks and flames but must show identical underlying burning/extinguished/charred state, fire perimeter and gameplay smoke density (allowing network latency and visual interpolation). Client smoke graphics do not determine gameplay visibility.

## Fixed-step world state

Each terrain chunk has stable ID `(sector, chunkX, chunkY)`, revision, seed, and an array/grid of cells with stable IDs. Each cell includes:

`fuelType`, `fuelKgPerM2` (estimated), `remainingFuel`, `fuelMoisture`, `surfaceWetness`, `temperatureProxy`, `fireIntensity`, `state=UNBURNED|HEATING|BURNING|SMOLDERING|EXTINGUISHED|CHARRED`, `elevation`, `slope`, `vegetationIds`, `lastChangedTick`.

Use a calibrated *semi-empirical wildfire spread model*, not an uncalibrated visual cellular automaton presented as scientifically exact. Model anisotropic spread rates from fuel, terrain slope, wind and moisture. Store *perimeter/front* separately for client overlays. Keep the number of burning cells bounded by sector budget and sim-Lod to allow 100 players.

## Wind field

`V_w(x,y,z,t)` is a vector in **m/s** with direction, gust components and coarse orography adjustments. Keep server time/seed and field revision; client receives baseline/updates for interpolated wind socks, tree motion, smoke direction and airplane feel. **Wind used for drop physics and fire spread is server state, not client weather particles.** When wind turns, the new state is applied to all active chunks at the same simulation tick and broadcast with revision IDs.

## Combustion / burn stage

```text
UNBURNED -> HEATING -> BURNING -> SMOLDERING -> CHARRED
                      |        |
                      +--> EXTINGUISHED --(fuel+dry+heat)--> HEATING
```

Fire intensity not equal to animation intensity. Transitions require server-owned fuel, heat, moisture and suppression thresholds. Tree IDs bind to vegetation lifecycle: living mesh/material -> on-fire visuals -> charred/optional fallen mesh. Destroyed/collidable geometry changes are authoritative and saved; physical collapse can be approximated with scripted states. Late join must use saved ID-state mappings and reinstantiate charred meshes.

## Server smoke model

Store coarse `smokeOpacity/visibilityHazard` field near significant burning fronts and the wind field. Clients draw VFX Graph particles and optional volumes to match server opacity envelope. Gameplay radio/vision hazard uses the coarse server field. Fine camera-based turbulence is cosmetic.

## Water from aircraft (solver)

After a valid water release, create e.g. **32–512 representative water parcels** depending on adaptive quality. Each parcel has mass, initial position, initial velocity (aircraft linear velocity + release-nozzle velocity + rotation-induced local offset), aerodynamic effective area and drag; simulate with substeps until intersection with terrain. Equation:

`dv/dt = g + 0.5 * rho_air * Cd * A / m * |V_wind - v| * (V_wind - v)`

`dx/dt = v`

The real breakup of a large water sheet into droplets is complex; use calibrated effective parcel parameters and dispersion spread. The mass-weighted sum across parcels must match released mass minus explicitly modeled evaporation/loss. Convert impacts to rasterized coverage `kg/m²` (and absorbed/evaporated ratios) on fire cells. This footprint, not cosmetic splash VFX, affects the burn grid.

Explicitly include aircraft pitch/roll, release door positions, water tank emptying curve, altitude, airspeed and gusts. The tank mass changes over the same release interval so aircraft mass/center of gravity update smoothly. Distinguish water-payload *litres* from *kg* (water density ~1000 kg/m³ approximation).

## Ground fire tools

A hose/pump subsystem outputs mass flow [kg/s or L/s] and nozzle spread/jet direction; server calculates ballistic arc/range approximations, water supply depletion and coverage. Hose geometry is **not** full Navier-Stokes; solve smooth spline client-side for visuals while enforcing end connection, reach and resource constraints on server. Extinguisher type and hand tools have configurable effective areas and materials.

## Fire/water interaction

- Fire intensity changes as a function of energy removed/wetness, fuel and incoming heat from neighbors. No binary 'drop raycast instantly deletes fire' behavior.
- Water cools, wets, slows spread; inadequate dosing may only reduce intensity; dryness and wind can reignite.
- An aerial drop has a graded footprint; on-edge misses still affect adjacent terrain realistically.
- Ground crews can protect structures by wetting or create containment lines that remove potential fuel.
- Fire spread and useful water coverage share **the same coordinate frame and meter scale**.

## Replication protocol

1. Client subscribes to nearby chunks and operational summaries of relevant distant incidents.
2. Server sends baseline `{chunkID,revision,seed,compressedCellStates,vegetationStates,smokeHazard}`.
3. Server emits delta `{chunkID,prevRevision,nextRevision,changes[]}`; client applies in order; gap => baseline repair.
4. On wind/drop/ignition, server emits versioned high-value events and resulting field changes.
5. Presentation interpolates changed states and spawns local particles deterministically from event seed if desired. Spark identity is not synchronized.
6. On reconnect/restart, baseline reconstructs identical gameplay state and charred vegetation.

Reliable ordered baseline/chunk critical events; transport-appropriate delta handling with sequence/version checks. Do not send all 100 players every fire cell at high frequency.

## Persistence

Server keeps active grid in memory, writes compressed per-chunk checkpoints and append-only meaningful events (`ignition`, `spreadRevision`, `dropApplied`, `vegetationBurned`, `incidentResolved`). Snapshot IDs and events are idempotent; restore most recent checkpoint and replay only non-applied event ranges. Never rely on Unity client particle state for recovery.

## Verification cases (repeatable)

- Calm-air stationary release vs 8 m/s crosswind release: centroid shifts downwind; total parcel mass conserved within tolerances.
- Same release at lower vs higher altitude: impact time and dispersion differ predictably.
- Wind 90° rotation changes downwind fire spread direction; two clients converge on same cell-state digest.
- All test trees burn stage-by-stage, late join sees char and restart restores it.
- Fire suppression has reproducible effect at fixed seed/tick; no client can send a direct 'extinguish this cell' command.
- Checkpoint, inject failure, resume: no duplicated score/vehicle inventory/water drops.

## Important accuracy note

This design is a **gameplay-grade approximation**. Predicting real wildfire spread or certified water drop footprint requires specialized field validation and measurements; no such scientific fidelity is assumed or claimed.
