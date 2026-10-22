# Development documentation workflow

The permanent rules in [AGENTS.md](../AGENTS.md) apply to every future task and milestone. README is the project introduction and development showcase; detailed evidence lives here. Preserve useful historical reports, label proposed designs and never promote a template or specification to implementation evidence.

## Before changing a system

Inspect branch, working tree, Unity/package versions and current implementation; make a Git checkpoint. Identify applicable evidence before editing so a real baseline can be captured. Record the baseline revision, test inputs, random seeds, scene, camera and measurement method. If none exists, state that explicitly. A new system's BEFORE diagram describes its absence or actual prior path, not an invented simpler model.

## Evidence and locations

| Change | Required report and evidence | Destination |
|---|---|---|
| Significant visual/art/UI change | Real Game View/build AFTER image; actual BEFORE if available; what changed, assets/textures/provenance, capture conditions and performance impact | `docs/previews/<feature-name>/COMPARISON.md` plus compressed images |
| Significant physics/simulation change | Previous/new algorithms or equations, SI definitions, assumptions, rationale, two diagrams, equivalent numerical test cases, raw results, plots when useful, verification and source references | `docs/physics/<system-name>/COMPARISON.md` |
| Gameplay/architecture change | Previous/new behavior, player impact, server/network effects, Mermaid flow and tests | `docs/reports/<date>-<feature-name>.md` |
| Significant rendering/simulation change | Equivalent before/after measurement conditions, hardware, counters, raw captures and limitations | `docs/performance/<date>-<feature-name>.md`, indexed in `docs/PERFORMANCE_HISTORY.md` |
| Documentation-only change | Structure/link/consistency checks, exact scope and runtime evidence marked not applicable | `docs/reports/<date>-<topic>.md` |

Start from [templates](templates/README.md). Replace placeholders before publishing a report; retain honest unavailable/not measured entries with reasons and follow-up tasks. Never create empty image links to imply captures exist. Embed real representative images in root README using paths from the repository root. Detailed reports can link additional images, plots and raw data.

## Rendered diagrams and mathematics

For future reports, embed actual diagrams and flowcharts as self-contained SVG files beside the Markdown. Keep Mermaid/Graphviz or a compact generating script as editable source, with a reproduction command; source blocks alone do not satisfy the drawing requirement. Check labels, arrows, bounds and readability in the exported image. Prefer static SVG for GitHub and PNG only as a necessary fallback; a JavaScript canvas will not render in a repository README. Historical source-only diagrams can be converted when next edited.

Use `$...$` for inline mathematics and display equations between standalone `$$` lines. Use proper fractions, subscripts/superscripts, symbols and SI units, and define each variable. Math inside a fenced code block is source, not a formatted equation. If the intended viewer lacks math support, export and embed an SVG formula. Keep generators and reports concise; do not duplicate the same diagram or equation in several forms without a rendering need. Quantitative plots must use actual data and standard plotting tools, with labeled axes and units. See the [frame-loss register](performance/FRAME_LOSS_REGISTER.md) for rendered SVG examples and reproducible data analysis.

Before a milestone's final commit, give every performance graph a plain-language reader guide: axes/units, ruler tick spacing, recording origin and warm-up, interval/sample definition, target lines, annotations and zoomed panels. Explain whether it measures callback spacing, CPU/GPU execution or presentation loss; graph ticks are not simulation ticks. Carry that interpretation into the handoff without claiming the milestone complete while its acceptance gaps remain.

## Verify, document and publish

1. Implement and verify behavior; run the relevant unit, integration, rendering and network checks. Record commands, environments, revisions, expected/actual outcomes and artifacts.
2. Collect applicable screenshots, mathematical models, BEFORE/AFTER diagrams, numerical comparisons, plots and measurements. Use the same inputs, conditions and seeds when comparing versions.
3. Update the README showcase, dated [changelog](CHANGELOG.md), [task list](../TASKS.md) and [development status](DEVELOPMENT_STATUS.md). Link reports; update asset/licensing records and [performance history](PERFORMANCE_HISTORY.md) when applicable.
4. Review evidence coverage against the milestone acceptance criteria. Missing required screenshots, diagrams, measurements or explanations keep the milestone open. Record tooling limitations with a concrete unchecked follow-up task. Documentation-only changes do not require invented game evidence.
5. Commit implementation, tests and all applicable documentation/evidence together. Verified partial work may be committed with its outstanding acceptance gates stated explicitly.
6. Push to the existing [FIRELINE-ADRIATIC repository](https://github.com/itsmangooo/FIRELINE-ADRIATIC). Use `main` for completed verified milestones, respecting an established branch/review workflow; never force-push or create a replacement remote. Confirm remote and local revision agreement. If push fails, report the local commit and reason without claiming publication.

## Evidence vocabulary

- **Planned:** specification or intended behavior only.
- **Implemented:** source exists, with precise verification limits stated.
- **Verified:** the named acceptance check ran and passed; include evidence.
- **Not measured / unavailable:** no result; explain why and track the missing work.
- **Not applicable:** this change does not affect the category; state why.

Local tests do not establish 100-player capacity; Editor captures do not establish built-client performance; visual fire is not authoritative combustion. Keep these distinctions in README, reports and handoffs.
