# AI development agent instructions — FIRELINE: ADRIATIC

## Read before implementing

1. `README.md`, `docs/GAME_VISION.md`, `docs/GAME_DESIGN.md`, `docs/TECHNICAL_ARCHITECTURE.md`.
2. `docs/SIMULATION_SPEC.md`, `docs/NETWORK_ARCHITECTURE.md`, `docs/PERFORMANCE_60FPS.md`.
3. `TASKS.md`, `docs/DEVELOPMENT_STATUS.md`, `docs/ASSET_MANIFEST.md`, `docs/ASSET_LICENSES.md`.

## Absolute rules

- **Do not recreate or reset the user's existing Unity project.** Inspect before modification and preserve existing files/configuration; create Git checkpoint first.
- **No feature-completion fiction:** documentation only is not an implementation; local test is not a 100-player test; asset URL is not an imported model; VFX flame is not authoritative combustion.
- Dedicated server owns wind, fire ignition/spread, fuel, combustion stages, vegetative destruction, gameplay smoke, aircraft/vehicle state, water footprints, equipment/incident decisions. Client-side sparks, smoke/flames particles, visual water and sounds are cosmetic.
- Run hardware profiling before claiming 60 FPS; test named PC in builds with representative populated/coastal smoky scenes. Keep image-quality presets.
- Keep aircraft/fire/wind in SI units; regionally compress *travel* geography, not runway dimensions or local flight laws.
- Research actual Unity package versions/editor compatibility before choosing Netcode for Entities, HDRP, Addressables, Physics, Jobs or ECS packages. No assume installed features.
- Only download from authorized official asset links. Verify each license, size, format, rig, material conversion and attribution before checking 'integrated'. Never scrape protected assets or leak secrets.
- All C# code must be C#; no need for JavaScript/TypeScript game code. Prefer pure simulation functions and data-oriented decomposition.
- Keep assemblies separated: Server, Simulation, Shared, Client, Authoring, Tests. Do not import HDRP assets in headless server runtime.
- Never execute arbitrary C# mods on server by default. Use validated data manifests and compatible bundles.
- Use direct IP self-hosting as baseline and avoid mandatory paid/cloud lobbies.
- Build/test an increment then update `TASKS.md`, `docs/DEVELOPMENT_STATUS.md`, document remaining blockers, commit/push if remote configured and permitted.

## Workflow

Check branch, dirty status, Unity version and installed packages → select next small pending task → implement → run unit/integration/render/network checks → record evidence → commit with descriptive message → push existing remote (never force) → handoff summary.

Stop if a task is blocked by licensing or incompatible package, report it and switch to independently testable next piece without pretending completion.
