# 60 FPS client and scalable server performance plan

## Definition of success

**1080p / 60 FPS on named minimum-target hardware** in a representative gameplay scene. 60 FPS budget = **16.67 ms per frame**. Specify test hardware, driver, graphics settings, native/upscaled resolution, populated world and active fire intensity with every capture. A 60 FPS capture of an empty HDRP scene does NOT pass.

Suggested reference PCs for QA (examples, not verified to pass):
- Mid-range target: recent 6-core desktop CPU, 16–32 GB RAM, RTX 3060 12 GB or comparable GPU, SSD, Windows 11.
- Higher quality target: RTX 4070-class or comparable, 32 GB RAM.
- Budget/lower presets on weaker GPUs where feasible; test actual VRAM and GPU bottlenecks rather than promising specific hardware support.

## Frame budgets (planning only)

| Subsystem | Initial CPU/GPU planning envelope | What to profile |
|---|---|---|
| Main thread incl. scripts, transforms, input and networking | aim < 5 ms CPU | Profiler Timeline, jobs sync, GC spikes |
| Physics + interpolation + active region gameplay | aim < 3 ms CPU | rigid bodies, nav, colliders, allocs |
| GPU terrain/foliage/aircraft/lighting/shadows | aim < 8 ms GPU | GPU profiler, RenderDoc/Frame Debugger |
| GPU fire/smoke/water + post processing | aim < 6 ms GPU | overdraw, volumetrics, transparent VFX |
| UI/culling/upload overhead | aim < 2 ms CPU | draw calls, uploads, UI rebuild |

**Do not add these sequentially and claim a universal time**: GPU and CPU execute in parallel. CPU and GPU *each* need to fit ~16.67 ms with headroom; the breakdown budgets are rough target envelopes with overlap. Aim p95 < 16.67 ms and p99 < ~25 ms at representative severity, then iterate. Avoid CPU garbage allocation spikes >2 ms and sustained VRAM paging. Measure build, not just Editor Play Mode.

## Scene benchmark matrix

- S1: Zemunik base aircraft+crew, clear morning.
- S2: low-level flyover densely vegetated coast, view distance kilometers.
- S3: active wildfire with 2 smoke columns + 1 water drop + 3 vehicles.
- S4: ground team close to flames, hose VFX, damaged/charring forest.
- S5: realistic stress, multiple incident columns visible, 9 aircraft in flight/nearby as permitted, 25+ ground vehicles or their relevant LODs, 100 connected clients (not necessarily all visible to one camera).
- S6: dusk/night coastal transition with volumetrics, headlamps and spotlights.

Capture time series over 5+ minutes (cold start excluded separately), p50/p95/p99 CPU main/render, GPU frame, memory/VRAM peaks, GC alloc/frame, entity counts, triangles/draw calls, VFX visible counts, network inbound/outbound, server tick. Fail if frame-time targets exceeded; report settings and machine.

## HDRP quality tiers

| Feature | Performance | Balanced (default) | Ultra |
|---|---|---|---|
| Resolution scaler | dynamic/upscaling allowed | 1080p native / dynamic on GPU bound | native if practical |
| Shadow distance/cascades | short/low | medium | high |
| Volumetric fog | low | medium | high |
| Smoke VFX particles | 25% visual density | 50–60% | 100% reference |
| Distant vegetation | aggressive impostors | LOD + impostors | longer LOD |
| Reflection effects | probes/basic | SSR selective | more SSR if budget |
| SSAO/contact | restrained | selective | detailed |
| Motion blur/film grain | off | optional | optional |
| Terrain textures | 1K/2K | 2K | 4K where warranted |

**Gameplay smoke visibility stays consistent across quality tiers**. Lower smoke particles must NOT let a player see through hazard zones that a high-quality player cannot. Render coarse consistent occlusion proxies from server smoke opacity field on every preset.

## Highest impact optimizations

1. **Visibility:** terrain/camera culling, occlusion culling, large-scale sector streaming; don't keep all of Croatia active.
2. **Vegetation:** GPU instancing, shared materials, LODGroups, billboard/impostor distance tiers, sensible shadow casters, avoid unique material per tree.
3. **VFX:** limit transparencies/overdraw; bound particle count and smoke volume resolution; no cloud of full-res alpha quads up close; GPU simulation only for cosmetics.
4. **Assets:** 2K as default for visible building/terrain material layers; 4K for key hero assets, shared material atlases, texture streaming, per-platform compression; mesh LODs and proxy colliders.
5. **Lighting:** limit realtime shadow-casting lights, cascades and volumetric resolution; time-of-day uses reproducible HDRP volumes and budget presets.
6. **Networking:** interest management, adaptive frequency, delta snapshots, region summary for commanders; don't instantiate hidden remote objects with full visual VFX.
7. **Server simulation:** chunk fire grid, fixed-step + adaptive LOD, Jobs/Burst if measured; skip pure visual tasks in server build; no DB writes per fire cell tick.
8. **Allocations:** avoid LINQ/GC-heavy operations in hot loops, pool gameplay impact objects, native containers safely, instrument allocations.

## Server target separate from client

A 60 FPS client does not imply 100-player server. Server acceptance: with 100 realistic synthetic clients and ≥3 burning regions, ≤ configured tick deadline p95, no accumulating queue backlog, finite memory, consistent fire revisions/checkpoint recovery. Actual hardware cost budget TBD by profiling. Document egress/ingress per-client and total; prototype at 2,16,32,64,100 before claims.

## Continuous perf gate

- Automated benchmark scene: run Release build at chosen preset/hardware, record JSON of frame and tick stats.
- Capture automated before/after photos and GPU counters on a reproducible camera route.
- Any new vegetation/VFX/streaming asset must include a GPU/frame impact summary.
- If default Balanced cannot stay near 60 FPS under target scene, tune quality preset or revise visual system, not claim 'optimized'.

## References

- Unity profiling in built apps: https://docs.unity3d.com/2022.2/Documentation/Manual/profiler-profiling-applications.html
- Unity draw call methods: https://docs.unity3d.com/6000.0/Documentation/Manual/optimizing-draw-calls-choose-method.html
- Unity dedicated server optimization: https://docs.unity3d.com/2022.3/Documentation/Manual/dedicated-server-introduction.html
