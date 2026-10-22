# Evidence report templates

These are unfilled templates, not implementation or verification evidence. Copy the appropriate template to its destination and replace all `TBD` entries before calling a report complete. Link actual source revisions and artifacts. If evidence is unavailable, describe the limitation and add a concrete task rather than a fictional result.

| Template | Destination |
|---|---|
| [Visual change](VISUAL_CHANGE.md) | `docs/previews/<feature-name>/COMPARISON.md` |
| [Physics comparison](PHYSICS_COMPARISON.md) | `docs/physics/<system-name>/COMPARISON.md` |
| [Gameplay/architecture change](GAMEPLAY_ARCHITECTURE_CHANGE.md) | `docs/reports/<date>-<feature-name>.md` |
| [Performance run](PERFORMANCE_RUN.md) | `docs/performance/<date>-<feature-name>.md` |

Relative links must be resolved from the report's final location. Add real image Markdown only after capture files exist. Preserve the two distinct BEFORE/AFTER diagrams in a physics report, replacing the template nodes with actual behavior. For a new system, document the real absence of a baseline; numerical before/after deltas are unavailable in that case.

Embed actual rendered SVG diagrams/flowcharts, retaining editable source and a generation command; a Mermaid block alone is insufficient. Use GitHub inline/display math (`$...$` / standalone `$$` delimiters), with an SVG formula fallback where the target viewer needs one. Verify the rendered output. Follow the [rendering rules](../DOCUMENTATION_WORKFLOW.md#rendered-diagrams-and-mathematics); do not add image links until the real artifacts exist.
