# M03 water-release prerequisite — 2026-10-26

**Verified numerical increment; M03 remains open.** Pure C# release candidate/tank debit, 64 default representative parcels, wind/gravity/quadratic drag, segment-plane impact and mass-conserving bilinear fire coverage run in the actual opt-in dedicated proof. Live `FlightAuthority` water/actions are unchanged. [Actual SI formulas, BEFORE/AFTER drawings, trajectory/footprint/mass graphs, assumptions and limits](../physics/water-parcels/COMPARISON.md).

**128 EditMode + 3 PlayMode tests**, [actual live Editor unit results](m03-water-evidence/editmode.json), [async PlayMode verdict](m03-water-evidence/playmode.json); [source-comment audit](m03-water-evidence/source-comments.json) covers 62 C# files/479 members/five configurations. Initial already-open batch refusal and unsupported synchronous PlayMode request ran no tests; later actual live runs supply the verdicts.

Fresh Windows Mono dedicated build: **0 errors / 506 warnings**, 330.725 s, 141,588,972 bytes; [build record](m03-water-evidence/builds/server-build.json), [packed inventory](m03-water-evidence/builds/server-packed-assets.txt). Existing build helper was invoked through the already-open idle Editor, refusing dirty `Assets/` objects and preserving its previous environment override. No user Editor was closed or project recreated. Current client binaries are not rebuilt/qualified by this headless-only change.

Two actual dedicated OS exits **0/0**, [process records, raw hashes and three role DLLs](m03-water-evidence/numerical/process-summary.json). All seven water cases replay exactly. [Independent water/negative/conservation checks](m03-water-evidence/verification.json), [unchanged original flight/fuel regression](m03-water-evidence/numerical/verification.json), [unchanged accelerated authority route](m03-water-evidence/route-verification.json), [final preservation/link/archive audit](m03-water-evidence/final-verification.json). Both complete raw JSON results are losslessly archived. They retain every first-parcel step, all 64 initial/final parcels, actual coverage/cells and all timing samples. The checker reconstructs every parcel independently, not just the success flag or one trajectory.

| Case | Last impact (s) | Centroid east X (m) | Centroid north Z (m) | Cells | Retained kg |
|---|---:|---:|---:|---:|---:|
| low_calm | 2.982732 | 80.000000 | 106.847527 | 4 | 2000.000000000 |
| high_calm | 6.755274 | 80.000000 | 123.154241 | 4 | 2000.000000000 |
| low_crosswind | 2.985136 | 95.449919 | 106.566563 | 6 | 2000.000000000 |
| high_crosswind | 6.758032 | 126.293166 | 122.695769 | 6 | 2000.000000000 |
| fine_high_crosswind | 6.763407 | 126.285684 | 123.069377 | 6 | 2000.000000000 |
| tilted_spinning | 4.716545 | 109.192350 | 117.128739 | 4 | 2000.000000000 |
| vacuum | 2.231957 | 80.000000 | 174.606328 | 6 | 2000.000000000 |

Maximum tank-to-grid balance error **4.55×10⁻¹³ kg**; high-crosswind centroid changes **0.373682 m** at half dt. Unknown-cell/invalid release paths reject atomically; five intentionally corrupted tank/launch/wind/impact/coverage records are rejected. These are synthetic planar/instantaneous-release tests, not validated CL-415 footprints or a completed scoop/drop operational loop.

An independent continuous-vacuum ballistic reference differs from the actual 10 ms numerical impact by **0.004580305 s / 0.274818313 m**; both finite-step gates pass. The comparison retains this numerical error and the lack of breakup/evaporation/real terrain rather than claiming calibrated accuracy.

## Performance scope

[Fresh hardware/OS inventory](m03-water-evidence/environment.json): i5-9400F / GTX 1660 SUPER / 16 GB, Windows 11 Pro 26300/driver 32.0.16.2101; headless graphics, no display preset/resolution/upscaling, Editor open/background desktop active. Separate per-process Stopwatch microdiagnostic, 100 warm-ups then 1,000 calls of 64 actual point steps with supplied constant (8,0,0) m/s wind. No wind-field sampling, creation/raster/fire/serialization/transport/rendering in its timer.

| Process | Mean batch ms | p95 ms | Max ms | Gen-0 collections |
|---|---:|---:|---:|---:|
| Process 1 | 0.0203563 | 0.0203000 | 0.0935000 | 0 |
| Process 2 | 0.0228097 | 0.0282000 | 0.4130000 | 0 |

This is not thread CPU time, full-server tick cost, a server-capacity/FPS pass or a matched speedup. FPS/1% low/frame CPU-GPU/draw calls/VRAM/full-host/fire-execution/bandwidth counters are unavailable/not applicable here, never zero. There is no new camera trace; existing FL-11/HOST-01/NET-01 remain open. [All published graph axes, rulers, clocks, sampling and warm-up meanings](../physics/water-parcels/COMPARISON.md#rendered-numerical-graphs-and-reader-guide). [Commands](../M03_BUILD_AND_TEST.md#water-parcel-prerequisite).

Baseline `4dc96fec9074ab600963ef615cd595866a0c789d`, checkpoint `codex/checkpoint-m03-water-parcels-20261026`. Unity 6000.6.7f1/HDRP 17.6.0/CLI latest 1.0.0-beta.14 unchanged. Five original configuration hashes/user sky preserved; existing force/contact/fuel/fire/wind/authority/client/shared/authoring/model/scene/package code/assets unchanged. Only the existing opt-in flight proof gains water output. No new RPC field or package/asset/download/license/preset change. Actual water-VFX screenshots are not applicable because there is no render/UI/VFX change; no water screenshot or earlier implementation baseline is fabricated.

Next: actual live release/tank/event scheduling and time-distributed flow, then scoop validity/intake/water geometry, terrain/dispersion/calibration and replicated suppression/controls/VFX evidence. Rotor/propeller animation is recorded as speed-dependent final polish, with pivots/engine-signal mapping still pending. M03 remains In Progress; no future milestone starts.
