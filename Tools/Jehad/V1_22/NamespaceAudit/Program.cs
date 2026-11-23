using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class Program
{
    private static string Namespace(SyntaxNode node) => string.Join(".", node.Ancestors()
        .OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(n => n.Name.ToString()));

    private static IEnumerable<string> VisibleNamespaces(SyntaxNode node, CompilationUnitSyntax root)
    {
        string own = Namespace(node);
        while (own.Length > 0)
        {
            yield return own;
            int dot = own.LastIndexOf('.');
            own = dot < 0 ? "" : own.Substring(0, dot);
        }
        var usings = root.Usings.Concat(node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().SelectMany(n => n.Usings));
        foreach (var u in usings)
            if (u.Alias == null && u.StaticKeyword.IsKind(SyntaxKind.None) && u.Name != null) yield return u.Name.ToString();
    }

    private static int Main(string[] args)
    {
        if (args.Length != 2) { Console.Error.WriteLine("Usage: NamespaceAudit REPO_ROOT REPORT.json"); return 2; }
        string repo = Path.GetFullPath(args[0]);
        string sourceRoot = Path.Combine(repo, "Assets", "_Project", "Scripts");
        string[] files = Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal).ToArray();
        if (files.Length == 0) throw new InvalidOperationException("No project sources found.");
        string digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", files.Select(f =>
            Path.GetRelativePath(repo, f).Replace('\\', '/') + "\0" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f))).ToLowerInvariant()))))).ToLowerInvariant();
        var findings = new List<object>();
        var syntaxErrors = new List<object>();
        int indexedTypes = 0, auditedFiles = 0, references = 0;
        foreach (string configuration in new[] { "Editor", "Player" })
        {
            var options = new CSharpParseOptions(LanguageVersion.CSharp9,
                preprocessorSymbols: configuration == "Editor" ? new[] { "UNITY_EDITOR" } : Array.Empty<string>());
            var roots = files.Select(f => (Path: f, Root: CSharpSyntaxTree.ParseText(File.ReadAllText(f), options, f).GetCompilationUnitRoot())).ToArray();
            var types = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var source in roots)
            {
                foreach (var d in source.Root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
                {
                    // Nested type lookup is outside this focused namespace audit.
                    if (d.Ancestors().OfType<BaseTypeDeclarationSyntax>().Any()) continue;
                    string ns = Namespace(d);
                    if (!ns.StartsWith("Echoheart.", StringComparison.Ordinal)) continue;
                    string name = d.Identifier.ValueText;
                    if (!types.TryGetValue(name, out var namespaces)) types[name] = namespaces = new HashSet<string>(StringComparer.Ordinal);
                    namespaces.Add(ns);
                }
            }
            indexedTypes = Math.Max(indexedTypes, types.Count);
            int count = 0;
            foreach (var source in roots.Where(s => s.Path.Replace('\\', '/').Contains("/Jehad/", StringComparison.Ordinal)))
            {
                count++;
                string path = Path.GetRelativePath(repo, source.Path).Replace('\\', '/');
                foreach (var error in source.Root.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
                    syntaxErrors.Add(new { configuration, path, diagnostic = error.ToString() });
                foreach (var id in source.Root.DescendantNodes().OfType<IdentifierNameSyntax>())
                {
                    string name = id.Identifier.ValueText;
                    if (!types.TryGetValue(name, out var namespaces)) continue;
                    if (id.Ancestors().OfType<UsingDirectiveSyntax>().Any()) continue;
                    if (id.Parent is QualifiedNameSyntax || id.Parent is AliasQualifiedNameSyntax) continue;
                    if (id.Parent is MemberAccessExpressionSyntax member && member.Name == id) continue;
                    // A local method group assigned to an event/delegate is not a type reference.
                    // The menus deliberately name their health-event handler Health.
                    if (id.Parent is AssignmentExpressionSyntax assignment && assignment.Right == id &&
                        id.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Members
                            .OfType<MethodDeclarationSyntax>().Any(m => m.Identifier.ValueText == name) == true) continue;
                    references++;
                    if (namespaces.Overlaps(VisibleNamespaces(id, source.Root))) continue;
                    var point = id.GetLocation().GetLineSpan().StartLinePosition;
                    findings.Add(new { configuration, path, line = point.Line + 1, column = point.Character + 1,
                        identifier = name, candidate_namespaces = namespaces.OrderBy(n => n, StringComparer.Ordinal).ToArray() });
                }
            }
            auditedFiles = Math.Max(auditedFiles, count);
        }
        var report = new { result = findings.Count == 0 && syntaxErrors.Count == 0 ? "PASS" : "FAIL",
            project_source_files_indexed = files.Length, distinct_top_level_project_type_names = indexedTypes,
            indexed_sources_sha256 = digest,
            jehad_source_files_audited = auditedFiles, configurations = new[] { "Editor", "Player" },
            candidate_references_checked = references, missing_namespace_candidates = findings, syntax_errors = syntaxErrors,
            scope = "Roslyn syntax/import audit of known top-level Echoheart type names in Jehad scripts. No Unity compilation, external API validation, symbol binding or gameplay execution." };
        string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        string target = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.WriteAllText(target, json + "\n");
        Console.WriteLine(json);
        return findings.Count == 0 && syntaxErrors.Count == 0 ? 0 : 1;
    }
}
