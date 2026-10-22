# Codex Session 02 — Server-authoritative fire and wind simulation

Implement a small, headless C# wildfire/wind simulation from `docs/SIMULATION_SPEC.md`, `docs/TECHNICAL_ARCHITECTURE.md`, `TASKS.md`.

- Bounded sector/chunk fire-cell map with indexed stable cell IDs, fuel, moisture, wetness, thermal proxy, fire intensity, 7 burn stages and charred vegetation state.
- Fixed-step server simulation using seeded spread model with wind direction/speed/gust, fuel and slope. Define smoke-opacity gameplay proxy based on server intensity.
- Injectable wind field drives fire and later aviation; document m/s and coordinate conventions.
- Manual server-authorized water coverage event reduces intensity/wets cells (no aircraft yet).
- Make it testable without rendering or requiring a populated scene; add finite-value, mass/resource, bounded CPU and regression tests.
- Expose versioned snapshots/checksums and per-chunk deltas as data types ready for networking; persist-burn serialization interfaces.

Use simple data test grid, not fake realistic map. Provide deterministic test runs: wind rotation changes spread distribution, extinguished cells transition correctly, charred cells never reset unintentionally, invalid client command rejected at API boundary. No VFX Graph simulation determines fire logic.


## Completion contract

- Work only on this milestone and its direct prerequisites; don't attempt the full game in a single session.
- Preserve existing project/configuration, inspect current files and Git state first.
- Build and run actual relevant checks; save commands, logs, screenshots and deterministic test evidence.
- Update `TASKS.md`, `docs/DEVELOPMENT_STATUS.md`, appropriate design/asset/license docs.
- Commit and push verified milestone to configured private Git remote (do not create a new repo if it exists; never force-push). If not authenticated/configured, report precise blocker.
- Summarize implemented, tested, untested, missing assets, remaining work and commit ID. Do not claim success without tests.
