# M00 foundation — actual visual captures

## BEFORE and AFTER

| BEFORE: original Unity Game View | AFTER: running Windows client |
|---|---|
| ![Starter Game View](starter_before.png) | ![Release client calibration scene](foundation_after.png) |

The baseline was captured on 2026-10-22 from the existing starter scene before M00 implementation at checkpoint `f93ad75`. AFTER was captured from the release client during the accepted local server/two-client smoke. Both images are actual Unity content at 1280×720. They use different camera positions and workloads and are not an equivalent performance comparison.

## What changed

The original `Assets/Scenes/SampleScene.unity` remains unchanged. A separate `Assets/Fireline/Scenes/M00_Client.unity` copies its HDRP lighting/sky and adds one ground box, 25 deterministic calibration boxes and one neutral HDRP/Lit material. The camera is at (12, 9, −17) m, Euler (24°, 326°, 0°), FOV 55°. The scene is a render smoke fixture with no Adriatic terrain, aircraft or gameplay.

BEFORE uses the original starter camera/settings from the preserved source scene. AFTER uses Windows Direct3D 12, the existing High Fidelity preset and a visible rendered client window. Capture waits five seconds after the first actual main-camera rendering callback. The screenshot was visually inspected: the ground, all rows of calibration boxes, lighting/shadows and sky are visible; there are no pink error materials.

## Assets and provenance

- Geometry: project-generated Unity cube primitives, no downloaded models or rigs.
- Material: `Assets/Fireline/Content/Foundation/Calibration.mat`, project-generated HDRP/Lit, base color (0.24, 0.32, 0.35), smoothness 0.25; no external textures.
- Lighting/sky: existing Unity HDRP starter assets, copied by reference, retaining their original Unity/template terms.
- Generator: `Assets/Fireline/Authoring/FoundationBuild.cs`; see [manifest](../../ASSET_MANIFEST.md) and [license notes](../../ASSET_LICENSES.md).

## Verification and performance limits

The [accepted smoke summary](../../reports/m00-evidence/smoke-summary.json) records two distinct connected/rendered client processes and a null-graphics dedicated server, all exiting with code 0. The accepted diagnostic frame sample averaged 59.73 FPS at 720p with two clients; see [performance report](../../performance/2026-10-22-m00-calibration.md). No matched baseline run or representative 1080p/60 FPS acceptance exists, and no performance improvement is claimed.

An initial hidden-window run was rejected because rendering was suppressed. A subsequent early screenshot was rejected as visually white during cold startup. The final probe starts its warm-up and duration at the first rendered frame; only the inspected final capture above is accepted evidence. No concept art or fabricated baseline is used.
