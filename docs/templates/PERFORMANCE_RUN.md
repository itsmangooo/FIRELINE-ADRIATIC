# Performance run — TBD change

**Unfilled template — not benchmark evidence.**

- Date / operator / milestone: TBD
- BEFORE and AFTER revisions, build IDs and build type: TBD
- CPU, GPU/VRAM, RAM, storage, OS, graphics driver: TBD
- Unity/package versions: TBD
- Resolution, native/upscaled mode, preset, VSync/frame cap: TBD
- Scene and camera route; coastal/populated/smoke conditions: TBD
- Player/client, aircraft, vehicle, active fire-cell and other entity counts: TBD
- Weather, seed, initial state, tick/time step and configuration: TBD
- Measurement tools/versions, warm-up, sampling duration, repeats and commands: TBD
- Definition of 1% low FPS and aggregation/percentile method: TBD; do not silently substitute reciprocal p99 frame time.
- Raw captures/data paths, plots and generating commands: TBD
- Embed rendered SVG plots/flowcharts with labeled axes/units and real data; retain compact editable generators. Use correctly typeset GitHub math or SVG formula fallbacks where necessary.
- Plain-language graph guide before final milestone commit: axes/units and tick spacing; recording origin/warm-up; what an interval/sample measures; target lines, labels and zooms. Distinguish ruler ticks from server simulation ticks and callback intervals from CPU/GPU timings or actual presentation loss: TBD

## Equivalent measurements

| Metric | Unit / statistic | BEFORE | AFTER | Difference |
|---|---|---|---|---|
| Average FPS | frames/s | Not measured | Not measured | Unavailable |
| 1% low FPS | frames/s; method above | Not measured | Not measured | Unavailable |
| CPU frame time | ms; main/render, p50/p95/p99 | Not measured | Not measured | Unavailable |
| GPU frame time | ms; p50/p95/p99 | Not measured | Not measured | Unavailable |
| Draw calls | per frame; stated statistic | Not measured | Not measured | Unavailable |
| Memory / VRAM | MiB; peak | Not measured | Not measured | Unavailable |
| Fire simulation | ms/step; p50/p95/p99 | Not measured | Not measured | Unavailable |
| Dedicated server tick | ms/tick; p50/p95/p99 | Not measured | Not measured | Unavailable |
| Network bandwidth | KiB/s; per client and total, in/out | Not measured | Not measured | Unavailable |

## Interpretation and limits

TBD equivalence of conditions, run variance, regressions, unavailable counters and their reasons. Record server hardware separately if different. Do not claim improvement from missing or incomparable runs. Link graphs when they clarify time-series spikes or distributions.

TBD outcome against the 1080p/60 FPS target and representative-scene p95/p99 criteria in `docs/PERFORMANCE_60FPS.md`. Editor observations cannot pass the built-client gate. Client FPS cannot establish server capacity.

## Follow-up and publication

- Missing measurements and unchecked tasks: TBD
- Link this report from performance history, README, changelog and development status: TBD
