# M00 client/server build and testing foundation

Use Unity **6000.6.7f1** with the relevant Windows/Linux player and Dedicated Server support modules. Build entry points use x64 Mono, non-development players and strict build errors. Unity automatically installed Linux SDKs/toolchain **1.1.0** when the Linux module was exercised; [metadata and compatibility audit](reports/m00-coastal-evidence/linux-toolchain-audit.json). The original SampleScene and existing build scene list are preserved; the build script supplies its own explicit scene list. HDRP is a client dependency. No cloud service or paid lobby is required.

## Existing live Editor

Use the latest Unity CLI (`unity self-update --check --format json`). Commands below run from the repository root and target its open Editor. Pass `--project-path` if working outside this root. Group names and verbs are separate arguments, not a single quoted argument.

```powershell
unity status --format json
unity command eval --caller plugin --skill unity-cli 'Fireline.Authoring.FoundationBuild.CreateScenes();'
unity command test run --caller plugin --skill unity-cli --mode editor --filter Fireline.Tests --filter_type assembly --async_tests true --format json
unity command test status --caller plugin --skill unity-cli --format json
```

Wait for completed test results with a nonzero test count and zero failures, then for Editor compilation/domain reload to finish. Copy the completed `Temp/pipeline_test_status.json` to the evidence directory. Creating scenes only adds missing M00 scenes; it refuses to run with dirty open scenes and restores the previous scene setup.

```powershell
unity command eval --caller plugin --skill unity-cli --timeout 1800 --detach 'Fireline.Authoring.FoundationBuild.BuildServer();'
# Wait for docs/reports/m00-evidence/server-build.json from this invocation.
# Confirm Succeeded, errors=0 and containsVisualAssets=false.
unity command eval --caller plugin --skill unity-cli --timeout 1800 --detach 'Fireline.Authoring.FoundationBuild.BuildClient();'
# Wait for this invocation's client-build.json and verify Succeeded/errors=0.
pwsh -File scripts/Test-M00Smoke.ps1
```

Do not queue builds together or during compilation. Detached job acceptance is not build success; job IDs can be lost during a domain reload. Check fresh report timestamps and `Logs/Editor.log` if a report is missing. Build output is under ignored `Builds/WindowsServer/` and `Builds/WindowsClient/`; reports and packed-asset inventories are under [m00-evidence](reports/m00-evidence/).

Unity's cached incremental reports can omit packed-asset entries. An empty inventory is unavailable evidence, not proof of isolation; the batch script rejects an empty server inventory. Obtain a full clean-build report before certifying server asset isolation in that case. The verified M00 server inventory is nonempty. The final incremental client report has no packed entries; its HDRP rendering is demonstrated by the separate runtime/capture evidence instead.

## Batch / CI entry point

With this project closed in Unity:

```powershell
pwsh -File scripts/Build-M00.ps1 -Smoke
```

This runs filtered EditMode tests (currently **20**, all must pass), builds both roles, validates fresh reports and runs the local three-process smoke. A CI worker needs a licensed Unity 6000.6.7f1 installation and the listed modules; rendered client smoke also needs an interactive Windows graphics session/GPU. Headless runners can omit `-Smoke` and run build/tests, but cannot claim graphical smoke acceptance. The script refuses to operate alongside an open project rather than closing it. No hosted CI runner, Unity license secret or automated GitHub run is provisioned by M00.

The closed-Editor local path has now been exercised, including Windows client/server, Linux x64 server and the Windows three-process smoke. Preserve earlier diagnostic artifacts by selecting a new evidence directory:

```powershell
pwsh -File scripts/Build-M00.ps1 -LinuxServer -Smoke -EvidenceRoot docs/reports/m00-coastal-evidence/batch
```

`FIRELINE_BUILD_EVIDENCE` provides the build helper's output root; the wrapper restores its previous environment value on exit. Server builds request `CleanBuildCache` to obtain a full asset inventory. Existing inference resources still compile and some ship even with null graphics; this is not complete GPU-resource stripping. Native Linux hosting, Linux two-client networking and load testing remain separate from the verified WSL negative startup test.

Archive `Builds/CI/`, `Builds/Smoke/`, build reports and test results as CI artifacts. Never put licenses/tokens into the repository. Re-run checks on the exact source/configuration to be released.

## Coastal benchmark client

`FoundationBuild.BuildBenchmarkClient()` builds the separate coastal workload into `Builds/WindowsBenchmark/FirelineBenchmark.exe`. It temporarily enables frame-timing statistics for that player, restores the user's setting, and copies third-party notices/licences into `Legal/`. Ordinary scene inspection does not accumulate benchmark samples. With the project Editor closed:

```powershell
pwsh -File scripts/Test-M00CoastalBenchmark.ps1
```

This runs one visible release client at 1920×1080, Balanced, clear then synthetic smoky conditions. Each case captures an image at five rendered seconds, warms up for 30 rendered seconds, then samples for 300 seconds. Pass existing preset names with `--fireline-quality`; `--fireline-benchmark-seconds` accepts 300–900 and requires `--fireline-benchmark-output`, independently of the connection-smoke exit timer. `--fireline-benchmark-scenario` accepts `clear` or `smoky`. No server connection is required for the render-only benchmark, and the fog workload is not gameplay smoke.

