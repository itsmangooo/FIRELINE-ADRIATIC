# Codex Session 10 — 100-player target, regional streaming and measurement

Read `docs/NETWORK_ARCHITECTURE.md`, `docs/PERFORMANCE_60FPS.md`, `docs/DEDICATED_SERVER.md`.

Implement sector subscriptions, command summary channels, aircraft corridor prefetch, versioned fire chunk updates, adaptive vehicle snapshot rates and network per-client bandwidth budgets. Avoid sending distant active fire cells/vehicles at local full rate. Headless server must not load HDRP client VFX. Build a realistic automated client-load runner including 9 aircraft, several fire zones, ground units/vehicle interactions and snapshot traffic.

Execute 4,16,32,64 and 100-client stages where infrastructure permits. Record machine specs, main/server CPU, sim step p95/p99, bandwidth/client and total, memory, packet loss, state-digest consistency. Produce honest PASS/FAIL report with capacity limit; 100 only considered supported after representative, documented success. Run client FPS benchmarks separately on named GPU/quality preset under smoke/fire load.


## Completion contract

- Work only on this milestone and its direct prerequisites; don't attempt the full game in a single session.
- Preserve existing project/configuration, inspect current files and Git state first.
- Build and run actual relevant checks; save commands, logs, screenshots and deterministic test evidence.
- Update `TASKS.md`, `docs/DEVELOPMENT_STATUS.md`, appropriate design/asset/license docs.
- Commit and push verified milestone to configured private Git remote (do not create a new repo if it exists; never force-push). If not authenticated/configured, report precise blocker.
- Summarize implemented, tested, untested, missing assets, remaining work and commit ID. Do not claim success without tests.
