# Technical architecture — Unity 6 HDRP, C#, headless server

## Architectural decision record (ADR-001, proposed)

- **Client:** Unity 6 HDRP, Windows desktop initially. HDRP lighting, terrain, VFX Graph, audio, native UI Toolkit/uGUI and player/vehicle presentation.
- **Authority:** standalone Unity Dedicated Server build for Windows/Linux, **no rendering or visual particles**. World is a pure domain/service boundary even if hosted in the Unity process.
- **Networking:** evaluate current supported *Netcode for Entities* and *Unity Transport* release for compatibility with HDRP/client presentation, physics authority, developer expertise and modding. **Unity's 100+ Megacity Metro sample uses URP, not HDRP**; use it as networking reference, not drop-in game architecture. Document resolved package versions before generating scenes/code.
- **Simulation:** C# authoritative world subsystem; fixed-step wind/fire and independent flight/vehicle fixed-step; pure functions and deterministic replay tests on a given server/build (floating point cross-platform exact bitwise determinism not promised).
- **Persistence:** PostgreSQL, versioned snapshots and operational events. Small private servers may choose a later tested SQLite backend; do not assume drop-in concurrency parity.
- **Modding:** versioned *data-only manifests* first; approved AssetBundles for visuals, maps and vehicles. No arbitrary client-supplied C# on servers.
- **Hosting:** direct IP/domain + optional listing service, owner-controlled saves and configuration. No mandatory vendor cloud tenancy.

## Separation of concerns

```text
Fireline/
  Assets/
    Fireline/
      Authoring/          # Unity authoring components, converters, inspectors
      Client/             # HDRP, camera, VFX, audio, views, UI
      Shared/             # shared DTOs, schema types, validation constants
      Server/             # authoritative world, gameplay, interest management
      Simulation/         # pure mathematics: flight, wind, water, fire, fuel
      Tests/              # EditMode, PlayMode, network integration, perf harness
      Content/            # approved imported assets, LODs and materials
      Scenes/             # minimal flight+fire+ground proof scenes
  docs/
  prompts/
  ServerConfig/
```

Use Unity assembly definitions (`.asmdef`) to prevent client presentation code leaking into server assemblies. Runtime `UnityEngine` types are allowed at outer adapters, while numerical simulation core should be isolatable and testable without scene objects. ECS conversion/baking is a boundary; avoid duplicating authoritative states in GameObjects and ECS entities.

## Domain contracts (initial)

- `IWorldClock`: monotonic game time, fixed-step tick IDs, server-controlled time-of-day; timezone only for reporting.
- `IWindField.Sample(positionMeters, altitudeMeters, tick) -> Vector3 m/s`.
- `IFireWorld.Step(dt, weather, fuel) -> FireDelta[]` and `ApplyWater(WaterCoverage)`; chunk revisions must be monotonic.
- `IAircraftSim.Step(input, dt, weather) -> rigid-body state` with SI units and payload; physics authority server-owned.
- `IWaterDropSolver.Calculate(aircraftState, nozzle/outlet, wind, dt) -> coverage field`.
- `IIncidentDispatch.Assign(unit, incident, requestedAssets)` validates rights/resource leases.
- `IReplicationService` converts authoritative state to reliable sector baseline/deltas.
- `IWorldStore` reads/writes snapshots and append-only event records with schema versions.

All state-changing commands include actor identity, server timestamp/tick, command ID for idempotence, version and resource references. Visual events carry *presentation parameters*, not decisions about whether cells burned or structures were saved.

## Build profiles

1. **Client Development**: HDRP and diagnostics, 2K assets, local server connection, profiling.
2. **Client Release**: quality tiers, asset bundles, optional upscaling, no debug instrumentation.
3. **Server Development**: Dedicated Server target, authoritative services and logs, no HDRP startup.
4. **Server Release**: standalone Linux/Windows, config + database + persistent data folders, CI smoke tests.

Never load the full client asset bundle on headless host. Server retains lightweight collision/terrain/vegetation descriptors, not fine visual meshes or PBR textures.

## Simulation clocks (initial proposals)

- Aircraft controller + authoritative flight model: **30–60 Hz**. Test water scooping and stability at 60.
- Active fire grid: **2–5 Hz** plus event-driven ignite/drop actions. Per-cell adaptive stepping with bounded budgets.
- Wind: interpolate between seeded authoritative field updates at **0.5–2 Hz**, optionally more frequently for gust-related safety where measured.
- Player/vehicle network snapshots: **10–30 Hz** by relevance and motion; aircraft near pilot and scoop event may need higher relevance.
- Client rendering: **60 FPS target**, interpolation hides lower server update frequency.
- Snapshots/persistence: seconds/minutes according to risk and database load, immediately at high-value incident state transitions.

These are *benchmarks to test*, not sacred values. Do not place GPU particles on the authoritative sim tick.

## Coordinate precision and 1:10 world

Use double-precision geographic/world coordinates for persistent IDs and broad travel; convert to floating origin for physics/rendering. Compression is a **topological and geospatial art choice** between regional hubs, not a uniformly scaled physics transform. Water-drop/flight/local firefighting calculations always use meters, m/s, kg, SI gravity.

## Must-pass architecture tests

- Standalone server runs without graphics APIs / HDRP scene.
- A client can connect by direct IP without managed cloud matchmaking.
- Full chunk baseline + later deltas reproduce identical authoritative fire-cell state on late join/reconnect.
- Same wind sample drives aircraft, fire and drop solver at common server timestamps.
- Crash recovery returns consistent inventory, burn-state and active incidents without double processing events.
- 60 FPS captured in built client under representative fire and view distances; 100 players only after staged load tests.
