# Performance history

## 2026-10-26 — M03 real-time rendered flight diagnostic

[Complete per-run counter table, timed aircraft adapter limits and peak locations](performance/2026-10-26-m03-realtime-flight.md).

Actual two Windows Mono local dedicated/rendered-owner/headless-observer groups, Editor closed; 1920×1080 existing Balanced/VSync=1/per-process D3D11; i5-9400F/GTX 1660 SUPER/16,323 MB Unity RAM/Windows 11 26300/driver 32.0.16.2101. Synthetic runway, zero wind, actual QA flight/captures/structured recording. Accepted: **3,675 intervals / 62.268213 s**, **59.018877 average / 20.534374 1% low FPS**, p95/p99 **17.657900/19.401200 ms**, peak allocated **486,693,979 bytes**. Repeat: **3,715 intervals / 62.351185 s**, **59.581867 average / 30.457950 1% low FPS**, p95/p99 **17.466800/18.516300 ms**, peak allocated **490,900,602 bytes**. Five rendered seconds warm-up; separate per-run distributions, no mixed percentile aggregation.

**FL-11** keeps accepted/repeat callback locations. [Every raw interval, exact scopes/hardware and full clock/ruler/zoom guides](reports/2026-10-26-m03-realtime-flight.md). Callback spacing includes waits/presentation and is not CPU/GPU time or lost-presentation counts. CPU/GPU execution, draw calls, VRAM, bandwidth/full-server ticks unavailable. Host adapter timings include explicit QA contact/serialization. Different camera/route/control/cache/startup conditions preclude a matched speedup/cost claim; no representative populated coast/smoke/Ultra 60 FPS or capacity pass. Rejected first attempt metrics retained separately. Optimization and earlier FL/HOST/NET entries stay deferred.

## 2026-10-26 — M03 authority route is numerical, not an FPS run

Two actual headless processes validate an accelerated calm-air route and exact physical replay. [Route data and clock guide](reports/2026-10-26-m03-authority-route.md). Physical trajectory durations/substeps are not CPU time, frame intervals or throughput. No new rendering/system change: new FPS/1% lows, CPU/GPU frame time, draw calls, VRAM, bandwidth and real-time server-tick measurements are unavailable/not applicable. The retained legacy `stepMs` array still times the old force Step only, not route/fuel/authority execution. No matched speedup or representative 60 FPS claim. Existing FL-10/HOST-01/NET-01 remain deferred; graphical baseline is the instrument run below.

## 2026-10-25 — M03 instrument rendered diagnostic

Actual Windows Mono release, Editor closed, **1920×1080 Balanced / VSync=1 / per-process D3D11**, i5-9400F / GTX 1660 SUPER / driver 32.0.16.2101 / Windows 11 26300 / 16,323 MB Unity RAM. Existing synthetic runway/prototype plus read-only instrument panel; one late headless observer and dedicated server. Five rendered seconds warm-up; **1,440 intervals / 24.515733 s** retained. Average/1% low **58.737791 / 17.839704 FPS**, p95/p99 callback spacing **19.093599 / 24.828499 ms**, peak allocated **495,136,036 bytes**. CPU/GPU execution, draw calls, VRAM, bandwidth and full server tick timings unavailable.

**FL-10:** #504 **184.623405 ms** at **8.532061–8.716684 s**; #242 **172.498405 ms** at **4.016753–4.189251 s**; #760 **52.085102 ms** at **13.049114–13.101199 s**. 729/10 intervals exceed 16.667/33.333 ms rulers; these are not dropped-presentation counts. Six host catch-up overruns separately discard **128.7779 ms**; largest host gap **89.7431 ms** at host elapsed **34.1454958 s**, adapter there **0.1349 ms**, not full-server timing. [Real data, conditions, exact scopes and both graph reader guides](reports/2026-10-25-m03-flight-instruments.md#actual-visual-and-performance-evidence), [follow-up sites](performance/FRAME_LOSS_REGISTER.md#m03-instrument-client-callbacks--2026-10-25).

No matched instrument-cost regression/speedup: cache/startup/admission/scheduling conditions are uncontrolled. No resource-step execution-cost comparison, representative five-minute coastal/smoke 60 FPS, future Ultra, Linux/WAN/capacity or full route qualification. Optimization remains deferred; older observations stay open.

## 2026-10-24 — M03 finite-fuel rendered regression

