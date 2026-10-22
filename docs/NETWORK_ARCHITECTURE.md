# Multiplayer/network architecture — 100 players as a target

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
