# CL-415 source audit — 2026-10-22

The user signed into Sketchfab, and the agent downloaded the **original OBJ** archive through the approved listing's normal Download button. Authentication is resolved. The original archive remains locally outside Git. **2026-10-24 update:** the user explicitly authorized temporary prototype import and plans replacement with a commercial model before launch. Exact nested source files are now extracted into the separate `CL415Prototype/Source` Unity folder; the new import report records actual runtime verification.

- [Official listing](https://sketchfab.com/3d-models/cl415-bf3eaa6a1f4f4fc18df9b05cf40de927): `cl415` by helijah, Creative Commons Attribution 4.0, commercial use allowed with attribution.
- Download: `cl415.zip`, SHA-256 `1b45ee5122ed6800bb5c5b5469f7ac29722e38c56d0e379dfe56bcc7bf9a6d8c`. Full file inventory, sizes and hashes: [source-audit.json](source-audit.json).
- The nested source archive contains OBJ, MTL, nine PNG textures and [Read-Me.txt](Read-Me.upstream.txt). The README identifies BARANGER Emmanuel (helijah), credits textures to **Gary BROWN**, and says **“This files are GPL.”** It provides neither a GPL version nor the full licence text. The download page and archive therefore do not give a consistent, unambiguous redistribution record for all contributors.

## Prototype decision and production follow-up

The original 2026-10-22 licence gate is superseded **for temporary prototype use only** by the user's direct 2026-10-24 instruction. This records a project decision, not a new third-party grant. Preserve both listing metadata and the exact upstream notice. Before launch, replace this model as requested, or obtain authoritative clarification. Do not infer that the listing relicenses another artist's textures, remove the embedded notice, or silently choose one grant. Obtain authoritative clarification covering geometry and all bundled textures, with the exact applicable licence and required notices; alternatively audit an authorized replacement. Preserve this original notice when resolving the issue. No approval to relicense third-party work is implied by the user's sign-in.

## Unity prototype import — 2026-10-24

The original OBJ's `o` declarations initially imported as one renderer named `default`. A reproducible derivative changes only those declarations to OBJ `g` groups and changes MTL texture paths to the unchanged source PNGs. Unity now preserves **144 named meshes/renderers, 35,799 triangles, 26,002 imported vertices and 144 submeshes**. Extra Unity vertices reflect imported mesh/UV/normal boundaries rather than added source geometry. The exact group/path-only transformation is independently verified against the archive.

Eleven materials map to HDRP/Lit: nine diffuse textures (2048 exterior, 512 instruments, 256 interior/panel), two glass materials preserving original alpha 0.15/0.30. No normal/metallic/roughness maps were supplied; metallic=0 and smoothness=0.35 opaque / 0.8 glass are explicit visual estimates. Root yaw=270 degrees aligns the imported nose to +Z. Bounds after ground placement are 28.785 × 8.816 × 19.795 assumed meters; source units and engineering scale are not independently certified. No Animator, Collider or LODGroup exists. Pivots, animated controls and complete cockpit usability remain unverified. This static asset is not bound to the separate synthetic flight solver.

See [running-build evidence and limits](../../../docs/previews/m03-cl415-prototype/COMPARISON.md) for the completed capture/build checks. The archive observations below are historical pre-import findings; the dimensions are not flight calibration.

## Read-only technical inspection

The OBJ has 20,100 position records and 144 distinct object/group declarations. Names include left/right flaps, propellers, wheels, rudder, doors and instrument objects. These names suggest separable geometry, **not verified moving pivots or a functional rig**. No skeleton or animation file is supplied. Instrument and interior texture filenames do not establish a complete cockpit.

Raw bounds are approximately 19.795 × 8.816 × 28.785 source units. Units/orientation, scale normalization, pivots, hierarchy, logos/livery rights, mesh quality, HDRP material conversion, cockpit coverage and runtime cost remain unverified in Unity. These were the pre-import unknowns; the separate M03 import report records actual Unity verification and screenshots. These dimensions are an archive observation, not validated aircraft specifications or flight physics.

The exterior import remains unchecked under M03. The user-approved 2026-10-23 dependency closeout closes M00's verified foundation scope and permits independent M01 fire/wind work; it did not authorize aircraft integration. The later user instruction above now authorizes the isolated prototype import without resolving contributor rights.

Reproduce the read-only inventory/hash checks from the repository root after obtaining the official original download:

```powershell
python scripts/verify_cl415_archive.py AssetSources/Aircraft/CL415/cl415.zip
```

The original archive is ignored by Git. The verifier neither extracts nor executes its contents and does not resolve the licence conflict. After verification, `python scripts/extract_cl415_prototype.py` extracts only the twelve fixed, hash-audited source files, retaining every original byte and refusing mismatched existing output.
