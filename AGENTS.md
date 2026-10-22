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

## Permanent development documentation and GitHub showcase rules

These requirements apply to **all future development tasks and milestones**. Read [docs/DOCUMENTATION_WORKFLOW.md](docs/DOCUMENTATION_WORKFLOW.md) and use the [report templates](docs/templates/README.md). Documentation must describe the actual implementation and evidence; plans and unfilled templates are not completed features.

### README and change history

- The root README is both the project introduction and the visual/technical development showcase. Update it whenever a meaningful feature, asset, visual element or simulation system changes.
- Maintain these sections: Project Overview; Current Development Status; Latest Development Updates; Visual Progress — Before & After; Physics & Simulation Improvements; Gameplay Systems; Architecture & Technical Diagrams; Performance Benchmarks; Development Roadmap.
- Keep major-change summaries concise and link to detailed reports in `docs/`. Preserve existing useful content and keep navigation readable.
- Maintain dated, chronological entries in `docs/CHANGELOG.md` (newest first), including evidence and limitations.

### Visual evidence

- Significant terrain/geography, Adriatic environment, vegetation/forest, textures/PBR materials, aircraft/cockpits, trucks/vehicles, firefighters/equipment, HDRP lighting/atmosphere, sea/water, fire/smoke/water VFX, HUD/menu/interface changes require **actual Unity Editor Game View or running-build screenshots**.
- Capture BEFORE when an actual earlier version exists and AFTER for the change. Record what changed, assets/textures used with provenance, and relevant measured performance impact or explicitly unmeasured impact.
- Store reasonably compressed images and a report under `docs/previews/<feature-name>/`, with meaningful names such as `terrain_before.png` and `terrain_after.png`. Embed representative real images in README; prefer side-by-side comparisons when useful.
- Never use concept art as implementation evidence or fabricate a baseline. State **"No baseline was captured"** when none exists. If graphical capture is unavailable, record the limitation and an outstanding evidence task.

### Physics and simulation evidence

- Significant changes to aerodynamics, lift/drag/thrust, aircraft mass/inertia/control, wind/turbulence, scooping, water release/trajectory/dispersion, ignition/combustion/spread, fuel consumption, suppression, smoke transport, hose pressure/flow, vehicle physics or other simulation mechanics require an implementation-based comparison.
- Write `docs/physics/<system-name>/COMPARISON.md`: actual previous behavior and equations/algorithm/governing logic; new equations/algorithm in LaTeX-compatible Markdown; important variable definitions and SI units; assumptions/approximations; reason for the change; solved problems and remaining limitations.
- Include **two diagrams, BEFORE and AFTER**, describing the real versions. Use Mermaid for processes and architecture, and plots or annotated physical diagrams when flowcharts cannot explain the behavior. For a new system, explicitly show that the baseline had no implementation; never invent a previous model.
- Compare numerical results under equivalent inputs, environment, time step, duration and random seeds where applicable. Include test cases, units, tolerances, verification results, source-code references and reproducible commands/raw data. Use tables and graphs where appropriate. Never invent benchmark values; unavailable baseline results must be labeled unavailable.
- Relevant metrics include aircraft lift/drag/climb/stall/payload response, water footprint/dispersion/wind drift/coverage, and fire spread/burned area/fuel/intensity/suppression. Summarize and link the comparison from README.

### Gameplay and architecture evidence

- For changed mechanics, record previous and new behavior, player-facing impact, network/server implications, updated gameplay flow and relevant tests.
- Use Mermaid for gameplay loops, dispatch/unit assignment, aircraft missions, firefighting operations, multiplayer synchronization, dedicated servers, persistence and incident resolution as relevant. Keep implementation diagrams consistent with code and clearly label proposed designs.

### Performance evidence

- Target **60 FPS at 1080p on documented reference hardware**. After significant rendering or simulation changes, measure where tooling permits and record results in `docs/PERFORMANCE_HISTORY.md`.
- Track average FPS, 1% low FPS when measurable, CPU/GPU frame times, draw calls, memory, fire-simulation execution time, dedicated-server tick duration and relevant network bandwidth. Record unavailable counters and reasons rather than substituting zero.
- Record hardware/OS/drivers, resolution/upscaling/preset, build/revisions, scene/test conditions, player/entity counts, measurement method and equivalent before/after runs. Follow `docs/PERFORMANCE_60FPS.md` for representative populated/coastal/smoky build scenarios and percentile gates. Do not infer server capacity from client FPS.
- Claim performance improvements only from measurements. Tooling limitations become explicit outstanding tasks; they do not waive acceptance gates.

### Completion and publishing gate

1. Inspect and checkpoint; identify applicable evidence and capture the existing baseline before modifying it.
2. Implement the change, verify it works and run relevant unit/integration/render/network checks.
3. Capture visual evidence where applicable; document actual mathematical models and generate BEFORE/AFTER diagrams, numerical comparisons and plots where applicable.
4. Update README, `docs/CHANGELOG.md`, `TASKS.md`, and `docs/DEVELOPMENT_STATUS.md`; update performance history and asset/provenance records where applicable.
5. Do not mark a milestone complete while required screenshots, diagrams, measurements or explanations are missing. Record missing evidence, reason and a concrete follow-up task. A verified partial increment may be committed with the milestone explicitly left open.
6. Commit meaningful implementation changes together with their tests, documentation, screenshots, mathematical comparisons and diagrams.
7. Push verified work to the **existing** repository, `https://github.com/itsmangooo/FIRELINE-ADRIATIC`. Completed verified milestones go to `main`, respecting any established branch/review workflow. Never create another GitHub repository or force-push. Verify the remote commit after pushing and report failures honestly.

For documentation-only tasks, verify links, structure and consistency; mark runtime screenshots, physics comparisons and performance measurements not applicable with a reason. Do not start unrelated feature development to demonstrate this workflow.
