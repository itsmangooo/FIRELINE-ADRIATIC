# M03 actual real-time flight — performance diagnostic

**2026-10-26. Observed callback gaps recorded; optimization deferred.** Two actual local dedicated-server/rendered-owner/late-headless-observer groups complete an automated calm-air runway hop. These are short instrumented diagnostics, with no matched earlier rendered flight baseline. They do not qualify representative 60 FPS, Ultra, 100 players or a performance improvement.

## Conditions and results

Unity 6000.6.7f1 / HDRP 17.6.0, Windows Mono release builds, Editor closed; 1920×1080 windowed D3D11, existing Balanced preset, VSync=1, no requested upscaling. i5-9400F / GTX 1660 SUPER / 16 GB (Unity reports 16,323 MB), Windows 11 Pro 26300, driver 32.0.16.2101. [Fresh environment inventory](../reports/m03-realtime-evidence/environment.json), [build records and exact role hashes](../reports/2026-10-26-m03-realtime-flight.md#evidence-and-reproduction). Baseline source is `970d1f1`; the published commit contains the implementation and evidence. Original user sky/configuration is preserved and unstaged. Synthetic runway, one aircraft, zero water/wind, real local transport and existing model/materials; no populated coast or smoke/VFX workload. Capture, HUD, per-frame traces and per-accepted-step server recording remain enabled.

| Measured quantity | Accepted run | Independent actual repeat |
|---|---:|---:|
| Callback intervals after five rendered seconds warm-up | 3,675 | 3,715 |
| Recorded callback duration (s) | 62.268213 | 62.351185 |
| Average callback-derived FPS | 59.018877 | 59.581867 |
| 1% low callback-derived FPS | 20.534374 | 30.457950 |
| p95 / p99 callback interval (ms) | 17.657900 / 19.401200 | 17.466800 / 18.516300 |
| Peak Unity allocated memory (bytes) | 486,693,979 | 490,900,602 |
| Maximum timed aircraft `Advance` wall duration (ms) | 17.204800 | 6.767000 |
| Aircraft catch-up-limit events | 7 | 11 |
| Discarded aircraft wall time (s) | 0.107567 | 0.076853 |

Average FPS is interval count divided by summed interval seconds; 1% low is the reciprocal mean of the slowest ceil(1% × count) intervals. It is not reciprocal p99. Each run has its own distribution. Client spacing includes presentation/wait time; the server `Stopwatch` covers only aircraft `Advance`, including optional contact/JSON recording, and can include IO/preemption. It is not whole-server tick duration, thread CPU time or per-5-ms-step execution time. No water/physics kernel was changed. CPU/GPU execution, draw calls, VRAM, gameplay-fire execution time, complete server tick timings and bandwidth are unavailable because this fixture does not instrument them. Unavailable counters are not zero.

## Locations and graph meanings

![Actual callback intervals and zoom](../previews/m03-realtime-flight/callback-timing.svg)

X is wall seconds from the first retained interval start near five rendered seconds warm-up, with ruler labels 5 s apart. Each sample is the spacing between adjacent camera callbacks, plotted at its end. Upper Y is milliseconds with 25 ms ruler spacing and all peaks retained. The lower panel shows the same samples at 0–50 ms, rulers 10 ms apart. The maximum annotation identifies accepted sample #3669: 62.003539–62.179461 s, **175.921202 ms**. Dashed/dotted 16.667/33.333 ms budget rulers represent 60/30 Hz. Graph ruler marks are separate from 5 ms aircraft simulation ticks, nominal 50 ms request gates and roughly 100 ms pose publication.

Repeat peak: #1621, 27.151040–27.322450 s, **171.409801 ms**. [Every accepted marked interval](../reports/m03-realtime-evidence/runtime/callback-intervals.marked.csv), [every repeat interval](../reports/m03-realtime-evidence/repeat/callback-intervals.marked.csv), [FL-11 register](FRAME_LOSS_REGISTER.md#m03-actual-real-time-flight-callbacks--2026-10-26). Callback gaps are not proven dropped presentations. The [full physical/presentation/callback graph guide](../previews/m03-realtime-flight/COMPARISON.md#physical-presentation-and-callback-graph-guide) explains the distinct origins, sampling, axes and thresholds for every published plot. [Editable generator](../../scripts/render_m03_realtime.py), [reproduction commands](../M03_BUILD_AND_TEST.md#real-time-rendered-runway-route).

Later investigation: synchronized CPU/GPU/presentation traces around screenshot readback/PNG encoding, HUD/trace allocations and IO, interpolation/cameras, Netcode, server QA serialization, HDRP/driver/GC/OS and local process contention. Compare capture/recording enabled/disabled under matched conditions, then profile the representative scenarios in [PERFORMANCE_60FPS](../PERFORMANCE_60FPS.md). No current trace attributes a particular gap to a source site. Different startup, flight/camera policy, cache/admission and instrumentation prevent a matched comparison with earlier taxi/headless diagnostics. The rejected first flight attempt remains separate. FL-11/HOST-01/NET-01 and representative percentile gates remain open; M03 is not complete.
