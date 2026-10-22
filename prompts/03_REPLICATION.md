# Codex Session 03 — Real dedicated server, 2+ client fire synchronization

Implement minimal multiplayer according to `docs/NETWORK_ARCHITECTURE.md`, `docs/DEDICATED_SERVER.md` and simulation interfaces.

Create actual headless server build and two real client processes with correct package version. Server runs fire and wind. On join, player subscribes to a chunk and receives authoritative baseline and revision. Server propagates deltas, not visual particles. Implement gap detection and baseline repair, heartbeat and disconnect. Read-only fire visual sample from client; do not allow clients to mutate fire cells. Include packet latency/loss tests with instrumentation.

Acceptance demonstration:
1. Ignite tree/brush on server. Both clients show same burn stage IDs.
2. Change wind on server. Same spread states emerge for both clients.
3. Apply server-side suppression. Both receive same outcome.
4. Start client #3 later; see existing charred tree.
5. Restart server with saved state; see same burned cell states. If persistence not yet implemented, mark #5 pending instead of simulating it, and implement a minimal checkpoint if possible.

Provide real connection commands, state digest logs and screenshots. Do not represent one Unity editor window as a real multiplayer test.


## Completion contract

- Work only on this milestone and its direct prerequisites; don't attempt the full game in a single session.
- Preserve existing project/configuration, inspect current files and Git state first.
- Build and run actual relevant checks; save commands, logs, screenshots and deterministic test evidence.
- Update `TASKS.md`, `docs/DEVELOPMENT_STATUS.md`, appropriate design/asset/license docs.
- Commit and push verified milestone to configured private Git remote (do not create a new repo if it exists; never force-push). If not authenticated/configured, report precise blocker.
- Summarize implemented, tested, untested, missing assets, remaining work and commit ID. Do not claim success without tests.
