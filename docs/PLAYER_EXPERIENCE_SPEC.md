# M05A — Player Experience, Controls, UI/UX & Settings

**Planned / Pending. No runtime implementation is claimed.** The [supplied prompt](../prompts/FIRELINE_ADRIATIC_M05A_Player_Experience.md) is the complete requirement list. M05A follows M05 and precedes M06; other milestone numbers remain unchanged.

## Required scope

- First-person/optional third-person movement, context-sensitive interactions, equipment/inventory, tool pickup/carry/use, vehicle entry/exit, truck/pump/hose operation, aircraft boarding/control, cockpit instruments/switches, assignments and operational radio/communication interfaces.
- Unity Input System contexts for on-foot, flight, ground vehicles, firefighting equipment, tactical map/command and UI/menu. Keyboard/mouse/controller, saved rebinding, sensitivity/dead zones, flight inversion, hold/toggle and future HOTAS/joystick profiles. Simplified/advanced flight obeys server rules. The declared Input System 1.20.1 must be audited again at implementation; hardware support needs actual tests.
- Functional Main Menu, Multiplayer Server Browser, Connection/Lobby, Unit/Duty Selection, Profile, Aircraft Selection/Assignment, Equipment/Vehicle Interaction, Operational Map, Incident Information, Aircraft HUD/Cockpit UI, Firefighter HUD, Dispatch/Command, After Action Reports and Settings. Use consistent, responsive aviation/emergency-services design, including readable loading/error/denied states. Direct-IP self-hosting remains available without a mandatory paid/cloud lobby.
- Playable tutorials for movement/interaction, tools, pumps/hoses, driving, aircraft startup/flight, scoop/drop and dispatch/map. Use the same simulation and validation as normal multiplayer, not disconnected demonstrations.

## Persistent settings

| Area | Required options |
|---|---|
| Graphics | Low/Performance, Medium/Balanced, High, Ultra, Custom; resolution/display mode, FPS cap, VSync, render scale, textures, shadows, vegetation, terrain detail, volumetric fog/smoke, water reflections, AA and supported upscaling |
| Audio | Master, engines, vehicles, environment, UI, radio, microphone |
| Controls | Keybinds, mouse/controller sensitivity, flight controls, inversion, dead zones |
| Gameplay/accessibility | HUD customization, subtitles, UI scale, colorblind-friendly overlays, camera shake, interaction assistance, units, tutorial hints |

These future player-facing tiers do not relabel today's Performant/Balanced/High Fidelity assets as complete. Preserve existing presets until measured mappings/replacements are verified. Settings must affect supported systems and survive restart; explain unsupported options. Local preferences cannot override server rules or weaken authoritative smoke visibility. Benchmark presets against the documented 1080p/60 FPS target rather than assigning arbitrary quality values.

**2026-10-24 user requirements:** the future supplied ~30 × 4 km map receives HDRP textures/lighting in M04, full streaming in M08, and quality qualification in M09. Include view/draw distance, animation detail/update distance and effect quality in the saved Low/Medium/High/Ultra/Custom settings. Ultra targets 1080p/60 FPS on i5-9400F / 16 GB / GTX 1660-class hardware; this is unverified and does not weaken percentile/smoke-parity gates. [Detailed delivery and evidence plan](LARGE_MAP_AND_QUALITY_PLAN.md).

## Dependencies and staged delivery

| Producer | M05A integration |
|---|---|
| M01 fire/wind and M02 replication/recovery | Truthful shared world/hazard state and server-validated commands |
| M03 flight/water, M04 environment, M05 ground forces | Normal controls, vehicle/equipment/cockpit interactions and available ground/aerial loops |
| M06 roles/assignments/incidents/AAR services | Real duty/aircraft assignment, dispatch/map/incident UI, radio workflow, AAR and operations training |
| M07 persistence | Later durable server profile/world/history guarantees; M05A local settings persistence is independent and does not prove database recovery |
| M08/M09 | Load/UI checks, final preset qualification, packaging and release accessibility acceptance |

Avoid a circular gate: implement M05A controls/settings and available-system integration after M05. M06 then implements its server producers against agreed command/data contracts and unblocks the remaining M05A operations bindings. M06 server work depends on those contracts and M01–M05, not on falsely completed dispatch screens. **Full M05A completion and player-facing M06 acceptance both require actual M06 bindings and end-to-end evidence.** Keep dependent items open; mocks, empty screens and invented AAR values are not completion.

Earlier milestones may implement basic controls/diagnostic interfaces needed for their tests. They do not complete M05A. UI submits requests and displays results; fire/wind/water, equipment resources, permissions, vehicles and smoke hazards remain server-owned. M09 consumes the finished experience and retains final hosting/release/performance qualification. No licence or evidence requirement is waived, and this planning change installs no packages or UI framework.

## Documentation and acceptance

- [ ] Normal controls operate actual systems and complete at least one available operational loop without debug commands.
- [ ] Rebindings/settings/presets persist after restart and affect supported systems; test context changes, focus loss, invalid settings and unsupported options/devices.
- [ ] Multiplayer rejects unauthorized/conflicting interactions; clients do not decide extinguishing or resource outcomes.
- [ ] UI works at documented common resolutions with scaling, controller navigation and accessibility; tutorials use real gameplay paths and operations data.
- [ ] Document player flow, default controls/input contexts, UI navigation/hierarchy, vehicle/equipment states, settings/preset mappings and tutorial progression. Retain Mermaid or other editable source and embed inspected rendered SVG exports, following [the workflow](DOCUMENTATION_WORKFLOW.md).
- [ ] Publish actual BEFORE/AFTER screenshots/UI comparisons (or "No baseline was captured"), real reference-hardware preset measurements, tests and limitations; update README with actual progress. Include relevant physics comparisons if mechanics change and explain performance graph ticks/intervals.

M05A remains incomplete while required controls, bindings, settings or evidence are missing. [TASKS.md](../TASKS.md) owns status; no placeholder controls/UI count as a completed milestone.
