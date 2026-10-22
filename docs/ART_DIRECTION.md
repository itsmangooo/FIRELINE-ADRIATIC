# Art Direction — high-end Croatian coastal environments

## Intent

Grounded, cinematic photorealistic-leaning simulator in real-looking Mediterranean weather and terrain (NOT stylized Valorant/Borderlands graphics from the unrelated RANKS project). Croatia should be recognizable from geography, stone architecture, vegetation and sunlight. Avoid false official logos and thin 'tech demo' scenes.

## Biomes and materials

- Karst slopes: Poly Haven `rocky_terrain`, `rocky_terrain_02`, `dry_ground_rocks`.
- Pine forests and understory: `forrest_ground_01`, `pine_bark`, purchased/licensed authored pine meshes; scale-appropriate LODs, wind bending and burn/char masks.
- Burn scar: `burned_ground_01` + decals and darkened/blackened material variants; server char state drives blend parameters.
- Coastal access: `coast_sand_01`, local stones and water shader; avoid unnecessary shore detail at high-altitude view.
- Roads/airfield: `asphalt_04`, painted markings and runway modular assets.
- Town and hangar: `white_plaster_02`, `corrugated_iron`, locally appropriate roof and concrete styles.

## Lighting / weather

HDRP Physically Based Sky/volumetrics and directional sunlight for day/night. Use Poly Haven HDRI (`cloud_layers`, `qwantani_sunset_puresky`, `aarfontein_dusk`) as lighting reference or fixed cinematic scenario, not as a hard-coded world sky with distant African landscapes. Weather/time/wind set by server; clouds/fog and client particles interpolate shared parameters. Night aerial firefighting generally restricted in standard mode.

## VFX system

- Server owns grid fire intensity, burn stage, wetness, smoke opacity envelope and wind vectors.
- VFX Graph renders flames, embers, smoke particles, water spray, steam and heat haze **from the received field**.
- GPU particle detail density LOD and distance impostors. Smoke columns render with coarse volumes at distance; no GPU particle networking.
- Water drop uses a *server-computed coverage footprint* plus a purely cosmetic sheet/particles.
- Burning trees use 3–4 distinct visible stages and persist with char mesh/material after restarts.

## Quality bar

- 1080p at 60 FPS on a **named target gaming PC** in representative scene (multiple fires, aircraft and ground crews); do not market 4K Ultra at 100 players without tests.
- Cockpit exterior/inner detail LOD policy, no 1M-triangle props close to all clients.
- Daylight must preserve readable road/structure and smoke boundaries; strong atmospheric mood without obscuring usable gameplay info.
- Validation renders: airfield at dusk; coastal fire plume from 500 m; water drop at altitude; firefighter with hose near charred vegetation; night ground response.

See `docs/PERFORMANCE_60FPS.md` for budgets.
