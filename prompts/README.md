# How to use the prompts in Codex

- **First message:** `00_KICKOFF.md` only. It introduces the game, asks Codex to read all context/docs and perform a project audit.
- Then **one prompt at a time** in numbered order; do not paste all sessions together to implement simultaneously.
- Some tests/dependencies overlap. If M01 fire code works but no multiplayer, continue into the real two-client replication test before claiming fire is multiplayer-ready.
- Read `TASKS.md` before each new session. Status + docs survive context clearing, chat history does not.
- At the end of each verified task: build/tests, docs update, Git commit, push, new task. Reuse existing Git remote.
- Save screenshots/benchmark data to trace what 'implemented' actually means. Asset URLs are not imported assets.

**Suggested first Codex message:**

> Work on the existing Unity 6 HDRP project. Read AGENTS.md, README.md, TASKS.md, docs/GAME_VISION.md, docs/GAME_DESIGN.md, docs/TECHNICAL_ARCHITECTURE.md, docs/SIMULATION_SPEC.md, docs/NETWORK_ARCHITECTURE.md, docs/ASSET_MANIFEST.md and docs/PERFORMANCE_60FPS.md. Then perform only the work in prompts/00_KICKOFF.md. Preserve the existing project; do not recreate it, do not implement future milestones, and report exact environment/build/asset findings. Git commit verified changes only.
