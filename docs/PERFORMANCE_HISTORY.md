# Performance history

Target: **60 FPS at 1080p on documented reference hardware**, with the build scenarios, presets and percentile gates in the [performance plan](PERFORMANCE_60FPS.md). Client rendering and 100-player dedicated-server capacity have separate acceptance gates.

## 2026-10-22 — Documentation workflow established

No performance measurements were taken. This change affects documentation and Git bookkeeping only; there is no rendering or simulation modification to compare. Hardware, average/1% low FPS, CPU/GPU frame time, draw calls, memory, fire simulation, server ticks and network bandwidth are **not measured**, not zero and not improved.

The initial representative benchmark scene, named hardware and client/server runs remain outstanding under M00 in [TASKS.md](../TASKS.md). No runtime tooling failure is claimed; runtime profiling was outside this documentation-only change.

## Recording future runs

Add dated entries newest first, linking a completed [performance report](templates/PERFORMANCE_RUN.md) under [docs/performance/](performance/README.md) and raw artifacts. Include hardware, resolution/preset, scene and conditions, player/entity count, measurement method, before/after revisions and results. Explain missing counters or incomparable baselines and track follow-up work. Never turn target budgets into measured results.
