# M03 isolated debug-flight verification

M03 is **In Progress**. This increment verifies a pure numerical solver in an opt-in dedicated-player harness; it does not implement a playable aircraft. [Equations, debug values, before/after diagrams and limitations](physics/debug-flight/COMPARISON.md), [actual verification report](reports/2026-10-24-m03-debug-flight.md).

Run from the existing repository root, project Editor closed:

```powershell
unity self-update --check --format json
unity test "$PWD" --editor-version 6000.6.7f1 --mode EditMode --filter Fireline.Tests --output Builds/M03Start/editmode.xml --timeout 900 -- -buildTarget Win64 -nographics -force-d3d11
$env:FIRELINE_BUILD_EVIDENCE='docs/reports/m03-flight-evidence/builds'
unity run "$PWD" --editor-version 6000.6.7f1 --timeout 1200 --log-file Builds/M03Start/server-build.log --no-tail -- -buildTarget Win64 -nographics -executeMethod Fireline.Authoring.FoundationBuild.BuildServer
./scripts/Test-M03Flight.ps1 -Port 8170
python scripts/analyze_m03_flight.py
python scripts/render_m03_flight.py
```

The Unity CLI skill requires the current CLI; the project editor/package versions stay fixed. Matplotlib is needed only for rendering report drawings, not solver/runtime/independent artifact checks. This worker's existing local plotting dependencies may be used with `$env:PYTHONPATH=(Join-Path $PWD Builds/AnalysisTools)`; that ignored directory is not a production dependency.

The runner starts **two distinct dedicated processes**, checks their actual exits and headless markers, then losslessly archives JSON/CSV and independently checks analytic vacuum motion, mass/CoG/inertia, all sampled attitudes/airspeeds/pressures, drag-power sign and exact same-runtime replay. Raw logs stay ignored under `Builds/M03Flight/`; only scoped FIRELINE markers are published. The proof uses `--fireline-flight-proof output.json`, has explicit failure exit **10**, and cannot share live save/load, fire/replication proof or smoke/benchmark exit modes. Client startup rejects this server-only option.

When the flight flag is absent, normal fire/replication startup remains active. Flight state is not persisted or sent to clients. The current aircraft licence/import gate remains in force; no asset is required for this headless path. Do not present these numerical trajectories or SVG plots as actual aircraft gameplay screenshots.
