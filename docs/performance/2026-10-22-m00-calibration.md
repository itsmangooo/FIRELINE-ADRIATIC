# M00 calibration diagnostics — 2026-10-22

**Scope: short diagnostic run; no performance acceptance or improvement claim.**

## Hardware and conditions

- Intel Core i5-9400F (6 cores/6 threads), NVIDIA GeForce GTX 1660 SUPER, 16,323 MiB reported RAM, Windows 11 build 26300.
- Windows GPU driver 32.0.16.1742; see [hardware inventory](../reports/m00-evidence/hardware.json). The raw metrics JSON's `driver` field contains Unity's graphics API/feature-level string, `Direct3D 12 [level 12.1]`, rather than the Windows driver revision.
- Windows x64 Mono non-development client, 1280×720, High Fidelity preset, VSync=1, client target-frame-rate left at Unity's default; no resolution scaling changed by this test.
- M00 calibration scene: 26 generated boxes, one directional light, starter sky/volumes, fixed camera at (12, 9, −17) m / (24°, 326°, 0°), FOV 55°. No fires, smoke, terrain streaming, aircraft, vehicles or player avatars.
- Two separate rendered clients plus one dedicated server on the same machine, loopback port 7979. The Editor and ordinary desktop applications remained open. This is not an isolated single-client benchmark.
- Client timer starts at first real main-camera render callback; five rendered seconds warm-up, then **896 frame intervals totaling 15.00098 s** within the 20-second rendered smoke interval. Screenshot readback/encoding and diagnostic allocation sampling are included in the run and can cause spikes.

## Observed sample

| Metric | BEFORE | AFTER diagnostic |
|---|---|---|
| Average rendered-callback FPS | Unavailable | 59.72945 frames/s |
| 1% low FPS | Unavailable | 38.22727 frames/s |
| Frame interval p95 | Unavailable | 17.36010 ms |
| Frame interval p99 | Unavailable | 18.06480 ms |
| Peak Unity allocated memory | Unavailable | 535,716,161 bytes (510.9 MiB) |
| CPU / GPU execution time | Not instrumented | Not instrumented |
| Draw calls / VRAM | Not instrumented | Not instrumented |
| Fire simulation execution time | Not applicable: no fire model | Not applicable |
| Dedicated server execution duration per tick | Not instrumented | Not instrumented |
| Network bandwidth | Not instrumented | Not instrumented |

Average FPS is 1,000 divided by mean callback interval in ms. 1% low FPS is 1,000 divided by the mean of the slowest `ceil(sampleCount × 0.01)` intervals (nine samples here). Percentiles use nearest rank. These intervals measure time between HDRP main-camera completion callbacks; they include scheduling/presentation waits and are not GPU timestamp measurements. Memory is Unity's allocated-memory counter, not total process memory or VRAM.

## Reproduction and evidence

Run the [M00 build/smoke instructions](../M00_BUILD_AND_TEST.md), then `pwsh -File scripts/Test-M00Smoke.ps1`. The first client captures metrics with `--fireline-metrics`; both client windows stay visible and partly exposed. Keep the scene, camera, hardware, preset, client count and desktop workload equivalent before making comparisons.

- [Raw metrics JSON](../reports/m00-evidence/client-diagnostic-metrics.json)
- [Raw chronological callback intervals CSV](../reports/m00-evidence/client-diagnostic-metrics.csv)
- [Accepted process markers and executable/runtime hashes](../reports/m00-evidence/smoke-summary.json)
- [Actual rendered image and comparison](../previews/m00-foundation/COMPARISON.md)

The initial hidden-window samples were rejected because update loops advanced without drawing. Another run captured an unready white frame during cold initialization; its short sampling window is not used here. The accepted run uses actual render callbacks and a render-based warm-up.

## Acceptance limits and outstanding work

No earlier standalone baseline was measured. The test is 720p, short, contains calibration geometry and runs two clients simultaneously. It does **not** pass the representative 1080p/60 FPS gate or establish 100-player server capacity. There is no claimed speedup or matched before/after result. A graph of these short diagnostic intervals would not resolve those missing workload/measurement conditions; raw samples are retained for later analysis.

- [ ] Build the representative populated/coastal/smoky benchmark scenes and record an equivalent pre-change baseline.
- [ ] Run a single-client 1080p preset matrix for at least five minutes per representative case.
- [ ] Instrument CPU/GPU execution timings, draw calls, VRAM, server tick duration and transport bandwidth.
- [ ] Separate screenshot/instrumentation overhead and background desktop load from benchmark measurements.
