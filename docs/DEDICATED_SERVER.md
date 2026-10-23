# Dedicated servers — self-hosting and operational model

## Current M02 local helper boundary

The [checkpoint increment](reports/2026-10-23-m02-checkpoints.md) supplies a bounded codec, validated candidate restoration and a single-writer local file store. These are not yet connected to normal server startup/save scheduling or demonstrated across process restart. Missing/corrupt data throws rather than silently resetting vegetation. Database/event replay, admission, operator backup policy and power-loss qualification remain future work. The lifecycle below remains proposed.

## Deliverables for self-hostable game

- Windows x64 headless build and Linux x64 headless build, generated using Unity Dedicated Server build target where supported by installed editor.
- `server.example.json` documented, no secrets; actual config stored outside shipped repository.
- CLI startup with world slot, transport bind address, TCP/UDP port(s), public discovery address and PostgreSQL DSN through **environment variables / secret manager**.
- `server.log` structured, bounded rotation, crash logs; health endpoint and controlled shutdown.
- Backup/restore tool for DB and world checkpoint files; versioned schema migrations.
- Whitelist, server password, role authorization, moderation and admin audit events.
- Operator docs for router port forwarding, firewall, hardware requirements (after benchmarking), save location, patches and world backups.
- Local single server + 2 client verification as first gateway; no rental service required.

## Example configuration schema (not executable implementation)

```json
{
  "serverName": "Adriatic Ops Community #1",
  "listenAddress": "0.0.0.0",
  "gamePort": 7777,
  "maxPlayersTarget": 100,
  "maxOperationalAircraft": 9,
  "requireUnitQualifications": true,
  "allowTrainingOverrides": false,
  "fireIgnitionRateMultiplier": 1.0,
  "weatherSeverityMultiplier": 1.0,
  "dayNightTimeScale": 1.0,
  "emptyServerMode": "coarse_simulation",
  "worldSaveSlot": "adriatic_01",
  "publicListingOptIn": false
}
```

Read `POSTGRES_CONNECTION_STRING` and credentials from env, never config checked into Git. Discover actual transport and admin port requirements from chosen stack.

## Server lifecycle

`LOAD CONFIG -> CONNECT DATABASE -> APPLY SAFE MIGRATIONS -> RESTORE CHECKPOINT -> REPLAY IDEMPOTENT EVENTS -> LOAD ACTIVE SECTORS -> START SIMULATION -> ACCEPT PLAYERS -> SNAPSHOTS & EVENTS -> SHUTDOWN CHECKPOINT`.

On crash: no manual recreation of burned trees; restore authoritative cell states/incident events. On empty server: coarsen sim and pause spawn according to settings; do not let unattended fires irreversibly destroy an entire server world without configured policy.

## Security

Role control is enforced **on server**, even if UI hides the 'fly' button. Each aircraft is a single leased asset; authorize squadron/seat/qualification atomically. Avoid direct DB credentials in game clients. Administrative operations are authenticated separately and logged. Back up database; allow `--restore` / validation-only mode. Don't load user-provided executable mods by default.

## Discord/Steam/backend separation

Optional public list server only advertises address, mod fingerprint, region, open slots and server version; gameplay world continues if listing is offline. Optional Steam identity can improve UX but must not be required to self-host if that conflicts with operator goals. Self-hosters own their own player data and can delete/export it under published server privacy policy.
