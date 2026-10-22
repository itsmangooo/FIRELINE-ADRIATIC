# Asset Manifest — Source links and pipeline (NO actual assets bundled)

> **Status key:** `Verified listing` means publicly indexed source/title/license verified, **NOT downloaded, rigged, imported or approved in Unity**. `Candidate` means asset identity/licensing/quality still must be confirmed by Codex or user. Check original product pages immediately before downloading, including whether availability or terms changed.

## What to download first (Milestone 0–1)

**Minimum:** one properly licensed CL-415 exterior model, representative Croatian coastal heightfield (legally obtained), seawater shader, four terrain PBR surfaces, a few pine assets, one burned-ground material, minimal fire/smoke VFX references. Start with high-quality source textures but optimized 2K runtime sizes.

### A. Aircraft

| Asset | URL | Listing/status | Import role |
|---|---|---|---|
| CL-415 by helijah | https://sketchfab.com/3d-models/cl415-bf3eaa6a1f4f4fc18df9b05cf40de927 | **Verified listing**, downloadable, CC BY; ~35.8k tris (2026 indexing) | first exterior model; **not validated for moving parts/cockpit** |
| Air Tractor AT-802F / Fire Boss | https://airtractor.com/wp-content/uploads/2022/09/Air-Tractor_802F-Brochure_2022.pdf | **specification reference only** | locate properly licensed 3D asset separately; never equate Fire Boss and land AT-802F |

### B. Poly Haven CC0 PBR materials (individual pages verified)

| Material | Source download page | Used for | Suggested runtime resolution |
|---|---|---|---|
| Rocky Terrain | https://polyhaven.com/a/rocky_terrain | Dalmatian karst/hills | 2–4K terrain layer |
| Rocky Terrain 02 | https://polyhaven.com/a/rocky_terrain_02 | low vegetation and broken rock | 2–4K terrain layer |
| Dry Ground Rocks | https://polyhaven.com/a/dry_ground_rocks | dry brush fuel beds, dusty tracks | 2K |
| Forest Ground 01 | https://polyhaven.com/a/forrest_ground_01 | under-pine litter | 2K |
| Burned Ground 01 | https://polyhaven.com/a/burned_ground_01 | persistent char overlay/ground blend | 2K |
| Coast Sand 01 | https://polyhaven.com/a/coast_sand_01 | beaches/water access | 2K |
| Asphalt 04 | https://polyhaven.com/a/asphalt_04 | roads/airport/tarmac (appropriate tiling) | 2–4K |
| White Plaster 02 | https://polyhaven.com/a/white_plaster_02 | Dalmatian building facades | 2K |
| Corrugated Iron | https://polyhaven.com/a/corrugated_iron | roofs, airport hangars | 2K |
| Pine Bark | https://polyhaven.com/a/pine_bark | trunks and burn-stage variants | 1–2K |

**License:** Poly Haven CC0 https://polyhaven.com/license . Users may download directly from each official page via its Download menu. Do not guess private file URLs or embed preview JPGs as production PBR; fetch proper source texture maps (diffuse/albedo, Normal DX, roughness, metallic, AO/displacement where relevant). Unity HDRP mask packing: examine current HDRP shader packing rules and repack maps rather than blindly assign AO/Rough/Metal textures to the wrong channels. Color/sRGB for albedo, linear for data maps; verify normal map orientation. Use texture streaming, mipmaps, VRAM budgets.

### C. HDRI / skies and lighting references

| Resource | Link | Role |
|---|---|---|
| Poly Haven HDRI catalogue | https://polyhaven.com/hdris | Find day, overcast and dusk reference lighting for authoring |
| Cloud Layers | https://polyhaven.com/a/cloud_layers | cloudy cinematic HDRI candidate; review horizon suitability |
| Qwantani Sunset (Pure Sky) | https://polyhaven.com/a/qwantani_sunset_puresky | dusk / sun study |
| Poly Haven Aarfontein Dusk | https://polyhaven.com/a/aarfontein_dusk | user-liked arid dusk scene; **lighting reference**, NOT Croatian terrain |

For actual dynamic day/night and server-weather data use HDRP Physically Based Sky + volumetric cloud/fog configured through server-driven time/weather parameters; HDRI is not a dynamic weather physics system.

### D. Ground equipment (index-verified Sketchfab listings, CC BY)

