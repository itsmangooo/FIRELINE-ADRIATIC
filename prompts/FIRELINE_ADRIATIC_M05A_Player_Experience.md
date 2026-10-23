# FIRELINE: ADRIATIC — Add Player Experience Milestone

**Repository:** https://github.com/itsmangooo/FIRELINE-ADRIATIC

Read the existing `TASKS.md`, `AGENTS.md`, `docs/GAME_DESIGN.md` and relevant development specifications.

Add **one dedicated milestone** to the existing development roadmap:

## M05A — Player Experience, Controls, UI/UX & Settings

Place this milestone **after M05 (Ground Firefighting) and before M06 (Operations)** without unnecessarily renumbering existing milestones.

This milestone will turn the existing technical simulations into a cohesive, accessible and genuinely playable game.

### 1. Gameplay & Player Interactions

Implement the complete player-facing interaction systems:

- First-person and optional third-person movement.
- Context-sensitive interaction system.
- Equipment selection and inventory.
- Picking up, carrying and using firefighting tools.
- Entering and exiting vehicles.
- Operating fire trucks, pumps and hoses.
- Boarding and controlling aircraft.
- Interacting with cockpit instruments and switches.
- Receiving and following incident assignments.
- Operational radio and communication interfaces.

All interactions must use the actual gameplay systems and server-authoritative state, not disconnected demonstrations.

### 2. Input & Controls

Use Unity's Input System.

Create separate control contexts for:

- On-foot movement.
- Aircraft flight.
- Ground vehicles.
- Firefighting equipment.
- Tactical map and command.
- UI and menus.

Support:

- Keyboard and mouse.
- Controller.
- Remappable keybindings.
- Sensitivity and dead-zone configuration.
- Inverted flight axes.
- Hold/toggle controls.
- Future HOTAS and joystick profiles.

Implement simplified and advanced aircraft control options where server rules permit.

### 3. UI & HUD

Create a complete, visually polished interface appropriate for a realistic firefighting simulator.

Required screens:

- Main Menu.
- Multiplayer Server Browser.
- Server Connection/Lobby.
- Unit and Duty Selection.
- Player Profile.
- Aircraft Selection and Assignment.
- Equipment and Vehicle Interaction.
- Operational Map.
- Incident Information.
- Aircraft HUD/Cockpit UI.
- Ground Firefighter HUD.
- Dispatch/Command UI.
- After Action Reports.
- Settings.

The UI must be fully functional, responsive and readable.

Use a consistent design system inspired by professional aviation and emergency-services interfaces.

### 4. Settings

Implement persistent player settings.

**Graphics:**

- Low / Performance.
- Medium / Balanced.
- High.
- Ultra.
- Custom.

Include resolution, display mode, FPS limit, VSync, render scale, texture quality, shadows, vegetation, terrain detail, volumetric fog, smoke quality, water reflections, antialiasing and supported upscaling options.

The target is **60 FPS at 1080p on documented reference hardware**.

Graphics presets must be benchmarked, not simply assigned arbitrary quality values.

Reduced graphics settings must not allow unfair visibility through gameplay-relevant smoke.

**Audio:**

- Master volume.
- Engines.
- Vehicles.
- Environmental effects.
- UI.
- Radio communications.
- Microphone settings.

**Controls:**

- Keybinds.
- Mouse/controller sensitivity.
- Flight controls.
- Axis inversion.
- Dead zones.

**Gameplay & Accessibility:**

- HUD customization.
- Subtitles.
- UI scaling.
- Colorblind-friendly overlays.
- Camera shake.
- Simplified/advanced interaction assistance.
- Measurement units.
- Tutorial hints.

### 5. Training & Tutorials

Create playable tutorials for:

- Basic movement and interaction.
- Firefighter tools.
- Pump and hose operation.
- Fire engine driving.
- Aircraft startup and flight controls.
- Water scooping and water dropping.
- Dispatch and operational map usage.

Training must use the same underlying simulation systems as normal multiplayer.

### 6. UX Documentation

Create detailed documentation for:

- Player gameplay flow.
- Default controls and input contexts.
- UI navigation and screen hierarchy.
- Vehicle/equipment interaction states.
- Settings and graphics presets.
- Tutorial progression.

Provide Mermaid diagrams showing gameplay and UI flows.

### 7. Verification & Evidence

This milestone is not complete until:

- Players can operate the available gameplay systems using normal controls.
- Keybindings can be changed and saved.
- Graphics presets can be switched and persist after restart.
- Settings correctly affect supported rendering systems.
- UI works at common resolutions.
- Players can complete at least one available operational gameplay loop without debug commands.
- Multiplayer interactions remain server-validated.
- Relevant screenshots, UI comparisons and gameplay diagrams are available.
- Actual performance measurements are documented.

Follow the permanent documentation requirements in `AGENTS.md`.

Update `README.md` with actual screenshots and progress.

Commit and push verified changes to the existing GitHub repository.

### Important — Immediate Task Scope

For now, **ONLY update the development roadmap and relevant documentation** to incorporate M05A.

**Do not immediately begin implementing this milestone.** Continue the currently active milestone according to its existing priorities.

Basic controls and temporary diagnostic interfaces may still be implemented during earlier milestones whenever necessary to test aircraft, vehicles, fire or networking.

M05A is responsible for integrating, polishing and completing the player-facing experience.

Do not present unfinished interfaces or placeholder controls as a completed milestone.
