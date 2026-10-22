# Codex Session 00 — Project audit and game vision first

We are building FIRELINE: ADRIATIC, an original Unity 6 HDRP/C# persistent 100-player-target co-op wildfire response simulator across a compressed Adriatic coast (Zemunik/Zadar operations base, Split, Omiš, Šibenik, islands and Ljubuški). Up to nine operational aircraft; pilots, copilot, ground crews, unit commanders, dispatch and logistics; community self-hosting, modding and persistent incidents. No PvP, no match reset. Air/ground crews share exactly one authoritative fire and weather state.

This archive is our design specification, **not** an implemented Unity game. Read `README.md`, `docs/GAME_VISION.md`, `docs/GAME_DESIGN.md`, `docs/DIAGRAMS.md`, `docs/TECHNICAL_ARCHITECTURE.md`, `docs/SIMULATION_SPEC.md`, `docs/NETWORK_ARCHITECTURE.md`, `docs/PERFORMANCE_60FPS.md`, `docs/ASSET_MANIFEST.md`, `TASKS.md` and `AGENTS.md`.

Inspect the existing Unity project **without recreating it**. Verify installed Unity Editor version, HDRP template setup, build targets, Unity C# compilation, Package Manager / networking compatibility, environment tooling and Git. Do not indiscriminately install everything before validation. Create a report of conflicting requirements and technical risks, including HDRP client + headless server + Netcode for Entities, dynamic weather and 100-player scalability.

Establish assembly and code boundaries and a detailed dependency-aware milestone plan. Preserve existing project assets. Prioritize proof that a headless authoritative server and two clients can share a fire chunk.

**In this session:** do an environment audit, organize docs, create project package/assembly boundary only if safe and justified, and make a tiny CI/build smoke proof if possible. No multi-region terrain, aircraft fleet, large Unity prefab generation, or pretend 100-player support. Record exact versions and verification results; finish by suggesting the next specific coding prompt.


## Completion contract

- Work only on this milestone and its direct prerequisites; don't attempt the full game in a single session.
- Preserve existing project/configuration, inspect current files and Git state first.
- Build and run actual relevant checks; save commands, logs, screenshots and deterministic test evidence.
- Update `TASKS.md`, `docs/DEVELOPMENT_STATUS.md`, appropriate design/asset/license docs.
- Commit and push verified milestone to configured private Git remote (do not create a new repo if it exists; never force-push). If not authenticated/configured, report precise blocker.
- Summarize implemented, tested, untested, missing assets, remaining work and commit ID. Do not claim success without tests.
