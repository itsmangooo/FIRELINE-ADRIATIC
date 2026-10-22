# Frame-loss investigation register — M00

**2026-10-22 · Locations measured; cause attribution and optimization deferred at the user's request.**

The largest recorded hitch is **83.47170 ms at sample #2**, from **0.022026 to 0.105498 seconds** relative to the start of the first stored interval. Three of 896 intervals exceed 20 ms. This report marks slow callback intervals, not confirmed dropped display frames: no presentation-loss counter, CPU timeline or GPU timestamps were captured.

## Recording and scope

This is offline analysis of the accepted M00 diagnostic already published in implementation commit `3d006ae1aecb8ba15a850d66c7a5aae96fc0c943` and indexed at checkpoint `7e11ca9`. There is **no new benchmark run or runtime change**. [Original conditions and raw artifacts](2026-10-22-m00-calibration.md): Unity 6000.6.7f1 / HDRP 17.6.0, Windows x64 Mono release client, i5-9400F / GTX 1660 SUPER, driver 32.0.16.1742, 16,323 MiB RAM, Windows 11 build 26300, 1280×720 High Fidelity, VSync=1. Two rendered clients, one headless server, the Editor and ordinary desktop applications shared the machine. The fixed-camera scene contains 26 calibration boxes and starter lighting/sky; no aircraft, fire or smoke.

The first client recorded 896 chronological main-camera completion intervals totaling 15.000975 s after approximately five rendered seconds of warm-up. The CSV contains durations only. Its reconstructed time origin is the **start of the first stored interval**, not application startup or an exact event timestamp. That first interval straddles the five-second sampling threshold; adding exactly five seconds would imply unavailable precision. There is no camera route or spatial hotspot to identify in this static scene.

## Marked timeline

![Measured callback intervals with the three intervals above 20 ms labeled](m00-frame-loss/frame-interval-timeline.svg)

[PNG fallback](m00-frame-loss/frame-interval-timeline.png) · [Every sample, reconstructed times and threshold flags](m00-frame-loss/marked-intervals.csv) · [Machine-readable analysis summary](m00-frame-loss/analysis-summary.json)

### How to read the first graph

- **Horizontal axis: elapsed recording time, in seconds.** The tick marks `0, 2, 4, …, 14` are two seconds apart. Zero means the start of the first stored interval, after roughly five seconds of rendered warm-up; it does not mean the game just launched. These graph ticks are ruler markings, **not server simulation ticks**.
- **Vertical axis: callback interval, in milliseconds.** An interval is the time gap between two consecutive main-camera render-completion callbacks. For example, 20 ms is 0.020 seconds. A taller spike means a longer gap, so the client updated its rendered view less smoothly at that moment. The gap includes rendering, waiting and scheduling; it is not a direct measurement of GPU work.
- **Dashed brown line: the 60 FPS target spacing.** Sixty evenly spaced frames each second leaves about **16.67 ms per frame**. A point near that line is near the target spacing. The dotted red line at **33.33 ms** marks twice that budget; it does not count dropped display frames.
- **Red dots and labels: the three intervals longer than 20 ms.** `#2` means the second recorded interval, not the second second of the run. Its label says **83.47 ms** because that one gap lasted about five target frame intervals. Its point is drawn at the interval's end, **0.105 s** into the recording. The table below also gives its start time; the capture does not prove which engine operation caused it.
- **Lower panel: a closer view of the same recording.** Its vertical scale spans only 14–21 ms, making smaller variations visible. It is not a second test. The two longest intervals extend above this cropped scale; the top panel shows their full height. Red dots in this detail panel mark the remaining members of the slowest nine samples.

The full recording still averages **59.73 FPS**. One large spike can make motion stutter even when that average is close to 60; this is why the report also preserves the slow intervals and 1% low. No exact number of dropped presentations can be read from these callback timings.

The following are the nine slowest intervals, listed in chronological order. CSV data-row numbering is one-based, excluding its header. Times are reconstructed from the full-precision samples; displayed values are rounded.