| Asset | Link | Status / notes |
|---|---|---|
| Fire Truck (amongus) | https://sketchfab.com/3d-models/fire-truck-4a0d8c5bfca643d2aaa9b113ff752c26 | **Verified listing**, CC BY, ~72.2k triangles, not rigged/animated as far as known |
| Firefighter (doctortex) | https://sketchfab.com/3d-models/firefighter-964fa8b171df41dd8fe6b1305da24812 | **Verified listing**, CC BY, ~29.4k triangles, inspect rig/license files |
| Fire Hose (NurulAthyrah) | https://sketchfab.com/3d-models/fire-hose-224c1b9c08c346ba85995b6c930efec9 | **Verified listing**, CC BY, ~13.5k triangles, prop only, not functional hose rig |
| Civil Service Vehicles Pack | https://sketchfab.com/3d-models/generic-civil-service-vehicles-pack-8ff2a13f30914932a70c7950cfa58465 | **Verified listing**, CC BY, contains generic fire truck; stylized / visual mismatch risk |
| Fire Extinguisher | https://sketchfab.com/3d-models/game-ready-fire-extinguisher-eac8a0eded454d01b0cc2fa4157b2912 | **Candidate**; link needs individual recheck before download |
| Halligan bar | https://sketchfab.com/3d-models/30-inch-halligan-bar-75adc2cd61aa4e059f07f9d3fb1d235c | **Candidate**; link needs individual recheck |
| Shovel | https://sketchfab.com/3d-models/shovel-pbr-game-ready-asset-55111ab4e14a4a0f86200b246677f915 | **Candidate**; link needs individual recheck |

Sketchfab CC BY normally requires credit/license notice and link and indication of changes. Commercial use generally allowed under CC BY, **verify model page and included textures individually**. Don't use `NC`/`ND` content for a commercial/modifiable game without separate rights. High-quality model may need re-topology/LOD/material harmonization. Do not scrape or bypass download restrictions.

### E. Other mandatory asset classes / research tasks

- Flight instruments, complete CL-415 **interior/cockpit**, animated props/gear/flaps/scoop doors. The exterior listing is not evidence those parts exist.
- AT-802F and *float-equipped* Fire Boss game-ready separate aircraft; verify license.
- Rigged firefighters wearing Croatia-appropriate PPE and helmets, pilot characters, first/third-person animations; check Mixamo terms at https://www.mixamo.com/ for use/redistribution restrictions.
- High-quality HDRP-compatible fire/smoke/water VFX: inspect official Unity samples or licensed VFX packs; **do not assume a screenshot is a free project asset**.
- Mediterranean pines/brush, olive trees, juniper, vegetation LODs with char/burn variants (seasonally plausible); model fuel metadata separately.
- Fire hose branchpipe/nozzle, pumps, hydrants, reels, foam appliances, breathing gear, axes, pry bars, rakes, shovels, fire beaters, tanks, maintenance tools, sirens.
- Vehicles with correct wheel pivots, wheel/suspension colliders, doors and usable crew spaces.
- Audio recordings: actual propeller/turboprop/cockpit/radio/fire/pump/wind/water. Record provenance, sound licenses, and loop points.
- Geographic elevation and coastline datasets: investigate official Copernicus DEM terms plus open source vector map licenses; never rip restricted Google/Maps scenery.

## Where to put files

```text
AssetSources/          # source downloads & licenses (keep outside Unity build; not necessarily Git)
  Aircraft/CL415/<originals + source-url.txt>
  Environment/PolyHaven/<asset-name>/<original-zips>
  Ground/<author>/<model>/
Assets/Fireline/Content/
  Aircraft/             # optimized FBX/prefab/textures, skeletons, colliders
  Environment/          # HDRP terrain layers, vegetation LODs, props
  VFX/                  # graph assets, simulation-driven spawners
  Audio/                # licensed recordings, mixer configs
```

## Approval checklist per asset

`[ ] original URL [ ] creator [ ] license [ ] downloaded from official link [ ] full source file and attribution saved [ ] no NC/ND conflict [ ] no copyrighted logos in non-approved usage [ ] dimensions [ ] pivots [ ] rig/animation [ ] GPU/CPU cost [ ] LODs [ ] Unity import screenshot [ ] gameplay view test [ ] status Approved`

**Do not falsely mark assets 'downloaded/imported' merely because this file contains their URLs.**
