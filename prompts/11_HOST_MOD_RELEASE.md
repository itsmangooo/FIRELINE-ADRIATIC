# Codex Session 11 — Community dedicated server, modding & release preparation

Package working Windows/Linux headless self-hosted server with example config, environment secrets, firewall/port docs, moderation, whitelist, auth, health/shutdown/backups and stable restart. Add versioned data-only mod definitions, Unity AssetBundle visual packages, dependency manifests, license auditing and client/server mod fingerprints. No arbitrary user C# execution on server by default. If Steam Workshop isn't integrated, note it as future only; keep direct IP hosting functional.

Create truthful user-facing game/manual: player role onboarding, controls, unit/qualification requests, dispatch assignment, aircraft and ground gameplay, AAR, server joining/hosting, mods. Generate marketing plan / Steam description / trailer shot list from actual implemented features, with no unverified 100-player or 60 FPS claim. Execute regression and permission tests.


## Completion contract

- Work only on this milestone and its direct prerequisites; don't attempt the full game in a single session.
- Preserve existing project/configuration, inspect current files and Git state first.
- Build and run actual relevant checks; save commands, logs, screenshots and deterministic test evidence.
- Update `TASKS.md`, `docs/DEVELOPMENT_STATUS.md`, appropriate design/asset/license docs.
- Commit and push verified milestone to configured private Git remote (do not create a new repo if it exists; never force-push). If not authenticated/configured, report precise blocker.
- Summarize implemented, tested, untested, missing assets, remaining work and commit ID. Do not claim success without tests.
