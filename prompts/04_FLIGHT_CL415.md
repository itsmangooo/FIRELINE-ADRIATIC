# Codex Session 04 — CL-415 flight model and first coastal loop

Read `docs/AIRCRAFT_SPECIFICATIONS.md`, `docs/GAME_DESIGN.md`, `docs/ASSET_MANIFEST.md` and flight math portions of `docs/SIMULATION_SPEC.md`.

Using the approved imported CL-415 model (or mark asset blocked and use a distinctly named debug proxy only for isolated physics tests), build a controlled six-degree-of-freedom aircraft prototype: gravity, thrust, lift/drag versus AoA, fuel/water mass and CoG, flaps/control surfaces, stall, gear, wind/turbulence and telemetry. 1 unit = 1 m; velocities in m/s; mass in kg. Use **server authority** for shared flight state and validate pilot inputs; use prediction/interpolation only if actually tested.

Build limited Zemunik-inspired runway + coastal route in a small sector, not all Dalmatia. Support keyboard/controller for basic flights, cockpit/external camera and a readable telemetry overlay. Aircraft must take off, follow a route, turn into/out of wind and land. Compare payload/no-payload behavior; fail safely for NaN/unphysical states. Do not call physics certified or real-aircraft-identical. Record all assumed curve values and aircraft asset limitations (cockpit/separate parts).


## Completion contract

- Work only on this milestone and its direct prerequisites; don't attempt the full game in a single session.
- Preserve existing project/configuration, inspect current files and Git state first.
- Build and run actual relevant checks; save commands, logs, screenshots and deterministic test evidence.
- Update `TASKS.md`, `docs/DEVELOPMENT_STATUS.md`, appropriate design/asset/license docs.
- Commit and push verified milestone to configured private Git remote (do not create a new repo if it exists; never force-push). If not authenticated/configured, report precise blocker.
- Summarize implemented, tested, untested, missing assets, remaining work and commit ID. Do not claim success without tests.
