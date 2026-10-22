# Codex Session 06 — HDRP visual pass with 60 FPS performance gates

Read `docs/ART_DIRECTION.md`, `docs/ASSET_MANIFEST.md`, `docs/PERFORMANCE_60FPS.md`.

Use actual approved Poly Haven CC0 materials in one representative Croatian coastal region with an airfield/wooded hill/sea and burning trees. No generic plane-only terrain. Correct HDRP mask packing, alpha/normal/sRGB and real geometry. Build dynamic sky lighting + weather-driven fog and smoke columns. Render local visual sparks, smoke, flame, water and steam in VFX Graph, driven strictly from server state. Include realistic charred tree/ground visuals and daylight/dusk screenshots.

Implement Performance/Balanced/Ultra quality tiers with vegetation instancing/LODs, terrain streaming, shadow ranges, material reuse and smoke overdraw budgets. Set Balanced 1080p/60 FPS **as a measurable target**, not an assumed result. Run **built client** benchmark in S2–S4 representative scenes, log CPU main/render, GPU frame p50/p95/p99, draw calls, memory, VRAM, VFX particle budget, hardware/drivers; tune until target p95 ≤16.67ms or report failure and fix proposals. Smoke visibility must not offer unfair advantage at low quality.


## Completion contract

- Work only on this milestone and its direct prerequisites; don't attempt the full game in a single session.
- Preserve existing project/configuration, inspect current files and Git state first.
- Build and run actual relevant checks; save commands, logs, screenshots and deterministic test evidence.
- Update `TASKS.md`, `docs/DEVELOPMENT_STATUS.md`, appropriate design/asset/license docs.
- Commit and push verified milestone to configured private Git remote (do not create a new repo if it exists; never force-push). If not authenticated/configured, report precise blocker.
- Summarize implemented, tested, untested, missing assets, remaining work and commit ID. Do not claim success without tests.
