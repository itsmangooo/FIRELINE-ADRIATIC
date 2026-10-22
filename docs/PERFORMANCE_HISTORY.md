# Performance history

Target: **60 FPS at 1080p on documented reference hardware**, with the build scenarios, presets and percentile gates in the [performance plan](PERFORMANCE_60FPS.md). Client rendering and 100-player dedicated-server capacity have separate acceptance gates.

## 2026-10-22 — M00 calibration diagnostic (not acceptance)

Recorded on i5-9400F / GTX 1660 SUPER, 16,323 MiB RAM, Windows 11, driver 32.0.16.1742. Two rendered Windows release clients plus a headless server ran on loopback with the Editor open. Resolution 1280×720, High Fidelity preset, VSync=1; no representative coastal/fire/smoke entities.

After five rendered seconds of warm-up, 896 main-camera callback intervals totaling 15.00098 seconds yielded average 59.72945 FPS, 1% low 38.22727 FPS, p95 interval 17.36010 ms and p99 18.06480 ms. Peak Unity allocated memory was 535,716,161 bytes. CPU/GPU execution time, draw calls, VRAM, server tick duration and bandwidth were not instrumented. No matched BEFORE run exists; no improvement or 1080p/60 FPS pass is claimed.

See [full conditions, raw samples and outstanding measurement tasks](performance/2026-10-22-m00-calibration.md). Earlier hidden-window and unready-capture runs were rejected. Asset integration and the representative benchmark remain M00 acceptance gaps.

## 2026-10-22 — Documentation workflow established

No performance measurements were taken. This change affects documentation and Git bookkeeping only; there is no rendering or simulation modification to compare. Hardware, average/1% low FPS, CPU/GPU frame time, draw calls, memory, fire simulation, server ticks and network bandwidth are **not measured**, not zero and not improved.

The initial representative benchmark scene, named hardware and client/server runs remain outstanding under M00 in [TASKS.md](../TASKS.md). No runtime tooling failure is claimed; runtime profiling was outside this documentation-only change.

## Recording future runs

Add dated entries newest first, linking a completed [performance report](templates/PERFORMANCE_RUN.md) under [docs/performance/](performance/README.md) and raw artifacts. Include hardware, resolution/preset, scene and conditions, player/entity count, measurement method, before/after revisions and results. Explain missing counters or incomparable baselines and track follow-up work. Never turn target budgets into measured results.