Actual Windows Mono release, Editor closed, **1920×1080 existing Balanced / VSync=1 / per-process D3D11**, i5-9400F / GTX 1660 SUPER / driver 32.0.16.2101 / Windows 11 26300 / 16,323 MB Unity RAM. Same debug runway/model/HUD, now 400 kg initial finite fuel, one late headless observer plus dedicated server. Five rendered seconds warm up; **1,448 intervals / 24.417126 s** retained. Average/1% low **59.302638 / 25.811398 FPS**, p95/p99 callback spacing **17.182400 / 17.807700 ms**, peak allocated **491,292,791 bytes**.

**FL-09:** #499 **182.220802 ms** at **8.433306–8.615527 s**; #242 **160.174206 ms** at **4.017848–4.178022 s**. 744/2 intervals exceed 16.667/33.333 ms rulers. These callback intervals include waits/presentation; they are not CPU/GPU execution or confirmed dropped presentations. Other counters remain unavailable as detailed in the [report](reports/2026-10-24-m03-fuel.md#actual-visual-and-performance-evidence). Host startup #2 separately discards 14.1929 ms wall time; host adapter max 5.6332 ms is not full-host execution. [Actual raw CSV/SVG and all ruler/sample/origin/zoom explanations](physics/aircraft-fuel/COMPARISON.md#actual-visual-evidence-and-deferred-performance), [exact profiling follow-up sites](performance/FRAME_LOSS_REGISTER.md#m03-finite-fuel-client-callbacks--2026-10-24).

Changed fuel mass and uncontrolled cache/startup/admission conditions prevent a matched rendering-cost comparison with the earlier engineering run. No speedup/optimization, resource-step CPU-cost baseline, representative coast/smoke five-minute 60 FPS, Ultra/base-GTX-1660 or capacity qualification is claimed. The user's new Ultra target and planned large map are future [M04/M05A/M08/M09 work](LARGE_MAP_AND_QUALITY_PLAN.md), not results. Optimization stays deferred.

## 2026-10-24 — M03 rendered engineering aircraft view

Actual Windows Mono release client **1920×1080 Balanced / VSync=1 / per-process D3D11**, one dry aircraft (144 imported renderers), 31 synthetic primitive surfaces and engineering HUD, one headless observer plus dedicated fire/wind/aircraft host. Editor closed; i5-9400F / GTX 1660 SUPER / driver 32.0.16.2101 / Windows 11 build 26300 / 16,323 MB Unity-reported RAM. Five rendered seconds warm up, **1448 intervals / 24.433480 s** retained. Average/1% low **59.262945 / 25.649861 FPS**, p95/p99 callback spacings **17.394099 / 17.924000 ms**. Peak Unity allocated bytes **482,846,054**.

**FL-08:** max **#499, 178.722903 ms**, recording **8.450635–8.629358 s**; second **#243, 165.068999 ms**, **4.038424–4.203493 s**. 736 intervals exceed 16.667 ms; two exceed 33.333 ms. These are observed main-camera callback intervals, not execution or confirmed dropped-present counts. CPU/GPU execution, draw calls/VRAM, allocations/GC, RTT/bandwidth and complete host/fire execution remain unavailable. No representative populated/coastal/smoky five-minute qualification or 60 FPS pass; p95 exceeds its ruler. No measured optimization claim.

The first rendered attempt was rejected by the independent view-trace checker for a one-frame focus-label mismatch and lacked actual late admission. Its **1733 intervals / 29.682669 s**, 58.384237 average / 15.775237 1% low, p95/p99 17.179800/17.757099 ms and **418.062896 ms at #1 (0–0.418063 s)** remain separately recorded. 908/5 intervals exceed the 16.667/33.333 ms rulers. Different startup/admission/cache conditions prevent treating attempts as a matched speedup comparison. The prior static BEFORE is 720p/different scene/camera; no matched rendering-impact baseline.

