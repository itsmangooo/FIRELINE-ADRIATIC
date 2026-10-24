# Performance history

## 2026-10-24 — M03 isolated flight Step elapsed diagnostic

Two separate Mono Windows dedicated players on i5-9400F / Windows 11 build 26300 / 16324 MiB visible RAM; Editor closed, one proof at a time, 100 warm-up calls excluded and 1000 retained independent 5 ms physical-step evaluations per run. These measure one pure `Step` call, not the complete server tick or a client frame.

| Actual process | Warm-up / retained calls | p50 / p95 / p99 (ms) | Maximum (ms) / retained call | Gen-0 collections |
|---|---:|---:|---:|---:|
| Run 1 | 100 / 1000 | 0.0017000 / 0.0017000 / 0.0018000 | 0.0457000 / #1 | 0 |
| Run 2 | 100 / 1000 | 0.0017000 / 0.0020000 / 0.0023000 | 0.0479000 / #1 | 0 |

GPU/FPS/draw calls are not applicable to Null graphics; complete host timing, memory/residency/allocation bytes and flight network bandwidth are unmeasured. Zero gen-0 collections does not prove zero allocation. No earlier flight implementation exists, so BEFORE cost/improvement is unavailable. [Raw lossless runtime files, method, SVG plot, exact units/tick/warm-up/interval guide and limitations](reports/2026-10-24-m03-debug-flight.md#timing-method-measured-results-and-graph-guide). Existing render-hitch data remain unchanged; optimization is deferred.

## 2026-10-23 — M02 concurrent-client callback/UDP diagnostic

Three 1280×720 Balanced/VSync=1 D3D11 Mono release clients (third joining later), one local server and optional UDP proxy, Editor closed, i5-9400F / GTX 1660 SUPER / 16,323 MiB / Windows 11 build 26300 / driver 32.0.16.2101. Five wall seconds of adapter warm-up; short per-client recordings, not matched five-minute 1080p scenarios. Direct client #1 averaged 52.63 FPS, 1% low 4.19, p95 19.034 ms, with **1193.136 ms at sample #602**; client #2 also recorded 1166.763 ms at #605. All six p95 intervals exceed 16.67 ms. Later impaired timings cannot establish an improvement: startup/history/duration differ.

| Phase | Proxy seconds | Received / forwarded UDP payload bytes | Observed random drops / received datagrams | Peak queued | Received / forwarded KiB/s |
|---|---:|---:|---:|---:|---:|
| initial | 44.883 | 854,530 / 816,376 | 74 / 1657 | 20 | 18.593 / 17.763 |
| restart | 13.641 | 42,652 / 39,017 | 9 / 132 | 20 | 3.054 / 2.793 |

Actual UDP payload counts exclude IP/UDP headers and include protocol ACK/retransmission/startup/idle. Constant 80 ms each-way delay and configured 5% loss passed replication/restart; jittered startup failed separately. CPU/GPU execution, draw calls, VRAM, complete server tick execution and confirmed dropped presentations remain unavailable; Unity allocated bytes are in raw JSON. No overlay isolated-cost, optimization, 60 FPS or capacity claim. [Raw CSV, six-panel/full-detail SVG, statistical definitions, all scales/ticks/warm-up/interval explanations](reports/2026-10-23-m02-replication.md#callback-measurements-and-graph-reader-guide); [FL-06 locations/deferred attribution](performance/FRAME_LOSS_REGISTER.md#m02-measured-callback-overruns--2026-10-23). Historical data below unchanged.

Target: **60 FPS at 1080p on documented reference hardware**, with the build scenarios, presets and percentile gates in the [performance plan](PERFORMANCE_60FPS.md). Client rendering and 100-player dedicated-server capacity have separate acceptance gates.

## 2026-10-23 — M01 bounded headless fire-step timing

On i5-9400F / Windows 11 build 26300, Editor closed and one proof server at a time, the 16-chunk/16,384-cell capacity fixture visited at most 5120 cells per step. Across 120 accelerated steps, `World.Step` p95 was **9.6310 ms**, maximum **10.1727 ms**, with four generation-0 collections and bounded total update-age backlog up to 13.2 simulated seconds. This measures elapsed CPU-body duration, including preemption/delta creation, not complete host ticks, client FPS, GPU work or 100-player capacity. No explicit warm-up was discarded.

M00 had no fire model, so a comparable previous fire-step cost is unavailable rather than zero. [Raw data, hardware, numerical plots and graph reader guide](reports/2026-10-23-m01-fire-wind.md#performance-and-graph-interpretation). Whole-host timing, per-frame allocations/GC pauses and gameplay bandwidth remain future measurements; headless GPU/VRAM/client counters are not applicable here. Existing 1080p rendering qualification remains open.

## 2026-10-22 — Five-minute 1080p coastal clear/static-fog release pair

On i5-9400F / GTX 1660 SUPER / driver 32.0.16.1742, one visible Balanced release client at a time, Editor closed, 30 s warm-up + 300 s recording: clear averaged 59.99849 FPS, 1% low 57.93009, p95 16.99410 ms; static fog averaged 59.99851 FPS, 1% low 58.11117, p95 16.98840 ms. Both exited 0; CSV summaries matched recomputation. CPU/main/GPU timing, draw-call and Unity allocated-memory counters were available; VRAM, GC attribution, server tick duration and bandwidth remain uninstrumented.

Both p95 intervals exceed the 16.67 ms gate. The imported coastal DSM/PBR scene uses generated vegetation/building/sea/fog proxies and lacks production gameplay; **no 60 FPS acceptance, optimization or server-capacity pass** is claimed. The first invisible-fog attempt was rejected and fixed before this pair. [Full conditions, raw data, actual screenshots, rendered SVG timeline, graph reader guide and pending counters](performance/2026-10-22-m00-coastal.md).

## 2026-10-22 — Existing M00 frame intervals mapped; optimization deferred

Offline analysis of the diagnostic below found three intervals above 20 ms: #1 (22.02620 ms), #2 (83.47170 ms) and #216 (20.18150 ms). The largest interval spans reconstructed sample-window time 0.022026–0.105498 s. All six recorded summary fields were recomputed and matched; the original samples and full-run results remain unchanged. These are callback overruns, not confirmed dropped presentations or CPU/GPU execution measurements.

The [frame-loss register](performance/FRAME_LOSS_REGISTER.md) includes rendered vector drawings, all sample flags, source suspects and reproducible analysis. No new benchmark, measured improvement or cause attribution is claimed. Profiling and optimization are deferred; representative 1080p acceptance remains outstanding.

## 2026-10-22 — M00 calibration diagnostic (not acceptance)

Recorded on i5-9400F / GTX 1660 SUPER, 16,323 MiB RAM, Windows 11, driver 32.0.16.1742. Two rendered Windows release clients plus a headless server ran on loopback with the Editor open. Resolution 1280×720, High Fidelity preset, VSync=1; no representative coastal/fire/smoke entities.

After five rendered seconds of warm-up, 896 main-camera callback intervals totaling 15.00098 seconds yielded average 59.72945 FPS, 1% low 38.22727 FPS, p95 interval 17.36010 ms and p99 18.06480 ms. Peak Unity allocated memory was 535,716,161 bytes. CPU/GPU execution time, draw calls, VRAM, server tick duration and bandwidth were not instrumented. No matched BEFORE run exists; no improvement or 1080p/60 FPS pass is claimed.

See [full conditions, raw samples and outstanding measurement tasks](performance/2026-10-22-m00-calibration.md). Earlier hidden-window and unready-capture runs were rejected. Asset integration and the representative benchmark remain M00 acceptance gaps.

## 2026-10-22 — Documentation workflow established

No performance measurements were taken. This change affects documentation and Git bookkeeping only; there is no rendering or simulation modification to compare. Hardware, average/1% low FPS, CPU/GPU frame time, draw calls, memory, fire simulation, server ticks and network bandwidth are **not measured**, not zero and not improved.

The initial representative benchmark scene, named hardware and client/server runs remain outstanding under M00 in [TASKS.md](../TASKS.md). No runtime tooling failure is claimed; runtime profiling was outside this documentation-only change.

## Recording future runs

Add dated entries newest first, linking a completed [performance report](templates/PERFORMANCE_RUN.md) under [docs/performance/](performance/README.md) and raw artifacts. Include hardware, resolution/preset, scene and conditions, player/entity count, measurement method, before/after revisions and results. Explain missing counters or incomparable baselines and track follow-up work. Never turn target budgets into measured results.
