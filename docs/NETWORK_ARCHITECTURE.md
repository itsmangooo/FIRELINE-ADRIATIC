# Multiplayer/network architecture — 100 players as a target

## M03 numerical water core — no live protocol change, 2026-10-26

The new water release/parcel/raster functions run only in the explicit dedicated numerical fixture, producing actual existing fire cooling locally. Aircraft RPC fields/version 2, client code and live `FlightAuthority` are unchanged. [Actual results and limits](reports/2026-10-26-m03-water-parcels.md). This does not authorize client-selected payloads/footprints or implement live water controls, release events, multiplayer suppression, idempotent event delivery or persistence. Those require a seat-authorized atomic server transaction and actual network checks next.

## M03 real-time route/cleanup acceptance — 2026-10-26

Two actual local dedicated/rendered-owner/late-headless-observer groups now complete a calm-air straight hop over existing version-2 input/pose/ACK RPCs. Requests remain server seat/epoch/sequence/tick/range/rate/lease validated. Client QA role labels grant no permission; server derives identity from connection. Enqueued/received RPCs and installed per-step inputs are independently reconciled. `[System.Serializable]` adds complete nested request JSON evidence without changing wire fields. Owner waits for actual vacant-owner/new-epoch snapshot, then allows 0.15 s transport updates before quit; observer sees/ACKs vacancy and disconnects. Earlier immediate quit caused lease expiry and remains a rejected attempt. [Source, data, actual cleanup checks and limits](reports/2026-10-26-m03-realtime-flight.md). No WAN/impairment/prediction/capacity/admission or measured bandwidth qualification.

## M03 aircraft protocol v2 — 2026-10-25

Aircraft request/reply/state headers now use `FlightWire.Version=2`; state adds horizontal `WindX/WindZ`, sampled by the server at the published post-step position/fire tick. Wind revision and aircraft/fire ticks remain included. Client rejects incompatible/nonfinite/out-of-bounds replicas before installing them, computes readouts only and cannot publish wind/pose/fuel. Matched rebuilt client/server RPC collections are mandatory; mixed earlier binaries are unqualified, even for scenes whose fire schema itself is unchanged. Reliable/ACK/lease/seat bounds remain as previously implemented. No measured bandwidth claim. Actual owner+late observer receive **404** exactly matched snapshots; **406** server wind publications independently reconstruct. [Evidence, actual message/readout SVG and limits](reports/2026-10-25-m03-flight-instruments.md). Impaired-network flight/admission/capacity/persistence remain open.

## M03 finite-fuel authority — 2026-10-24

Fuel now decreases only in accepted server physical steps; the existing complete-pose `FuelKg` field carries that truth without a schema change. There is no client fuel/refuel command and the existing forged-state rejection still applies. The client uses received fuel/water to derive its CoG visual offset; it never runs the fuel integrator. [Actual transaction, tests and evidence](physics/aircraft-fuel/COMPARISON.md). This does not add aircraft persistence, production admission, prediction or impaired-network qualification.

## M03 engineering input/view binding — 2026-10-24

The client now has explicit keyboard/gamepad seat/input requests and a 20 Hz fresh-owner transmission gate; the existing server still independently validates connection identity, epoch, serial, observed tick, bounds/rate and lease. Visual poses use tested 100 ms receipt interpolation with endpoint holding, no extrapolation or client simulation. Actual rendered-owner/late-observer proof matches 401 received snapshots to server publications and reconstructs 1,723 display frames. Focus gate, release/reclaim and final disconnect are verified through real transport. [Actual evidence and initial failed recording correction](reports/2026-10-24-m03-flight-view.md).

This adds no new RPC schema, prediction/correction, account/pilot qualification, flight persistence, WAN/traffic/capacity result or cloud dependency. Existing reliable stop-and-wait pose bounds remain. The engineering mesh/controls/interpolation now exist; the following older pending statements describe their earlier scope.

## M03 implemented debug-aircraft protocol — 2026-10-24

An explicit opt-in session now accepts subscribe/claim/input/release/exact-tick ACK requests for one estimated aircraft. Only actual transport source identity can control its occupied seat. Epoch, sequence, observed tick, control bounds, 50 Hz accepted-input rate and 0.5 s lease guard the server-owned state. Complete SI poses use reliable RPCs at most 10 Hz, one unacknowledged pose per peer, five-second ACK timeout; client-published poses are rejected. Eight admitted identities per debug-session lifetime, 64 processed requests per host callback/eight per peer bound application work. This is no transport-level flood protection or production authentication/qualification service. [Actual flow, wire/clock details, late observer/disconnect proof and limitations](physics/live-flight-host/COMPARISON.md).

Flight poses are read-only data in the client adapter; prediction/interpolation, mesh binding, reconnect, real traffic measurement, impaired-network flight tests and production ghost/interest scheduling remain pending. M02 checkpoint encoding is unchanged; this flight mode explicitly excludes save/load. M02 fire RPCs remain separate and normal server fire/wind continues alongside the opt-in aircraft.

## M02 implemented bounded protocol — 2026-10-23

Actual dedicated-to-client schema-1 baselines and exact-base revision deltas now cross targeted reliable RPCs, in bounded 480-character fragments. Clients validate/ACK canonical state, repair gaps through a baseline, display phases read-only and exchange one-second heartbeats with a 15-second silent timeout. Eight peers / eight fragments per peer per Update / at least 0.5 seconds after ACK bound this prototype. No client ignition/water RPC is exposed. Explicit local load/save is integrated and separate-process restart is demonstrated.

