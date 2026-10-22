# Modding & community-hosted content

## First public scope: safe data-only modifications

- Aircraft/vehicle stat definitions: new mass, wing area, curve references, prop animations and display model references (bounded, validated).
- Ground equipment: pump flow, hose type, tank size, fire vehicle capacity, sound references.
- Additional maps/terrain sectors, nav routes, vegetation fuel layers, buildings and firefighter props.
- Weather/incidents profiles and procedural dispatch presets.
- Unit names, liveries and emblems that community admins approve.

## Mod package shape

```text
Mods/ExampleCoastalVehicles/
  manifest.json
  definitions/aircraft.json
  definitions/vehicles.json
  bundles/windows-client.bundle
  bundles/linux-server-data.bundle  # optional collision/scene data; no GPU-only textures
  licenses/THIRD_PARTY_NOTICES.txt
```

Example manifest properties: `modId`, `semanticVersion`, `gameVersionRange`, `dependencies`, `contentHash`, `author`, `license`, `assetBundles`, `definitions`, `networkSchemaVersion`, `requiresAdminApproval`. All mods are validated on server and clients receive a required mod fingerprint before joining. Do not pull arbitrary executables from random servers.

## Safety

- No unrestricted arbitrary .NET/C# DLL loading by default; trust boundaries and remote code execution concerns.
- Validate numeric limits (payload, max thrust, cell counts), size/type of asset bundles, path traversal protections, dependency graph and schema versions.
- Only approved, signed or allowlisted server content is loaded into shared multiplayer server.
- Mods must not be able to write unvalidated authoritative fire states, change clients' inventories or bypass permission checks.
- Preserve separate asset license manifest per mod and reject incompatible/unknown license packages.

## Future

Optional Steam Workshop publishing/discovery, browser-based mod catalog, migration tools, version conflict reporting and server content pre-download. No core gameplay dependence on Steam services.
