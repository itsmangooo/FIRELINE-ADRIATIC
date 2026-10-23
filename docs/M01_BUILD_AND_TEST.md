# M01 build, tests and headless proof

Uses the existing Unity 6000.6.7f1 project, HDRP 17.6.0 client and Netcode/Entities/Transport 6.6.0 foundation. No new package is required. [Model and limits](physics/fire-wind/COMPARISON.md); [M00 build isolation](M00_BUILD_AND_TEST.md).

## Tests and builds

On this licensed worker, run the full EditMode suite in the live Editor:

```powershell
unity command test run --caller plugin --skill unity-cli --project-path "$PWD" --mode EditMode --filter Fireline.Tests --format json
```

Wait for actual completion with 34 passing cases before building. An in-progress test status is not a pass. Set an evidence directory before invoking the existing build helper in Editor C#:

```csharp
System.Environment.SetEnvironmentVariable("FIRELINE_BUILD_EVIDENCE", "docs/reports/m01-evidence/builds");
Fireline.Authoring.FoundationBuild.BuildServer();
```

The helper produces a non-development Windows Mono dedicated server with clean packed-asset inventory, temporarily clears HDRP pipeline references and restores the Editor settings in `finally`. Preserve unrelated user asset edits; do not reset the project. A tool timeout does not establish build failure: inspect the fresh build JSON and Editor state before retrying. Linux uses the existing `BuildLinuxServer()` entry point after the target is switched, or the documented closed-Editor CLI path.

## Independent dedicated-server runs

Finish builds, close only this clean project Editor, then:

```powershell
pwsh -File scripts/Test-M01Simulation.ps1
$env:PYTHONPATH = Join-Path $PWD 'Builds/AnalysisTools'
python scripts/analyze_m01.py
```

The runner owns two serial `-batchmode -nographics` server processes on loopback port 7992, requires fresh 9-case/600-step reports and exit 0, verifies headless markers and compares every case digest across processes. It retains the first full raw JSON/CSV, both marker lists and executable/managed-assembly hashes. The second raw report remains under ignored `Builds/M01Proof/`. On timeout/failure it stops only its own process. `--fireline-sim-proof <path>` is a dedicated-server-only diagnostic entry point, not a public client control or mod interface.

Normal server startup also hosts the synthetic 32×32 M01 fixture and advances it at 5 Hz from the existing fixed-update loop. `FIRELINE_SIMULATION_STOP` records live ticks/digest. The fixture is not a production incident or map. Existing direct-IP client connection smoke may be rerun against the updated server; the M00 client cannot display the fire because no M02 state protocol or M04 VFX exists yet. Report the actual client binary used rather than implying it was rebuilt.

## Evidence interpretation

Numerical plots are not Game View screenshots. CPU timings measure accelerated `World.Step` execution, including delta creation; they exclude serialization, networking and the rest of the host update. The 120-step maximum-budget fixture records GC generation-0 collection counts and update-age backlog; this does not measure per-frame allocation, GPU, VRAM, transport bytes or full-host capacity. Do not infer client FPS or 100-player readiness. Both first-step/JIT effects and later samples remain in the data; no warm-up is discarded.

M01 exports detached snapshot/latest-step delta data and a store interface. It does not implement baseline delivery, delta application, restart storage, gap repair, admission/authentication or client convergence. Those are M02 work, and require their own real process evidence.

## Linux scope and reproduction

After a successful current Linux server cross-build, reuse the prepared workspace Ubuntu root described in the M00 guide:

```powershell
$m01WslProject = '/mnt/host/a/Unity Projects/FIRELINE ADRIATIC'
wsl -d docker-desktop -u root -- sh "$m01WslProject/scripts/Test-M01LinuxServer.sh" "$m01WslProject"
python scripts/verify_m01_linux.py
```

This exact path/distro describes the tested worker, not a portable native-host installation recipe. The shell mounts only the build/proc/device paths within the workspace root and unmounts them on exit. Both Linux replays passed; the gust heat proxy differs by at most 1.11e-16 from Windows, with identical phase grids in these cases. See the report for hashes and limitations. Same-runtime replay is verified; cross-OS bit identity is not promised.

The final closed-Editor test report is `docs/reports/m01-evidence/editmode-final.xml` (34 passed); the earlier live 32-case JSON remains historical evidence. M01 is Verified; the requested source-comment pass comes next; M02 then owns serialization/transport/recovery implementation.
