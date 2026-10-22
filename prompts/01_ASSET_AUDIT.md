# Codex Session 01 — Asset download, licensing, Blender/HDRP pipeline

Read `docs/ASSET_MANIFEST.md`, `docs/ASSET_LICENSES.md`, `docs/ART_DIRECTION.md` and `docs/PERFORMANCE_60FPS.md`.

Research/download through *official permitted links* only; do not evade Sketchfab downloads/authentication. Audit and prepare only one CL-415 exterior asset and the minimum approved Poly Haven textures for the first forest/coast fire scene. Verify actual 3D file existence, exact license, animation readiness, geometry/pivot, cockpit/gear/prop parts, and texture channel maps. If a model is not usable, keep a blocker, don't create fake 'production asset' replacements.

Prepare Blender conversion instructions or automated importer where reliable: FBX/glTF to Unity-ready, scale in meters, correct forward/up axes, original sources kept untouched, optimized mesh + LOD and collider proxy. Prepare HDRP Lit/Terrain Lit materials from diffuse/normal DX/roughness/metallic/AO and appropriate HDRP mask-channel packing; import 2K by default, use 4K only as justified; texture streaming and LOD.

Deliver a downloadable asset/source manifest table showing **verified listing vs downloaded vs imported vs visually QA'd**; create one reference scene containing the imported rock/dry ground/forest/burned ground, and screenshots of actual material appearance. Keep all provenance/attribution in manifest. Don't bulk download massive asset libraries and don't start full gameplay in this step.


## Completion contract

- Work only on this milestone and its direct prerequisites; don't attempt the full game in a single session.
- Preserve existing project/configuration, inspect current files and Git state first.
- Build and run actual relevant checks; save commands, logs, screenshots and deterministic test evidence.
- Update `TASKS.md`, `docs/DEVELOPMENT_STATUS.md`, appropriate design/asset/license docs.
- Commit and push verified milestone to configured private Git remote (do not create a new repo if it exists; never force-push). If not authenticated/configured, report precise blocker.
- Summarize implemented, tested, untested, missing assets, remaining work and commit ID. Do not claim success without tests.
