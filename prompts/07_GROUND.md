# Codex Session 07 — Ground operations as real cooperative gameplay

Implement a limited firefighter role and one functional fire engine with hose/pump/nozzle using approved licensed model and rig. Do not count a decorative hose texture as functional hose. Needs drive, park, pump start, attach hose, water tank depletion, pressure/flow budget, nozzle spray modes, server-authorized water coverage and actual changes to authoritative fire cells. Ground player can exit vehicle, deploy tool, wet perimeter and defend a small structure. Add first/third-person animations and a clear equipment status HUD; visuals may use local particles but not locally decide suppression.

Test two players: one pilot performs an aerial drop, one firefighter applies hose water; both observe same fire reduction and water inventories. Avoid massive firefighter squad implementation in this milestone. Optimize hose mesh via spline visuals and bounded segment count, not fully physically simulated fluid.


## Completion contract

- Work only on this milestone and its direct prerequisites; don't attempt the full game in a single session.
- Preserve existing project/configuration, inspect current files and Git state first.
- Build and run actual relevant checks; save commands, logs, screenshots and deterministic test evidence.
- Update `TASKS.md`, `docs/DEVELOPMENT_STATUS.md`, appropriate design/asset/license docs.
- Commit and push verified milestone to configured private Git remote (do not create a new repo if it exists; never force-push). If not authenticated/configured, report precise blocker.
- Summarize implemented, tested, untested, missing assets, remaining work and commit ID. Do not claim success without tests.
