using System.Diagnostics;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>Audit project-owned C# comments using the compiler bundled with the installed .NET SDK.</summary>
internal static class Program
{
    /// <summary>
    /// Compare executable tokens against the supplied Git revision under client/server/Editor symbols.
    /// Also require explanations on every project method, constructor, local function and property.
    /// Run from the repository root; print JSON and return failure for missing comments or changed code.
    /// This checks syntax/token identity, not the factual correctness of the prose.
    /// </summary>
    private static int Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Pass the Git revision before the comment pass.");
        var files = Directory.GetFiles("Assets/Fireline", "*.cs", SearchOption.AllDirectories).Order().ToArray();
        var missing = new SortedSet<string>();
        var changed = new SortedSet<string>();
        var members = new HashSet<string>();
        string[][] configurations = [[], ["UNITY_EDITOR"], ["UNITY_SERVER"], ["UNITY_CLIENT"], ["UNITY_EDITOR", "UNITY_SERVER"]];
        foreach (string file in files)
        {
            string path = file.Replace('\\', '/');
            string before = Git("show", args[0] + ":" + path);
            string after = File.ReadAllText(file);
            foreach (var symbols in configurations)
            {
                var options = CSharpParseOptions.Default.WithPreprocessorSymbols(symbols);
                var oldRoot = CSharpSyntaxTree.ParseText(before, options).GetRoot();
                var root = CSharpSyntaxTree.ParseText(after, options).GetRoot();
                if (!Tokens(oldRoot).SequenceEqual(Tokens(root))) changed.Add(path);
                if (root.ContainsDiagnostics && root.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
                    throw new InvalidOperationException("C# syntax failed: " + path);
                foreach (var node in root.DescendantNodes().Where(n => n is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or PropertyDeclarationSyntax))
                {
                    string name = node switch
                    {
                        MethodDeclarationSyntax m => m.Identifier.Text,
                        ConstructorDeclarationSyntax c => c.Identifier.Text,
                        LocalFunctionStatementSyntax l => l.Identifier.Text,
                        PropertyDeclarationSyntax p => p.Identifier.Text,
                        _ => node.Kind().ToString()
                    };
                    string key = path + ":" + (node.GetLocation().GetLineSpan().StartLinePosition.Line + 1) + ":" + name;
                    members.Add(key);
                    string trivia = node.GetLeadingTrivia().ToFullString();
                    if (!trivia.Contains("/// <summary>") && !(node is LocalFunctionStatementSyntax && trivia.Contains("// ")))
                        missing.Add(key);
                }
            }
        }
        Console.WriteLine(JsonSerializer.Serialize(new { baseline = args[0], files = files.Length,
            configurations = configurations.Length, documentedMembers = members.Count - missing.Count,
            missingComments = missing, changedExecutableTokens = changed }, new JsonSerializerOptions { WriteIndented = true }));
        return missing.Count == 0 && changed.Count == 0 ? 0 : 1;
    }

    /// <summary>Keep token kinds and exact literal text, excluding comment/whitespace trivia.</summary>
    private static IEnumerable<string> Tokens(SyntaxNode root) => root.DescendantTokens().Select(t => t.RawKind + ":" + t.Text);

    /// <summary>Read a Git object with argument-safe process launching; fail if the requested baseline cannot be read.</summary>
    private static string Git(params string[] args)
    {
        var start = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start git.");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException(error);
        return output;
    }
}
