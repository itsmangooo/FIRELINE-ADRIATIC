# Permanent documentation workflow — 2026-10-22

## Scope and baseline

The user requested permanent development/documentation rules, missing directories/templates, a README showcase preserving existing content, and a commit/push to the existing GitHub repository. No unrelated feature development was authorized for this increment.

Initial inspection found no local `.git` directory. `gh repo view itsmangooo/FIRELINE-ADRIATIC --json nameWithOwner,isEmpty,defaultBranchRef,url` confirmed that the existing remote was empty. A local `main` branch was initialized and attached to that remote; checkpoint `8577e4d` preserved existing Unity source/configuration and planning documents before edits. No replacement GitHub repository was created.

`ProjectSettings/ProjectVersion.txt` records Unity 6000.6.7f1; `Packages/manifest.json` declares HDRP 17.6.0. The source inventory contains `SampleScene`, HDRP presets and tutorial scripts. These observations do not establish Editor/build compatibility or implemented gameplay.

## Changes

- Permanent rules in [AGENTS.md](../../AGENTS.md) apply to every future task and milestone.
- README now has the nine required showcase sections, explicit implementation boundaries and links to reports; original pack usage, core-document inventory, operating principles and source links are retained.
- Added reusable visual, physics, gameplay/architecture and performance templates, evidence-directory guides, chronological changelog and performance history.
- Task/status records distinguish this documentation increment from the still-pending M00–M09 game milestones.
- Added `.gitignore` to exclude generated Unity/IDE state and local credential files from future commits.

## Validation

Local validation passed on 2026-10-22:

- PowerShell assertions found all nine required README headings and confirmed original operating-principle and authoritative-source sections were retained verbatim. Original pack usage and core-document content remain present.
- Checked **58 local Markdown links across 31 Markdown files**: all targets exist. All fenced code blocks are balanced. This is a structural check, not a claim of browser-rendered Mermaid validation.
- `git diff --check`: passed with no whitespace errors.
- `git diff --name-only 8577e4d -- Assets Packages ProjectSettings prompts .vsconfig`: empty; project content/configuration and original prompts are unchanged.
- `git ls-files` review: no generated Unity/IDE state tracked. `git check-ignore Library/probe Temp/probe Logs/probe UserSettings/probe 'FIRELINE ADRIATIC.slnx' Assembly-CSharp.csproj`: all six paths correctly excluded.

Publication verification is pending commit/push; compare `git rev-parse HEAD` with `git ls-remote origin refs/heads/main` after publishing.

## Applicability and limitations

Visual screenshots, physics equations/comparison runs, runtime tests and performance measurements are **not applicable** to this documentation-only increment. No visual or simulation code changed. No baseline was captured; no game screenshots or fabricated numeric examples were added. Mermaid describes the documentation workflow, and template diagrams are explicitly unfilled placeholders.

No Unity compilation, client/server launch, multiplayer check, graphical capture or hardware profiling was performed. Those remain M00 and later acceptance work, not completed evidence or a claimed tooling failure. External reference URLs retained from the original pack were not revalidated by this change.

## Next action

Begin the M00 Editor/tooling/build audit when requested. Capture actual baselines before significant visual or physics changes and keep milestones open until required evidence is available.
