# GAME DESIGN — Systems & gameplay logic

## Player-experience delivery — planned M05A

[M05A](PLAYER_EXPERIENCE_SPEC.md) integrates actual M01–M05 mechanics into normal controls, contextual interactions, responsive UI, persistent settings, accessibility and playable training. It follows M05 and precedes M06; final operations bindings require real M06 producers. Earlier diagnostic controls are allowed but do not complete M05A. This addition is planned, not runtime implementation.

## Player-facing loop

**Join → Authenticate → Unit/qualification check → Report for shift → Standby & preparation → Dispatch → Assignment → Travel/deploy → Observe/suppress/contain → Reallocate or stand down → Refill/repair → After Action Report → Continue shift.**

No end-of-round. Fire incidents are objects in a persistent world, and several incidents can run at once. Players can leave; server continues with bounded background fire progression. A vacant server uses a configurable *reduced sim rate and ignition cap* to avoid total world destruction.

## Unit structure

- **Regional Operations Command:** sets priorities, sees all incidents, assigns regional resources.
- **Incident Commander:** plans a specific fire and sector staging, dispatches crews, marks air-drop corridors.
- **Air Operations Commander:** manages up to nine aircraft, authorizes assignments, monitors fuel/payload and visibility.
- **Squadron crews:** CL-415 pilot and copilot (two crew slots if implemented); AT-802 pilot (one).
- **Ground Unit Leader:** allocates trucks, hoses, pumps, fire breaks and water supply tasks.
- **Firefighters / Drivers / Logistics:** operate tools, haul and refill water, maintain fuel, move crews and equipment.

Unit membership controls eligibility; in-game qualifications grant capability, not real licensure. Admin/command can temporarily override in sandbox. Command roles and actions are logged. A server can have 100 players, but 9 aircraft is a global authoritative resource limit, including reserved aircraft in flight/operation (choose count semantics in config and enforce atomically).

## Activity budget and agency

No forced 30-minute idle time. Standby offers preflight, refilling, maintenance, training range, weather assessment and assignment briefing. Clear indication when aircraft are unavailable; players can take ground/logistics duty or leave and return.

## Server-owned incident state machine

`POTENTIAL -> DISPATCHED -> RESPONDING -> ACTIVE -> CONTAINED -> MONITORING -> RESOLVED` with alternate `CANCELED`, `MERGED`, `ESCALATED` event labels. `ACTIVE` is true while at least one connected fire chunk has burning cells; `CONTAINED` means no spread past a declared containment line for a configurable interval; `RESOLVED` requires all gameplay-burning cells extinguished and monitoring rules satisfied. If wind reignites the incident during monitoring it returns to ACTIVE. Incidents can merge; history and score attribution must not double count.

### Dispatch generation

- Server evaluates weather, fuel moisture, seasonal hazard, terrain/settlement exposure, concurrent incidents, player population and configurable risk limits.
- Incidents receive coordinates, threat score, incident commander, event history and resource requests.
- No spawns where world is unloaded in a way that creates unbounded high-resolution simulation. Distant regions simulate coarsely until players approach.
- At least one incident can be fully ground-managed; some require aircraft for efficiency but never arbitrary 'press button to extinguish' logic.

## Aerial sortie flow

`STANDBY -> ASSIGNED -> PREFLIGHT -> TAXI -> TAKEOFF -> TRANSIT -> ON_SCENE -> SCOOP (CL-415/Fire Boss) or GROUND_FILL -> APPROACH -> DROP -> EVALUATE -> [SCOOP/REFILL | REASSIGN | RETURN] -> LAND -> REFUEL -> STANDBY`.

- Water doors, flaps, landing gear, engine state, mass and instruments are tied to flight model state.
- Invalid scoop at excessive speed or on unsuitable water is rejected by server and may damage aircraft in advanced settings.
- Dropped water has a measured footprint. The server awards contribution only for actual suppression/infrastructure benefit.
- Fuel and water are finite. No teleport between fires except admin tools / training.

## Ground sortie flow

`AT_STATION -> ASSIGNED -> VEHICLE_CHECK -> TRAVEL -> STAGING -> PUMP_SETUP -> HOSE_DEPLOY -> ATTACK_FRONT / DEFEND_STRUCTURES -> REFILL / REPOSITION -> CONTINUE or RETURN -> REPORT`.

- Hose uses flow/pressure budget, range and nozzle patterns. Avoid full fluid simulation.
- Crew can operate fire break tools, extinguishers, pumps and tankers with real server resource consumption.
- Aircraft drops cannot instantly neutralize all cells; cooling and wetting may delay spread, and ground crews improve containment.

## Sessions, night and geography

- Persistent world; player shifts 60–90 min as voluntary/organizational units only. Daily and weekly snapshots of achievements.
- Day/night speed and lighting configurable independent of 1:10 geographical distance compression.
- High-risk low-level aerial firefighting/scooping at night is restricted by default; ground operations and standby continue. Private server may configure a training override with clear labeling.
- Most water operations and airports remain approximately real size; only between-region transit geography is compressed.

## Contribution and AAR

Attribute contribution to incident, player, crew, unit and command. Measure **marginal useful action**, e.g., prevented spread to high-threat cells, protection of structures, extinguished active intensity weighted by fuel, successful containment, availability / logistics response. Do not simply award liters dropped or time connected. Cap repeated farming and duplicate drop attribution; preserve a shared contribution trace.

After Action Report includes: start/end, fire perimeter history, wind/time series, threatened/saved structures, sorties, validated water impact, ground water used, resource cost, participation, meaningful unit awards. Command decisions are attributed without creating inflated personal rewards.

## Failure, fairness, accessibility

- Fire may destroy structures or spread beyond local capabilities; incidents can end with partial loss, not always success.
- Disconnect/return roles and permissions, no permanent airplane theft. Abandoned air vehicle switches to safe autonomous/park state as designed; avoid instant disappearance mid-flight.
- Adjustable simulation complexity, aim assist for aerial drop training, subtitled dispatch, colorblind-friendly fire state overlays, optional simplified startup procedure.
- No hard-coded paid progression; community server admins may restrict squads but training solo is open.

## Map and scenario progression

1. Limited Zemunik-inspired airfield + one nearby wooded hill/coast with scoop area.
2. One populated settlement protected by ground crews.
3. Two simultaneous independent incidents; dispatch must choose allocation.
4. Realistic streaming sectors and flight corridors.
5. Broader coast + cross-border region only after performance results.
