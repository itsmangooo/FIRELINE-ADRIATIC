# Codex Session 05 — Server water scoop / physically informed release

Read `docs/SIMULATION_SPEC.md` and `docs/AIRCRAFT_SPECIFICATIONS.md`.

Add approved aircraft water tank with scooping gate based on compatible aircraft type, water surface position, speed and attitude, time, tank capacity, mass/CoG updates. Drop solver: authoritative parcel model with start position and aircraft linear/angular velocity, aircraft orientation, release geometry, release rate, gravity, aerodynamic drag, wind vector sampled along trajectory, terrain collision and mass-weighted coverage raster. **Never simply raycast straight down** and delete burning cells.

Expose drop footprint to fire simulation, apply cooling/wetting/intensity changes on server, replicate the outcomes to clients. Client renders release/water spray/splash/steam based on server event and footprint; cosmetic droplets can differ. Test identical seed calms vs 8m/s crosswind and multiple release heights, mass conservation tolerance and exact matching fire results on two clients. Demonstrate from coast to fire and back; list unresolved calibration data.


## Completion contract

- Work only on this milestone and its direct prerequisites; don't attempt the full game in a single session.
- Preserve existing project/configuration, inspect current files and Git state first.
- Build and run actual relevant checks; save commands, logs, screenshots and deterministic test evidence.
- Update `TASKS.md`, `docs/DEVELOPMENT_STATUS.md`, appropriate design/asset/license docs.
- Commit and push verified milestone to configured private Git remote (do not create a new repo if it exists; never force-push). If not authenticated/configured, report precise blocker.
- Summarize implemented, tested, untested, missing assets, remaining work and commit ID. Do not claim success without tests.
