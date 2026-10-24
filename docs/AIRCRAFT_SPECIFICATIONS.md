# Aircraft specifications & flight-simulation data sheet

## Current M03 engineering boundary — 2026-10-24

The estimated dry server aircraft now has live seat/input/pose authority and a separate client engineering view with test keyboard/gamepad mapping, two external cameras, explicit debug CoG/model offsets, 100 ms cosmetic interpolation and telemetry. [Actual behavior, equations, screenshots, device/render evidence and limits](previews/m03-flight-view/COMPARISON.md). The existing flight/contact constants are unchanged and are **not the production CL-415 baseline below**. Real CoG/gear rig, movable parts, cockpit, calibrated aerodynamic/fuel/resource behavior, airborne route, full collision and water acceptance remain pending. Earlier scope paragraphs below are historical.

## M03 debug contact prerequisite — 2026-10-24

The optional pure contact adapter now adds finite-plane three-point support, combined friction/braking/steering and current/candidate envelope validation to the unchanged flight core. All gear values are uncalibrated estimates independent of the static imported mesh. [Actual equations, SI constants, comparisons, raw evidence and limitations](physics/runway-contact/COMPARISON.md). It is verified in an isolated dedicated-player harness, not installed live flight/contact, general airframe/terrain collision or pilot/network gameplay. Historical paragraphs below describe their original increments.

## Implemented M03 debug boundary — 2026-10-24

`DEBUG_FLIGHT_ONLY_V1` now implements an isolated numerical 6DOF core with synthetic 10000 kg dry body, 4000 kg water limit and estimated aerodynamic/inertia/thrust constants. **It is not the CL-415 baseline below** and does not blend aircraft variants or claim manufacturer performance. [All actual constants, equations and tests](physics/debug-flight/COMPARISON.md), [real headless build/process evidence](reports/2026-10-24-m03-debug-flight.md). Production variant JSON, licensed model, calibration, fuel consumption, live pilot authority/contact/replication and flight loop remain open M03 work.

## Reference policy

Model **CL-415** first. Published specifications are references, not a complete aerodynamic model. Every value is tagged `SOURCE_VERIFIED`, `APPROX`, `CALIBRATE` or `UNKNOWN` in the actual JSON aircraft definition. Different CL-415 variants can have different operating weights/capacity; do not blend data from the CL-415EAF conversion with Croatian operational CL-415 without documentation.

| Parameter | CL-415 baseline (initial) | Status / reference |
|---|---:|---|
| Water capacity | **6,136 L** | Published by De Havilland, https://dehavilland.com/aerial-firefighters/ |
| Aircraft type | amphibious twin-engine firefighting aircraft | verified manufacturer description |
| Wingspan | approximately **28.6 m** | approximate public reference; verify variant and primary sheet |
| Overall length | approximately **19.8 m** | approximate public reference; verify variant and primary sheet |
| Wing area | approximately **100 m²** | preliminary reference; confirm in aviation reference |
| Engine / power curves | TBD | primary performance charts needed |
| Empty / takeoff / scoop weights | TBD (variant-specific) | do not invent exact figures |
| Aerodynamic curves (CL(alpha), CD(alpha), CM(alpha)) | TBD | calibrate with flight tests |
| CoG and inertia tensor | TBD | estimate from mass distribution and tune |

**Secondary aircraft:** Air Tractor AT-802F / float-equipped Fire Boss (distinct variants).

- AT-802F nominal tank ~800 US gal (= approximately **3,028 L**), verified from official Air Tractor brochure: https://airtractor.com/wp-content/uploads/2022/09/Air-Tractor_802F-Brochure_2022.pdf
- Fire Boss with floats can scoop; land-based AT-802F must be ground filled. Official Air Tractor article reports Fire Boss scooping ~3,028 L in **12–15 s** in appropriate conditions: https://airtractor.com/news/2014/08/at-802f-field-report/
- Never equate AT-802F / AT-802A / Fire Boss handling; separate configs.

## Required aircraft data model

`identifier, variant, mesh/prefabId, massEmptyKg, maxTakeoffKg, fuelCapacityKg, fuelBurnKgPerSecCurve, waterCapacityLiters, cargoLoadKg, cgOffset, inertiaTensor, wingspanM, wingAreaM2, fuselageLengthM, flapConfig, liftDragCoeffVsAoA, stallAoA, thrustVsThrottleAltitudeAirspeed, controlResponseCurves, gearDrag, waterScoopProfile, waterReleaseOutlets, instrumentIds, inputProfile`.

Use SI: m, kg, seconds, Newton, m/s, radians internally; UI may display feet/knots when desired. Add validation rejecting negative mass, impossible max payload and missing curves.

## Physics

Treat rigid body as 6DOF mass + inertia under gravity, prop thrust and distributed aerodynamic surfaces. Compute relative air velocity `V_air = V_world - V_wind`; calculate dynamic pressure `q = 0.5*rho*|V_air|²`; lift `L=q*S*C_L(alpha,flaps)`, drag `D=q*S*C_D(alpha,flaps)` and pitch/yaw/roll aerodynamic moments. Water mass changes gravitational force and CoG/inertia at each loading/release step.

Unity Rigidbody can integrate linear/angular velocity but should not substitute automatic air resistance for proper forces. Audit FixedUpdate timestep choice and physics stability; all authoritative flight impacts/tanks belong to server. Clients predict/interpolate only after tests establish divergence correction strategy.

## Interaction design

- Pilot: control surfaces and throttle, engine, gear, flaps, wheel brakes, trim, water doors, scoop deployment.
- Copilot (CL-415): navigation, radio, checklists, water monitoring or other meaningful tasks; controls use one server authoritative cockpit state to prevent conflicting commands.
- Preflight: checklist / training simplification; meaningful consequences for skipped operations only when realism setting enabled.
- Scoop: on suitable sea surface, enforce speed, pitch, depth/level, loading, mass change; log invalid scoop.
- Drop: based on outlet geometry, aircraft linear/angular state and wind solver, then derive fire impact.

## Validation matrix

- Ground still, takeoff roll, climb/cruise, turns, approach, touchdown.
- Dry vs full water mass: stall speed trends, climb performance and turn radius should change plausibly.
- 10 m/s head/cross/tail winds; gust recovery and compass/radio wind information.
- Unexpected low-altitude or failed scoop must be handled without runaway Rigidbody/NaN.
- Flight model tuned against public curves *where available*; document deviation rather than claim study-level precision.