[Implemented BEFORE/AFTER SVG, wire guards, actual three-client/late-join/restart evidence and limits](reports/2026-10-23-m02-replication.md). Both direct and constant-delay/loss cases pass; cold/jittered startup remains NET-01. Production interest, unreliable snapshots/traffic scheduling, auth/admission, reconnect UX, WAN and capacity are unimplemented/unqualified. The historical checkpoints and broader proposed message classes below must not be read as current implementation.

## M01 server state — historical checkpoint

The authoritative server now owns fire chunks, revisions, wind and coarse hazard, with detached snapshot/latest-step delta types. These are data contracts, not a replication implementation. Two clients still prove only the original connection handshake; no client receives fire or sends a water command in M01. M02 must implement/test serialization, reliable baselines, ordered deltas/gap repair, late join and restart state. [Actual M01 implementation](reports/2026-10-23-m01-fire-wind.md).

## M00 foundation boundary

The foundation uses Netcode for Entities 6.6.0 and Transport 6.6.0 with one client or server world per process. Explicit numeric IPv4 listen/connect requests support local and direct-IP startup without a managed lobby. Connection `NetworkId` values can be observed for smoke verification. No `NetworkStreamInGame`, ghost snapshots, fire chunks or gameplay messages are added at this stage. Authentication, public admission, reconnect and packet-loss recovery remain unimplemented. See the [M00 report](reports/2026-10-22-m00-foundation.md) for the exact verification scope; the protocol below remains proposed.

## Dedicated server is not optional

Main mode is **headless dedicated authoritative server**; never make client listen-host or P2P the authoritative persistent production mode. One logical world runs per server instance. Hosting is community-owned; direct IP works without mandatory matchmaker. Evaluate supported Netcode for Entities/Unity Transport package versions. Server controls fire, weather, physical water footprint, vehicle state, incidents, inventory/permissions, statistics and writes.

## What crosses the wire

| Message class | Direction | Reliability and relevance |
|---|---|---|
| Movement / aircraft controls | client→server | Sequenced/validated input; rate-limited, time-ordered |
| Positions, angular velocity, payload, engine | server→client | Unreliable delta snapshots with interpolation, high relevancy nearby |
| Fire chunk baseline and char states | server→client | Reliable + version/checksum, at subscription/repair |
| Fire chunk deltas | server→client | Sequenced delta updates + gap recovery |
| Weather field sample/revision | server→client | Periodic + event when gust/front shift; interpolation |
| Drop command | client→server | Authenticated equipment/payload/aircraft/permission validated |
| Drop footprint & suppression result | server→clients | Reliable high-value event + fire cell revisions |
| Dispatch, assignments, incidents, qualification | server↔client | Reliable, authenticated commands and authoritative replies |
| UI cosmetic sparks | client only | **Never networked** |
| Radio audio | voice provider / separate subsystem | optional, rate limits / permissions; not fire sim tick |

Never accept a client's `I extinguished cell X`, wind direction, scoreboard result or 'teleport airplane' as authoritative state. Validate inputs against server simulation, rate limits, ownership and physically reasonable bounds. A dedicated server doesn't guarantee anti-cheat automatically; audit attack surfaces.

## Geographic relevancy / interest management

Divide into large sectors with smaller fire chunks. Subscription by player/camera/aircraft velocity predicted path, incident assignments, group command role and distance. Ground crew in Split receives detailed local entities and fire; dispecher gets low-frequency summaries of Omiš/Zadar/Ljubuški; aircraft near sector boundary prefetches next sector. Avoid replicated ghosts for individual VFX particles.

Recommended test targets: nearby player/vehicle snapshots 10–30 Hz, remote strategic 0.2–2 Hz, active fire deltas 2–5 Hz depending on change. Initial baseline is compressed and chunked. Use per-client bandwidth budgeting, significance ranking, and interest-priority falloff. No exact network packet sizes promised without implementation.

## Client smoothness vs truth

Render aircraft/pilot using local input prediction and correction where feasible, use interpolation for remote entities. Physics authority remains server. Fire presentation uses interpolation and client-side VFX driven by authoritative cell intensities, so exact particle trajectories can differ while the *same* trees burn and the *same* water suppresses them.

## Admission, reconnect, voice

- Identity tokens / password or whitelist; server validates membership/qualifications; admins are distinct from players.
- Network transport encryption and authentication appropriate to chosen stack; replay protection and rate limiting.
- Reconnect resume: reassign seat carefully; prevent dual logins, reserve role/vehicle for a short grace period.
- Host operators choose whether to require Steam or allow direct independent credentials; Steam not mandatory.
- Radio channels scoped by aircraft, ground team, incident and command; voice is optional and separated from core simulation.

## Tests / 100 player gates

1. Server + two separate processes; broadcast same fire-cell checksum, water result and wind revision.
2. Third client late joins; receives full active chunk baseline; 100% cell ID/state parity after convergence.
3. 16, 32, 64 and 100 bot-driven clients **with meaningful aircraft/vehicle/fire inputs**, not idle dummy sockets.
4. Test worse-network scenarios (latency 30/80/150 ms, loss 1–5%, jitter), no data races or invalid commands.
5. Representative world: 9 operational aircraft, multiple burning sectors, ≥20 ground vehicles, 100 actors, smoke and streamed terrain; profiler records server main-thread spikes, memory, egress per client, p95/p99 RTT.
6. Fail tests transparently; reduce update rates and scope first, not hard-code false acceptance.

## One point to stress-test early

Unity's official Megacity Metro demonstrates 100+ with URP + Netcode for Entities; the FIRELINE client's HDRP visuals and fire/VFX are additional workloads. The Unity client FPS budget and server capacity are *independent* acceptance gates.
