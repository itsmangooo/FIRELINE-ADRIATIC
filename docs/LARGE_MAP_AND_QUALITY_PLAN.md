# Large map, HDRP environment and graphics delivery plan

**Planned, requested 2026-10-24.** The user may supply a roughly **30 × 4 km** map tonight. Its arrival, exact dimensions, contents, formats, provenance and runtime cost are not yet verified. This plan does not import it or change the active M03 milestone. Existing scenes and quality assets remain preserved.

## Delivery gates and first playable scope

| Gate | Player-visible outcome | Still required |
|---|---|---|
| Current M03 engineering test | Connected aircraft, basic test inputs, two cameras and authoritative pose | Not a complete aerial firefighting loop |
| M03 aerial prototype | Repeatable takeoff/route/landing, valid scoop/drop and shared suppression | Calibrated/configured flight resources, collision, water accounting/trajectory and equivalent client evidence |
| M04 visible aerial slice | Small coastal test sector, HDRP environment and real fire/smoke/water presentation | Real screenshots, normal-control loop and representative profiling |
| M05 ground co-op | Fire engine/pump/hose and aircraft act on the same fire | Validated resources and multiplayer ground/aerial operation |
| M05A + M06 approachable operations | Menus, tutorials, settings, roles, dispatch and incident completion | Actual service bindings; no placeholder completion |
| M08–M09 broader world and release | Streamed region, load-qualified server and measured quality tiers | Real large-map traversal/load/profile evidence and packaging |

These are dependency gates, not calendar commitments. A first aerial test slice can precede full-map completion, ground crews, persistence and 100-user qualification. Re-estimate delivery after an actual normal-control takeoff/landing and conserved-water drop are verified; the current engineering view is not evidence that those gates are already met.

## Map and art integration

- **M04:** inspect the supplied map without overwriting original files; inventory dimensions, height scale, coordinate frame, terrain/mesh tiles, seams, collision descriptors, vegetation density, texture sources and memory. Start with one bounded coastal/airstrip/fire/scoop sector. Keep local flight/runway/water dimensions in SI meters; any travel compression needs an explicit geographic decision.
- **M04:** apply the audited HDRP Terrain Lit/Lit textures, layer/mask conversion, water/coast/vegetation treatment, sky, lighting and atmosphere. Preserve original source/provenance and capture actual BEFORE/AFTER views. Do not load the entire proposed 120 km² at full detail merely because the source map exists.
- **M08:** implement/qualify sector streaming, prefetch for flight speed, world coordinate precision/floating-origin strategy if measured necessary, lightweight server collision/height/fuel descriptors and interest management across the full map. No client HDRP content in the dedicated server.
- **M09:** validate representative route traversal and memory/residency on the delivered full map. Until the map is inspected and sectors work, its footprint remains a supplied-content expectation rather than an implemented world.

## User's graphics target

The requested **Ultra target is at least 60 FPS at 1920×1080 on an Intel i5-9400F, 16 GB RAM and GTX 1660-class GPU, and suitable higher hardware**. This supersedes the old planning assumption that budget hardware must use only lower presets. It is a design/acceptance target, **not a current performance guarantee**. The local GTX 1660 SUPER measurement does not automatically qualify a base GTX 1660 or every newer GPU. Reference OS, drivers, VRAM, storage, native/render resolution, scaling and presentation settings must be named for each run.

M04 develops measured budgets and representative content; **M05A** implements persistent **Low, Medium, High, Ultra and Custom** options, and **M09** qualifies them. Tiers must control actual view/draw distance, terrain/vegetation LOD and density, shadows, texture residency, animation update/detail distance, fire/smoke/water effect quality, volumetrics, reflections, AA and supported scaling. Do not invent distances or density multipliers before profiling real assets. Keep all gameplay smoke/hazard occlusion and authoritative simulation consistent across tiers; changing animation/VFX detail cannot change collision, controls or suppression outcomes.

Use the [performance matrix and percentile gates](PERFORMANCE_60FPS.md), five-minute populated coastal/smoky build runs and reproducible flight/ground cameras. Preserve image-quality intent. Report native versus upscaled results separately, average/1% low, CPU/GPU timings, p95/p99, memory/VRAM, streaming stalls and server/network costs. If Ultra misses the target, leave the gate open, identify the measured cost and revise content/rendering budgets transparently. Renaming a lower preset Ultra, silently dropping resolution or quoting an empty-scene average does not satisfy the target.

Optimization remains deferred at the user's earlier request; current hitches stay in the [frame-loss register](performance/FRAME_LOSS_REGISTER.md). Map intake/planning is allowed when supplied; bulk integration, lighting/presets and optimization wait for their milestone dependencies. No assets, graphics settings, runtime screenshots or new measurements are claimed by this planning document.
