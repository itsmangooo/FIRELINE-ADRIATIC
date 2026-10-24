# Asset licenses and provenance

## CL-415 prototype decision — 2026-10-24

The user explicitly authorized importing the official archive for temporary prototype use despite the previously recorded conflicting notices, and intends a commercial replacement before launch. The import retains all original source bytes and the BARANGER Emmanuel / Gary BROWN notice. This is a scoped project decision, not a declaration that the contributors' rights were reconciled. The prior import hold below is superseded for the prototype; replace or resolve all applicable rights/notices before production distribution. [Original source, hashes and decision](../AssetSources/Aircraft/CL415/AUDIT.md), [distributed notices](../THIRD_PARTY_ASSETS.md).

## M00 coastal source audit — 2026-10-22

The five imported surfaces are Poly Haven CC0 assets: Rocky Terrain by Amal Kumar; Dry Ground Rocks, Forest Ground 01, Coast Sand 01 and Burned Ground 01 by Rob Tuytel. Checked the official asset pages and [licence](https://polyhaven.com/license), then obtained real 2K map files through the [official public API](https://github.com/Poly-Haven/Public-API/blob/master/ToS.md), using the required project-specific user-agent. Size and upstream MD5 checks passed; input/derived SHA-256, URLs and creator metadata are retained in the [acquisition manifest](../AssetSources/Environment/acquisition-manifest.json). No preview renders were used as textures.

Petrčane uses an adapted Copernicus **GLO-90** DSM crop, not restricted EEA-10 or GLO-30-R data. The [official licence bundle](../AssetSources/Environment/Copernicus/cop_dem_licenses_2025.pdf), pages **19–21**, grants reproduction, distribution, communication and adaptation under its free/open terms. Required modified-source and liability notices plus subsequent-distribution obligations are in [THIRD_PARTY_ASSETS.md](../THIRD_PARTY_ASSETS.md); the benchmark build copies that notice and the bundle into `Legal/`. The upstream tile's older `eula_F.pdf` concerns restricted instances and is not used as this crop's licence. No official endorsement is implied. The [AWS registry](https://registry.opendata.aws/copernicus-dem/) and [Copernicus collection](https://dataspace.copernicus.eu/explore-data/data-collections/copernicus-contributing-missions/collections-description/COP-DEM) identify the dataset and source obligations.

The user resolved Sketchfab authentication and the original OBJ archive was downloaded through its official button. The listing says CC BY 4.0, but the included README says GPL (no version/full text) and separately credits textures to Gary BROWN. This conflict blocks redistribution/import pending authoritative clarification covering all contributors and required notices, or an authorized replacement. [Actual archive audit, hashes, source inventory and next action](../AssetSources/Aircraft/CL415/AUDIT.md). No aircraft content is imported or shipped.

## M00 foundation provenance — 2026-10-22

No third-party aircraft, vegetation, texture or audio source was acquired during the foundation increment. `FL-M00-CALIBRATION` in the [manifest](ASSET_MANIFEST.md) uses Unity-generated primitive geometry and a project-authored material without texture maps. It has no external model artist or separate attribution requirement. Original Unity HDRP starter content remains subject to its existing Unity/template terms; this report does not relabel that content CC0. Netcode and its dependencies retain their package notices under the installed Unity packages.

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
| SF-CL415 | CL-415 | helijah / textures credited to Gary BROWN | https://sketchfab.com/3d-models/cl415-bf3eaa6a1f4f4fc18df9b05cf40de927 | Listing CC BY 4.0; embedded GPL notice unresolved | Unity group derivative, HDRP conversion, orientation and static preview | Original contributor notices retained | See source-audit.json | Yes: separate static preview client only | User-authorized temporary prototype; replace before launch |

## Attribution policy

- Maintain a human-readable CREDITS / THIRD_PARTY_ASSETS file generated from manifest.
- If source is CC BY, capture artist name, model title, model URL, license name + license URL, changes made. Preserve relevant notices in distributed build credits (not only repo).
- Poly Haven content is CC0, attribution optional but source URL recorded for reproducibility.
- Allow removal/replacement of third-party content without breaking simulation; gameplay data shouldn't refer to specific author file paths.

## Art asset QC

Inspect and flag: nonmanifold or inefficient mesh, 8K textures on small objects, huge draw calls/material count, unrealistic scale, missing interiors, incorrect pivots, duplicated embedded textures, lack of LOD, model authored for Blender Cycles not HDRP, no animation rig. Link the approved converted prefab, Blender source, editor screenshot and render stats in manifest.
