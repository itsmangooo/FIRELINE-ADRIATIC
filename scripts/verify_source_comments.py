"""Verify the post-M01 comment pass against its Git checkpoint without running acquisition/build scripts.

Run from the checkout root with Python, PowerShell and the installed .NET 10 SDK.
C# validation uses Roslyn from that SDK; no Unity packages or NuGet dependencies are added.
"""
import argparse
import ast
import json
from pathlib import Path
import shlex
import subprocess
import sys


def run(*args):
    """Run an argument-safe read/analysis command, capture text, and fail on any nonzero exit."""
    return subprocess.check_output(args, text=True, encoding="utf-8-sig")


def python_tree(text):
    """Parse Python while removing only genuine first-body docstrings for executable-AST comparison."""
    tree = ast.parse(text)
    for node in ast.walk(tree):
        if isinstance(node, (ast.Module, ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef)) and ast.get_docstring(node) is not None:
            node.body = node.body[1:]
    return ast.dump(tree, include_attributes=False)


def main():
    """Check C# tokens, Python ASTs, PowerShell tokens and shell tokens against the supplied baseline; print scoped JSON."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("baseline")
    args = parser.parse_args()
    paths = run("git", "ls-tree", "-r", "--name-only", args.baseline, "scripts").splitlines()
    checked = {"python": 0, "powershell": 0, "shell": 0}
    for name in paths:
        path = Path(name)
        if path.suffix not in (".py", ".ps1", ".sh"):
            continue
        before = run("git", "show", f"{args.baseline}:{name}")
        after = path.read_text(encoding="utf-8-sig")
        if path.suffix == ".py":
            assert python_tree(before) == python_tree(after), name
            for node in ast.walk(ast.parse(after)):
                if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):
                    assert ast.get_docstring(node), (name, node.name)
            checked["python"] += 1
        elif path.suffix == ".sh":
            assert shlex.split(before, comments=True, posix=False) == shlex.split(after, comments=True, posix=False), name
            checked["shell"] += 1
        else:
            # Ask PowerShell's parser, preserving exact literal text and command arguments.
            command = r"""
$file = $env:FIRELINE_COMMENT_AUDIT_FILE
$revision = $env:FIRELINE_COMMENT_AUDIT_BASE
$before = (git show ($revision + ':' + $file)) -join "`n"
$after = Get-Content -LiteralPath $file -Raw
# Parse one script and retain executable tokens, ignoring only comments, newlines and end markers.
function Tokens([string]$source) {
    $items = $null; $errors = $null
    [System.Management.Automation.Language.Parser]::ParseInput($source, [ref]$items, [ref]$errors) | Out-Null
    if ($errors.Count) { throw 'PowerShell syntax error' }
    @($items | Where-Object { $_.Kind -notin @('Comment', 'NewLine', 'EndOfInput') } | ForEach-Object { $_.Kind.ToString() + ':' + $_.Text.Replace("`r`n", "`n") })
}
if (Compare-Object (Tokens $before) (Tokens $after) -SyncWindow 0) { throw 'Executable PowerShell tokens changed' }
"""
            import os
            environment = dict(os.environ, FIRELINE_COMMENT_AUDIT_FILE=name, FIRELINE_COMMENT_AUDIT_BASE=args.baseline)
            subprocess.run(["powershell", "-NoProfile", "-Command", command], env=environment, check=True, capture_output=True)
            checked["powershell"] += 1
    csharp = json.loads(run("dotnet", "run", "--project", "scripts/CommentAudit", "--artifacts-path",
                            "Builds/CommentAudit/artifacts", "--", args.baseline))
    print(json.dumps({"baseline": args.baseline, "passed": True, "csharp": csharp,
                      "unchangedScriptCode": checked,
                      "limits": "Comments/docstrings only relative to the baseline; prose accuracy still requires review. New audit tools are outside their own baseline comparison."}, indent=2))


if __name__ == "__main__":
    main()
