# M02 — Live fire replication and explicit checkpoint operation

**Current state: Verified for the bounded Windows proof; see the [live evidence report](reports/2026-10-23-m02-replication.md).** This guide describes the implemented commands; only the report's completed checks establish verification. Existing Unity 6000.6.7f1/HDRP 17.6.0 and Netcode/Entities/Transport 6.6.0 are retained.

## Build and run

Run from the repository root with the project Editor closed:

```powershell
$env:FIRELINE_BUILD_EVIDENCE='docs/reports/m02-evidence/builds'
unity test "$PWD" --editor-version 6000.6.7f1 --mode EditMode --filter Fireline.Tests --output Builds/M02Start/editmode.xml --timeout 600 -- -buildTarget Win64 -nographics
unity run "$PWD" --editor-version 6000.6.7f1 --timeout 1800 --log-file Builds/M02Start/server-build-headless.log --no-tail -- -buildTarget Win64 -nographics -executeMethod Fireline.Authoring.FoundationBuild.BuildServer
unity run "$PWD" --editor-version 6000.6.7f1 --timeout 1800 --log-file Builds/M02Start/client-build.log --no-tail -- -buildTarget Win64 -force-d3d11 -executeMethod Fireline.Authoring.FoundationBuild.BuildClient
```

The server target uses an empty scene and no client assembly. The graphics overrides avoid the D3D12 Editor device-removal failure recorded in the report; project API/preset settings are preserved. A build report must be fresh and successful, with nonempty server asset inventory; stale binaries/reports do not qualify.

For ordinary direct-IP operation, run the server and client in separate terminals. Checkpoint paths below are relative to the working directory; `Saves/` is ignored as operator-owned state:

```powershell
./Builds/WindowsServer/FirelineServer.exe -batchmode -nographics --fireline-bind 127.0.0.1 --fireline-port 7979 --fireline-world-save Saves/world.fls
./Builds/WindowsClient/FirelineClient.exe -force-d3d11 --fireline-connect 127.0.0.1 --fireline-port 7979 -screen-width 1280 -screen-height 720 -screen-fullscreen 0
```

The normal server starts the existing synthetic M01 fixture, not a production incident world. It saves every five wall seconds and at controlled shutdown when a destination is supplied. There are no player fire/water controls; the display is read-only. To load a saved world on a fresh server process, explicitly add `--fireline-world-load Saves/world.fls` alongside the save destination. Missing/corrupt requested input fails startup; leaving out the load flag deliberately creates a new synthetic world and does not silently load a file merely because it exists. Back up operator data before deliberately starting a new world at an existing save destination.

## Acceptance runners

```powershell
./scripts/Test-M02Replication.ps1 -Port 8070
./scripts/Test-M02Replication.ps1 -Impaired -Port 8090
./scripts/Test-M02Negative.ps1 -Port 8080
```

The first two runners each start two separate server processes sequentially, with three new rendered clients per phase. Initial proof has two clients present before the 5 Hz scripted scenario starts; the third joins after suppression. Each phase compares full canonical checkpoint SHA-256, saved state, server target, client output, process exits and captures. The second server explicitly loads the first server's saved world; no in-memory object is reused across those processes.

Impaired runs use real loopback UDP proxies with seeded 5% random loss and configured 80 ms constant delay per direction. A separate stale-base fault tests application repair; reliable transport loss is not falsely described as unordered RPC delivery. The negative runner deliberately blackouts a connected peer and requires exit 9, then corrupts a separate checkpoint and requires server exit 6 without overwriting a valid save. These expected failures are scoped negative-test successes, not successful operation under disconnected/corrupt conditions.

The proof-only scenario freezes at 120 fire ticks (24 simulated seconds) for an exact common comparison. Ordinary servers never freeze for this check. Eight live subscribers, at most eight fragments per subscriber/Update and 32 retained peer diagnostic records bound the prototype; production admission/interest/traffic scaling remains future work. Scores/inventory/event journals and automatic reconnect UX are not implemented.

## Artifacts and measurements

[Main report](reports/2026-10-23-m02-replication.md), [before/after display report](previews/m02-replication/COMPARISON.md), [source code](../Assets/Fireline/Server/ServerFireReplication.cs), [test harness](../scripts/Test-M02Replication.ps1). The display report contains inspected actual runtime captures. Jittered/cold startup failures remain NET-01, as documented in the main report; they are not passing acceptance cases.

Raw Unity logs remain ignored under `Builds/`. Publish only filtered FIRELINE markers and sanitized structured evidence; do not copy command-line authentication data. Runtime hashes identify managed assemblies as well as launchers. Callback CSVs describe concurrently rendered diagnostic clients, not pure execution/dropped presentations or a one-client 1080p capacity/performance gate. Read the report's graph guide before interpreting ticks or intervals. Physics equations and historical M00/M01 evidence are unchanged.
