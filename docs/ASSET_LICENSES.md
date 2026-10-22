# Asset licenses and provenance

## Default decisions

1. Prefer **CC0** (Poly Haven/ambientCG when asset individually confirms), then **CC BY** with precise credits; avoid NonCommercial, NoDerivatives, restricted redistribution, editorial-only and unclear licenses in commercial game content.
2. Free model availability is not a blanket grant of commercial rights. Inspect license, author, modified usage and bundled texture rights **at acquisition time**.
3. License obligations apply to assets shipped in both client builds and public mod tools. Keep approved source assets in a documented manifest; do not repackage source CC BY files as a separate marketplace library without assessing restrictions.
4. Model names, liveries, brands, unit logos and real military insignia may involve rights beyond asset copyright; fictional original squadron livery recommended.
5. Audio recordings need separate provenance and licenses; never rip Microsoft Flight Simulator, film or other games' sound files.
6. Assets marked 'candidate' are **not approved**; block production integration until confirmed.

## Template audit row

| ID | Asset | Author | Original URL | License and URL | Changed? | Attribution text | Source hash | Used in build? | Approval |
|---|---|---|---|---|---|---|---|---|
| PH-ROCK-01 | Rocky Terrain | Poly Haven / page author | https://polyhaven.com/a/rocky_terrain | CC0 https://polyhaven.com/license | TBD | optional | TBD | No | Candidate |
| SF-CL415 | CL-415 | helijah | https://sketchfab.com/3d-models/cl415-bf3eaa6a1f4f4fc18df9b05cf40de927 | CC BY, verify exact license page | TBD | Required if used | TBD | No | Candidate |

## Attribution policy

- Maintain a human-readable CREDITS / THIRD_PARTY_ASSETS file generated from manifest.
- If source is CC BY, capture artist name, model title, model URL, license name + license URL, changes made. Preserve relevant notices in distributed build credits (not only repo).
- Poly Haven content is CC0, attribution optional but source URL recorded for reproducibility.
- Allow removal/replacement of third-party content without breaking simulation; gameplay data shouldn't refer to specific author file paths.

## Art asset QC

Inspect and flag: nonmanifold or inefficient mesh, 8K textures on small objects, huge draw calls/material count, unrealistic scale, missing interiors, incorrect pivots, duplicated embedded textures, lack of LOD, model authored for Blender Cycles not HDRP, no animation rig. Link the approved converted prefab, Blender source, editor screenshot and render stats in manifest.