CSV blanks mean unavailable counters. FrameTimingManager values can lag render callbacks; CPU frame time is not pure execution time. The proxy scene and these diagnostics do not pass production S1–S6 acceptance, prove a speedup or establish server capacity. [Implementation and limits](reports/2026-10-22-m00-coastal-foundation.md).

## Linux execution check

The Linux build entry point is `FoundationBuild.BuildLinuxServer()`. The verified local test used an official Ubuntu Base 24.04.5 archive under ignored `Builds/LinuxRuntime/` and the existing WSL2 kernel. [Official archive URL and verified SHA-256](reports/m00-coastal-evidence/linux/ubuntu-source.txt). The acceptance script creates a workspace-local root, temporarily mounts the built server/proc/minimal devices, executes a five-second headless/clock negative smoke, expects exit 4 because there are no clients, and unmounts its mounts on exit. It needs a Linux root-capable WSL test environment; it does not install a distribution or repair the host.

```powershell
wsl -d docker-desktop -- sh '/mnt/host/a/Unity Projects/FIRELINE ADRIATIC/scripts/Test-M00LinuxServer.sh' '/mnt/host/a/Unity Projects/FIRELINE ADRIATIC'
```

That path/distribution is specific to this machine. Use the actual checkout mount in another configured environment. The underlying Docker Desktop WSL distribution reported a read-only VHD fallback; the workspace-local Ubuntu test still succeeded. No system repair was attempted. [Observed environment, hashes and narrow result](reports/m00-coastal-evidence/linux/startup-summary.json).

## Direct-IP startup

```powershell
& './Builds/WindowsServer/FirelineServer.exe' -batchmode -nographics --fireline-bind 127.0.0.1 --fireline-port 7979
& './Builds/WindowsClient/FirelineClient.exe' --fireline-connect 127.0.0.1 --fireline-port 7979
```

The server binds loopback by default. Use an explicit LAN IPv4 address or `0.0.0.0` when intentionally self-hosting beyond the machine; firewall/NAT configuration and WAN behavior are unverified. The client stays disconnected without `--fireline-connect`. Authentication, admission controls and gameplay protocol validation are future prerequisites for public hosting.

`--fireline-smoke-seconds N` exits after N seconds (1–300): server time starts at probe startup; client time starts at the first actual rendered frame, with an extra 60-second absolute startup guard. Server smoke additionally requires `--fireline-min-clients N` distinct simultaneous connections at peak (default 2) and an advancing fixed clock. Client smoke requires a completed Netcode connection, actual HDRP main-camera render callbacks, a non-null renderer/pipeline and a saved screenshot when requested. Failed smoke returns exit code 4; invalid headless state returns 3. Normal runs have no automatic exit. `--fireline-capture <path>` saves a real rendered client frame after five rendered seconds. `--fireline-metrics <path>` records intervals between actual main-camera render completions after that warm-up; it does not count update-only loops or cold renderer initialization as rendered frames.

The smoke runner uses three distinct processes on loopback, validates their exit codes and markers, stores full logs locally, and commits concise evidence plus executable/runtime-assembly hashes. Its two interactive client windows must remain visible for genuine rendering and capture; the server stays hidden/headless. An initial hidden-client run was rejected after screenshot capture failed and update-only loops produced invalid frame samples. It stops only its own processes on failure. This proves startup and Netcode handshake compatibility, **not fire replication, gameplay authority, packet-loss recovery or 100-player capacity**.

## Build isolation and recovery

The dedicated-server build uses the empty `M00_Server` scene and `StandaloneBuildSubtarget.Server`. Client assembly is excluded under `UNITY_SERVER`; server assembly is excluded from standalone clients. Simulation has `noEngineReferences` and no assembly dependencies. Authoring and tests are Editor-only.

Netcode 6.6's build processor deletes its generated `Assets/netcode-build-assets-temp` folder outside the AssetDatabase. A subsequent build in the same Editor can observe stale folder registration and fail to create `script-order.asset`. The entry point ensures the physical temporary folder exists and imports it before building; package source is unchanged. The generated folder/meta are ignored by Git.

During server build, the build entry point temporarily removes default and per-quality pipeline references and restores them, quality selection, backend and subtarget in `finally`. Always review the resulting packed-asset inventory and runtime headless check. If the Editor is forcibly terminated during a build, compare project settings with the Git checkpoint before continuing; no crash-safe transaction is claimed.

Existing graphical presets remain available. The M00 client scene contains generated calibration geometry, not an Adriatic environment or representative performance workload. No flight, water, wind or combustion mechanics are implemented.

The existing Performant HDRP asset disables volumetrics. The coastal runner rejects `-Preset Performant -Scenario smoky`; run its clear case separately. Balanced/High Fidelity support the static fog fixture. No original quality asset is changed.
