# Changelog

Record meaningful development changes in dated entries, newest first. Link implementation evidence and disclose limitations. Plans and templates are not completed game features.

## 2026-10-22 — Frame-loss register and rendered documentation rules

- Analyzed the existing 896 M00 callback intervals: three exceed 20 ms; the largest is 83.47170 ms at sample #2. Added a [location register](performance/FRAME_LOSS_REGISTER.md), every-sample flags, recomputed metrics, a reproducible analyzer, and actual SVG/PNG timeline and flowchart.
- Marked screenshot/readback, instrumentation, resource-sharing and startup paths for later profiling; their contribution to individual hitches remains unproven. Optimization is deferred at the user's request. No runtime, scene, package or physical model changed; previous build/test evidence is unchanged and M00 stays open.
- Updated permanent AGENTS/workflow/template guidance: render actual SVG diagrams and flowcharts, retain editable sources, and correctly typeset Markdown mathematics with vector fallbacks when needed. Added a plain-language graph guide and required axes/tick/interval explanations before future final milestone commits.
- Verified metrics against the original JSON, rejection of inconsistent evidence, local links, SVG structure, plot/flowchart appearance and authored whitespace. See the register for exact commands and limits.

## 2026-10-22 — M00 Unity client/server foundation

- Added six modular C# assemblies, a pure fixed clock, validated direct-IP startup and role-specific foundation scenes/build entry points.
- Resolved Netcode for Entities/Entities/Transport 6.6.0 alongside existing Unity 6000.6.7f1 / HDRP 17.6.0; retained project scenes, presets and Editor version. Unity-generated serialization/build-setting changes are documented in the report.
- Verified 15 EditMode cases, Windows release client and dedicated-server builds, opposite-role assembly exclusion, a no-client failure case, and one headless server with two distinct rendered clients exiting successfully.
- Added actual [visual comparison](previews/m00-foundation/COMPARISON.md), [clock documentation](physics/foundation-clock/COMPARISON.md), [limited 720p diagnostics](performance/2026-10-22-m00-calibration.md), raw samples and build/runtime evidence. Rejected hidden/unready render captures rather than publishing them as valid results.
- M00 remains open for licensed aircraft/coastal asset integration and representative performance acceptance. No future gameplay milestone or 100-player/60 FPS claim is made. [Full report](reports/2026-10-22-m00-foundation.md).

## 2026-10-22 — Permanent documentation and GitHub showcase workflow

- Checkpointed the existing Unity project and planning pack locally at `8577e4d` before documentation edits; the existing GitHub repository was confirmed empty.
- Extended [AGENTS.md](../AGENTS.md) with permanent README, screenshot, mathematics, BEFORE/AFTER diagram, gameplay, performance and completion requirements.
- Expanded [README](../README.md) into the requested nine-section showcase while retaining original pack guidance and reference material.
- Added the [workflow guide](DOCUMENTATION_WORKFLOW.md), [templates](templates/README.md), [visual](previews/README.md), [physics](physics/README.md) and [performance](performance/README.md) evidence directories and [performance history](PERFORMANCE_HISTORY.md).
- Updated task/status tracking and added Unity/IDE cache exclusions. Unity assets, scripts, scenes, package configuration and project settings were not changed by the workflow increment.
- Validation and scope are recorded in the [documentation report](reports/2026-10-22-documentation-workflow.md). No gameplay, physics, screenshot, FPS or multiplayer-capacity claim is made; all game milestones remain pending.
