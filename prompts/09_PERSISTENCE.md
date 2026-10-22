# Codex Session 09 — Persistent world, PostgreSQL and crash safety

Implement versioned PostgreSQL schema migrations from `docs/DATABASE_SCHEMA.md`, with validated minimal schemas for players, units, memberships/qualifications, incident states/events, air/ground assignments, authoritative fire chunk snapshots, weather, vehicles, after-action report and audit. Keep fire ticks in memory; save consistent compressed checkpoints plus important append-only events, with unique IDs to avoid duplicate drops/inventory changes on replay. Use environment-sourced DB secrets. Ensure server restart restores same vegetation burn stages and active fire incidents; verify from new clients.

Generate actual ER diagram from implemented tables, `docker-compose` optional **only if requested/available** or simple local PostgreSQL setup, migration tests, backup/restore. Do not expose DB access to clients. Benchmark data write rate under fire load and demonstrate finite pressure on DB.


## Completion contract

- Work only on this milestone and its direct prerequisites; don't attempt the full game in a single session.
- Preserve existing project/configuration, inspect current files and Git state first.
- Build and run actual relevant checks; save commands, logs, screenshots and deterministic test evidence.
- Update `TASKS.md`, `docs/DEVELOPMENT_STATUS.md`, appropriate design/asset/license docs.
- Commit and push verified milestone to configured private Git remote (do not create a new repo if it exists; never force-push). If not authenticated/configured, report precise blocker.
- Summarize implemented, tested, untested, missing assets, remaining work and commit ID. Do not claim success without tests.