| Sample | Interval start (s) | Interval end (s) | Duration (ms) | Location / observation |
|---|---:|---:|---:|---|
| #1 | 0.000000 | 0.022026 | 22.02620 | Start of recording; above 20 ms |
| #2 | 0.022026 | 0.105498 | **83.47170** | Largest hitch; only interval above two 60 Hz budgets |
| #64 | 1.117308 | 1.135434 | 18.12660 | Isolated smaller overrun |
| #91 | 1.567877 | 1.586385 | 18.50830 | Isolated smaller overrun |
| #115 | 1.967366 | 1.985725 | 18.35930 | Isolated smaller overrun |
| #216 | 3.650318 | 3.670500 | **20.18150** | Third interval above 20 ms; cause unknown |
| #701 | 11.734155 | 11.752533 | 18.37810 | Near another smaller overrun at #703 |
| #703 | 11.766961 | 11.785278 | 18.31750 | #702 separates these two overruns |
| #851 | 14.233696 | 14.251761 | 18.06480 | Late smaller overrun |

| Strict threshold | Intervals exceeding it | Interpretation |
|---|---:|---|
| 16.66667 ms (60 Hz budget) | 444 / 896 | Includes tiny scheduling/VSync fluctuations around the budget |
| 18 ms | 9 / 896 | Exactly the slowest nine in this run |
| 20 ms | 3 / 896 | #1, #2 and #216 |
| 33.33333 ms (two budgets) | 1 / 896 | #2 only |
| 50 ms | 1 / 896 | #2 only |

An interval just above 16.66667 ms is not evidence of an entire missed refresh. The 83.47170 ms interval spans approximately 5.01 nominal budgets, but this does not establish how many frames the display actually dropped.

## Definitions and verification

Let $d_i$ be the numerical duration of callback interval $i$ in milliseconds, $t_i$ its reconstructed end time in seconds, and $N$ the sample count. The budget and chronological reconstruction are:

$$
B = \frac{1000}{60}\,\mathrm{ms} \approx 16.66667\,\mathrm{ms},
\qquad t_0=0,\qquad t_i=\frac{1}{1000}\sum_{j=1}^{i}d_j.
$$

Average FPS and the recorded 1% low definition use the arithmetic mean of intervals, with $d_{(j)}$ sorted from shortest to longest and $m=\lceil 0.01N\rceil$:

$$
\mathrm{FPS}_{\mathrm{avg}}=\frac{1000N}{\sum_{i=1}^{N}d_i},
\qquad
\mathrm{FPS}_{1\%}=\frac{1000m}{\sum_{j=N-m+1}^{N}d_{(j)}}.
$$

For this run $N=896$ and $m=9$. Recomputed results match all six recorded summary fields (sample count, duration, average, 1% low, p95 and p99), within relative tolerance $10^{-10}$ or absolute tolerance $10^{-9}$ in each field's unit: **59.72945 FPS average, 38.22727 FPS 1% low, p95 17.36010 ms, p99 18.06480 ms**. Percentiles use nearest rank. The script rejects invalid durations or a mismatch against the recorded JSON before writing derived evidence.

As a sensitivity check only, removing the first two intervals leaves 894 intervals averaging 60.01822 FPS, with maximum 20.18150 ms. This describes their influence; it does **not** replace the full-run result or demonstrate an optimization. No matched BEFORE benchmark exists, and representative 1080p acceptance remains open.

## Source locations to revisit later

