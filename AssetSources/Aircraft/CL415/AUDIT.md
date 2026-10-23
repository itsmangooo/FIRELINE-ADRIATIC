# CL-415 source audit — 2026-10-22

The user signed into Sketchfab, and the agent downloaded the **original OBJ** archive through the approved listing's normal Download button. Authentication is resolved. The archive remains locally outside Unity and Git; no model or texture from it ships in a build.

- [Official listing](https://sketchfab.com/3d-models/cl415-bf3eaa6a1f4f4fc18df9b05cf40de927): `cl415` by helijah, Creative Commons Attribution 4.0, commercial use allowed with attribution.
- Download: `cl415.zip`, SHA-256 `1b45ee5122ed6800bb5c5b5469f7ac29722e38c56d0e379dfe56bcc7bf9a6d8c`. Full file inventory, sizes and hashes: [source-audit.json](source-audit.json).
- The nested source archive contains OBJ, MTL, nine PNG textures and [Read-Me.txt](Read-Me.upstream.txt). The README identifies BARANGER Emmanuel (helijah), credits textures to **Gary BROWN**, and says **“This files are GPL.”** It provides neither a GPL version nor the full licence text. The download page and archive therefore do not give a consistent, unambiguous redistribution record for all contributors.

## Import gate

**Blocked on licence reconciliation.** Do not infer that the listing relicenses another artist's textures, remove the embedded notice, or silently choose one grant. Obtain authoritative clarification covering geometry and all bundled textures, with the exact applicable licence and required notices; alternatively audit an authorized replacement. Preserve this original notice when resolving the issue. No approval to relicense third-party work is implied by the user's sign-in.

## Read-only technical inspection

The OBJ has 20,100 position records and 144 distinct object/group declarations. Names include left/right flaps, propellers, wheels, rudder, doors and instrument objects. These names suggest separable geometry, **not verified moving pivots or a functional rig**. No skeleton or animation file is supplied. Instrument and interior texture filenames do not establish a complete cockpit.

Raw bounds are approximately 19.795 × 8.816 × 28.785 source units. Units/orientation, scale normalization, pivots, hierarchy, logos/livery rights, mesh quality, HDRP material conversion, cockpit coverage and runtime cost remain unverified in Unity. No import screenshot exists because the licensing gate prevents integration. These dimensions are an archive observation, not validated aircraft specifications or flight physics.

The exterior import remains unchecked under M03. The user-approved 2026-10-23 dependency closeout closes M00's verified foundation scope and permits independent M01 fire/wind work; it does not resolve this licence gate or authorize aircraft integration.

Reproduce the read-only inventory/hash checks from the repository root after obtaining the official original download:

```powershell
python scripts/verify_cl415_archive.py AssetSources/Aircraft/CL415/cl415.zip
```

The original archive is ignored by Git. The verifier neither extracts nor executes its contents and does not resolve the licence conflict.
