# Codex Session 08 — Zemunik, units, incident command & AAR

Implement a small but functional Zemunik base/station with fictional aerial squadron and ground units. Add server-side unit membership, in-game aircraft/vehicle qualification permissions, role allocation, vehicle lease, dispatcher board, command map, one or two simultaneous incidents, assignment/reassignment, readiness/refill, and After Action Report with *actual meaningful contribution* metrics (effective suppression, response, protected areas) rather than raw water volume/time.

Server must authorize assignment, leases, role qualifications and one of max 9 operational aircraft per server. A 10th operational aircraft must be rejected atomically. Support one player disconnect/return scenario. Dispatch missions are continuously generated from weather and incident rules and do not reset world when one is resolved. Test integrated air/ground incident from standby to AAR and back.


## Completion contract

- Work only on this milestone and its direct prerequisites; don't attempt the full game in a single session.
- Preserve existing project/configuration, inspect current files and Git state first.
- Build and run actual relevant checks; save commands, logs, screenshots and deterministic test evidence.
- Update `TASKS.md`, `docs/DEVELOPMENT_STATUS.md`, appropriate design/asset/license docs.
- Commit and push verified milestone to configured private Git remote (do not create a new repo if it exists; never force-push). If not authenticated/configured, report precise blocker.
- Summarize implemented, tested, untested, missing assets, remaining work and commit ID. Do not claim success without tests.
