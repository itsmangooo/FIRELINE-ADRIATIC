# Post-M01 — Human-readable source comments

**2026-10-23. Verified — explanatory comments and offline audit tooling.** Baseline: published M01 `cc232b921ef9472cac1f43ec36813eb44f7d31e5`. This is the requested explanatory pass before M02, not a gameplay milestone.

## Scope and reading the code

All 17 maintained `Assets/Fireline` C# files now explain their 144 methods, constructors, local functions and properties. Comments describe purpose in ordinary language, inputs/outputs, SI units, server ownership, side effects and important failure/approximation limits. Lifecycle callbacks explain when Unity calls them and why their cleanup matters. Test comments explain the behavior each fixture protects. Data-contract notes distinguish copied snapshot buffers from mutable delta DTO references.

All 14 existing tooling scripts are covered: eight Python files, four PowerShell entry scripts and two shell runners. Python functions have real docstrings, including nested helpers; PowerShell uses comment-based help for parameters and entry flow; shell runners explain input paths, cleanup and acceptance. Script-level execution paths are explained where no `main` function exists. The embedded native `SetWindowPos` declaration is explained at its Add-Type call.

Unity's two starter tutorial scripts under `Assets/TutorialInfo`, vendor packages, generated IDE/build files and downloaded assets are excluded as third-party/generated material. The user's unrelated sky-profile edit remains byte-identical and outside the commit. No project settings, scenes, assets, package versions, equations or runtime behavior are intentionally changed.

## Verification and reproduction

Reproduce this historical identity check from the repository root at the comment-pass revision `3b0e936c11db3d550da8cfdc2e6fa70c37ee65a3` (use a separate checkout). Later implementation commits intentionally change executable code and are not expected to pass an identity check against M01:

```powershell
python scripts/verify_source_comments.py cc232b921ef9472cac1f43ec36813eb44f7d31e5
unity test "$PWD" --editor-version 6000.6.7f1 --mode EditMode --filter Fireline.Tests --output Builds/CommentAudit/editmode.xml --timeout 600 -- -buildTarget Win64 -nographics
```

The new audit tools use Python, PowerShell and Roslyn bundled with the installed .NET 10 SDK, with no NuGet or Unity package addition. They verify the existing code against the named checkpoint. New audit-tool code is outside its own baseline comparison and was compiled/reviewed separately. This is a source-identity guard, not a substitute for reviewing whether prose is accurate.

| Check | Actual result |
|---|---|
| C# executable token identity | Passed in five default/Editor/server/client symbol configurations, 17 files |
| C# function/property comment coverage | 144/144, no missing explanations |
| Python executable AST identity | Passed, eight existing scripts; only genuine docstrings/comments excluded |
| PowerShell executable token identity | Passed, four scripts; normalize line endings, preserve string/argument text |
| Shell token identity | Passed, two scripts; comments excluded, quoted tokens preserved |
| Python syntax / new .NET audit tool | Passed |
| Unity regression | Passed, 34/34 Unity EditMode cases, zero failures |

The audit checks identity/coverage, not the factual correctness of every sentence or future edits. Docstrings change Python introspection text intentionally; executable AST comparison removes only genuine documentation expressions. Scripts that download assets/build games were not executed merely to test comments. Existing M00 frame-loss data, M01 numerical evidence and performance results remain unchanged.

New runtime screenshots, physics comparisons, diagrams and performance captures are **not applicable**: this increment changes source explanations and adds offline audit tooling, with no player-facing, mathematical or rendering change. Existing milestone reports remain the implementation evidence. M02 follows immediately after this verified pass; M05A stays planned.

[Source identity/coverage result](source-comments-evidence/source-audit.json) and [fresh Unity NUnit report](source-comments-evidence/editmode.xml). Windows/Linux player builds were not repeated for comments: executable C# tokens are identical to the verified M01 source. New audit tooling compiles independently; the full Unity regression passed.