| ID / priority | Observed issue and source location | Confidence / missing evidence | Deferred investigation |
|---|---|---|---|
| FL-01 · first | #1/#2 at recording start. [`ClientProbe.Capture`](../../Assets/Fireline/Client/ClientProbe.cs), lines 80–94, waits for end of frame, reads back a screenshot texture, encodes PNG and writes synchronously. Capture scheduling and metrics both use the five-rendered-second threshold (lines 50–54 and 75–76). | Source path and timing threshold are confirmed. **Capture causing #2 is a hypothesis**: no timestamp aligns readback/encoding/I/O with a particular sample. | Add timestamped capture markers; compare equivalent runs with capture disabled and enabled; inspect main/render-thread and GPU timelines before changing code. |
| FL-02 · next | #216 at 3.650318–3.670500 s and the smaller overruns listed above. | Location confirmed; no CPU, GPU, GC, disk or scheduling attribution recorded. | Correlate repeated intervals with CPU/GPU counters, GC events and presentation waits. Do not assign a rendering pass or model to these samples yet. |
| FL-03 · measurement overhead | [`OnCameraRendered`](../../Assets/Fireline/Client/ClientProbe.cs), lines 69–77, checks `Camera.main` and samples every callback. [`FoundationMetrics.Sample`](../../Assets/Fireline/Client/FoundationMetrics.cs), lines 17–21, grows a list and queries Unity allocated memory per interval. | These operations execute in the measured path. Their cost and any list-growth/GC contribution were **not measured**. | Compare sampling configurations and record allocation/GC markers; preserve render validation while isolating instrumentation overhead. |
| FL-04 · run isolation | Two visible clients, a server, the Editor and desktop processes compete for the same machine; VSync=1. | Run conditions confirmed; CPU/GPU contention or VSync waits causing a specific hitch remain unproven. | Repeat with one rendered client, then the original two-client conditions, on the same hardware/preset; record background load and presentation timing. |
| FL-05 · cold startup | A prior rejected run produced an unready white capture during initialization; it is outside this accepted warm sample. | Startup readiness issue observed previously; no duration or pass attribution was recorded. | Profile startup separately, with first-render/capture-ready timestamps; do not mix cold initialization with steady-run FPS. |

`FoundationMetrics.Save` (lines 24–50) sorts and writes files when the smoke run ends, **after** the collected intervals. Those final CSV/JSON writes cannot explain earlier spikes in this dataset. No implemented fire, aircraft, water or terrain-streaming system exists to blame for these calibration timings. Dedicated-server tick execution time remains uninstrumented.

## Reproduction and deferred work

![Implemented offline evidence-analysis flow](m00-frame-loss/analysis-flow.svg)

[PNG flowchart fallback](m00-frame-loss/analysis-flow.png). The [generator](../../scripts/analyze_frame_intervals.py) retains editable source for both vector drawings and recomputes all marked samples from the original CSV. Run from repository root with Python 3.10+:

```powershell
python scripts/analyze_frame_intervals.py
```

Core analysis uses the standard library. To reproduce the SVG/PNG drawings, install Matplotlib into the ignored tools directory (no Unity packages change):

```powershell
python -m pip install --target Builds/AnalysisTools matplotlib==3.11.2
$env:PYTHONPATH = Join-Path $PWD 'Builds/AnalysisTools'
python scripts/analyze_frame_intervals.py --plot
```

This report was generated with Python 3.14 and Matplotlib 3.11.2. The summary includes a SHA-256 of the CSV normalized to UTF-8 without BOM and LF newlines. Original CSV/JSON evidence is unchanged. Drawings were visually inspected; 141 local links across 50 Markdown files, required README sections, code/display-math delimiters and SVG XML were checked. Every derived CSV row and reconstructed time was verified; a deliberately mismatched metrics JSON was rejected. No new client/server build or unit/integration/network run is applicable to this offline report. Runtime screenshots and physics BEFORE/AFTER comparisons are not applicable because no scene, gameplay or physical model changed; the plots are data visualizations, not game captures. Asset/provenance records are unaffected.

- [x] Locate slow intervals and preserve an annotated dataset plus rendered vector timeline.
- [x] Recompute the accepted metrics and document source suspects without treating them as proven causes.
- [ ] **Deferred:** perform FL-01–FL-05 attribution runs when optimization work resumes.
- [ ] **Deferred:** choose and implement optimizations only after attribution; compare equivalent BEFORE/AFTER runs.
- [ ] Collect the representative five-minute 1080p preset matrix and missing CPU/GPU/server/bandwidth counters under [M00](../../TASKS.md).

M00 remains **In Progress**. This register does not unlock future gameplay milestones or waive performance acceptance.