[Full source/hardware/hash/method evidence](reports/2026-10-24-m03-flight-view.md), [actual SVGs and complete interval/tick/warm-up/axis/zoom guides](previews/m03-flight-view/COMPARISON.md#performance-graph-guide), [exact locations and deferred profiling sites](performance/FRAME_LOSS_REGISTER.md#m03-engineering-view-callbacks--2026-10-24). Existing FL-06/FL-07, HOST-01 and NET-01 remain unresolved. Optimization remains deferred.

## 2026-10-24 — M03 live debug host and two headless client replicas

One actual Windows Mono dedicated player plus two real Netcode client players on loopback, all Null graphics; one dry estimated aircraft and the existing synthetic fire/wind fixture. Editor closed, i5-9400F / Windows 11 Pro 26300 / 16,324 MiB visible RAM. All **835 host callbacks / 14.001966 wall s** retained, no warm-up excluded; 2797 aircraft steps (13.985 physical s), fire tick 69. Application catch-up discards **0.0132569 wall s** once. Independent checker matches **154 received aircraft snapshots** to server publications.

`FlightAuthority.Advance` Stopwatch p95/max **0.0633 / 6.3483 ms**, maximum at callback **#1**. This is the bounded adapter (wind/validation/contact/flight substeps), not total host execution. Callback-gap p95/max **18.3073 / 51.6976 ms**; maximum **#2**, interval **0.0265593–0.0782569 s** after adapter Start. Carried remainder plus that gap triggers the one cap event (**HOST-01**). Startup and proof-only trace I/O remain included. No synchronized attribution, optimization or equivalent previous live-host baseline exists.

FPS/1% low/GPU/draw calls/VRAM are not applicable to headless players; full CPU stages/GC/allocations/RTT/network bytes unavailable. This local short proof is not impaired-network/capacity or representative rendering qualification. [Raw traces, hardware/build identities, actual SVGs and full axis/tick/warm-up/sample guide](physics/live-flight-host/COMPARISON.md#measured-timing-and-graph-guide), [exact event and deferred profiling sites](performance/FRAME_LOSS_REGISTER.md#m03-live-host-catch-up-observation--2026-10-24). FL-06/FL-07 and NET-01 remain unchanged.

## 2026-10-24 — M03 isolated runway-contact Step diagnostic

Two separate Windows Mono dedicated players on i5-9400F / Windows 11 Pro build 26300 / 16,324 MiB visible RAM (CIM), Editor closed. Each times 1000 independent three-wheel-contact Step calls after 100 explicit warm-ups, state reset per call, physical step 5 ms. Actual p50/p95/p99: **7.1/7.2/7.6 µs** (run 1) and **7.1/7.3/7.4 µs** (run 2). Maxima **61.0 µs at #1** and **98.9 µs at #322**. These are entire function elapsed observations, not complete host ticks, render intervals or dropped presentations.

No equivalent prior contact-cost baseline; no optimization, 60 FPS or capacity claim. CPU substage attribution, allocations/GC, full host timing and network traffic unavailable. FPS/GPU/draw calls/VRAM are not applicable to Null graphics. [Actual cases/raw timing arrays, SVG and plain-language axis/tick/warm-up/call guide](physics/runway-contact/COMPARISON.md#measured-call-timing-and-graph-guide). FL-06/FL-07 observations and deferred optimization remain unchanged.

## 2026-10-24 — M03 static aircraft preview callback diagnostic

One 1920×1080 Balanced/VSync=1 D3D11 release client and one local headless server; Editor closed, i5-9400F / GTX 1660 SUPER / Windows 11 26300 / driver 32.0.16.2101 / 16,323 MB visible RAM. Five rendered seconds warm-up; **1489 intervals / 25.000917 s**, 59.558 average FPS, 32.506 1% low (mean slowest 15 intervals), p95/p99 **16.9248/17.3213 ms**. Maximum **206.632495 ms**, sample **#2**, recording **0.017872–0.224504 s**. 744 intervals exceed 16.667 ms; one exceeds 33.333 ms. These are callback spacings, not execution times or dropped-presentation counts.

Peak Unity allocated bytes 475,032,553; CPU/GPU execution, draw calls, VRAM, GC, bandwidth and full host tick counters unavailable. Static pad/one aircraft only, no representative coast/smoke/flight workload; single short run, no matched BEFORE. p95 exceeds the 60 Hz ruler. **No 60 FPS acceptance, improvement or capacity claim.** [Actual screenshot, raw CSV/marked positions/SVG, build IDs, environment, definitions and complete graph guide](previews/m03-cl415-prototype/COMPARISON.md#performance-diagnostic-and-graph-guide), [FL-07 deferred attribution](performance/FRAME_LOSS_REGISTER.md#m03-static-aircraft-callback-overrun--2026-10-24).

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
