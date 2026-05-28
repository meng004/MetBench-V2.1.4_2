using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MetBench.Analyzers;

/// <summary>
/// METBENCH002 — Generic field-flow tracer for public sealed records with
/// elevated cross-projection footprint.
///
/// v2 charter §6 P3 (docs/superpowers/specs/2026-05-28-code-governance-v2-charter.md).
///
/// Companion to <see cref="MultiProjectionRecordAnalyzer"/> (METBENCH001).
/// Differentiation:
///   * use-site spectrum — METBENCH001 scans only <c>new T(...)</c>
///     (<see cref="ObjectCreationExpressionSyntax"/>); METBENCH002 additionally
///     scans positional / property patterns <c>is T t and { ... }</c>
///     (<see cref="RecursivePatternSyntax"/>) so pattern-match-only consumers
///     are not missed.
///   * threshold — METBENCH001 emits at ≥ 2 distinct external files;
///     METBENCH002 emits at ≥ 5 distinct external files, a curated high-fanout
///     signal designed to surface only genuinely wide-spread records.
///
/// Severity is intentionally <see cref="DiagnosticSeverity.Info"/> while the
/// rule beds in; a follow-up PR may promote to Warning once the parity-test
/// inventory has caught up.
///
/// RS1030 compliance — syntax-only; no <c>SemanticModel</c> calls.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FieldFlowTracerAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "METBENCH002";

    private const int Threshold = 5;

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Public sealed record with elevated cross-projection footprint",
        messageFormat: "Record '{0}' has {1} distinct cross-file construction sites: {2}. Per CLAUDE.md §12.4 R1 + v2 charter §6 P3, adding a field requires touching every site; verify ParityTests coverage or document a decision record.",
        category: "MetBench.Governance",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Generic AST-based field-flow tracer for public sealed records (v2 charter P3). Complements METBENCH001 with higher threshold (>=5 distinct external files) and expanded use-site syntax detection (ObjectCreation + RecursivePattern).",
        customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(OnCompilation);
    }

    private static void OnCompilation(CompilationAnalysisContext context)
    {
        // Pass 1: collect public sealed record declarations (syntax-only).
        // record-name → (set of declaration-file paths, declaration location).
        var records = new Dictionary<string, (HashSet<string> DeclFiles, Location? DeclLocation)>(System.StringComparer.Ordinal);

        foreach (var tree in context.Compilation.SyntaxTrees)
        {
            var root = tree.GetRoot(context.CancellationToken);
            foreach (var decl in root.DescendantNodes().OfType<RecordDeclarationSyntax>())
            {
                if (!decl.Modifiers.Any(SyntaxKind.PublicKeyword)) continue;
                if (!decl.Modifiers.Any(SyntaxKind.SealedKeyword)) continue;
                var name = decl.Identifier.ValueText;
                if (string.IsNullOrEmpty(name)) continue;

                if (!records.TryGetValue(name, out var entry))
                {
                    entry = (new HashSet<string>(System.StringComparer.Ordinal), decl.Identifier.GetLocation());
                    records[name] = entry;
                }
                entry.DeclFiles.Add(tree.FilePath ?? string.Empty);
            }
        }

        if (records.Count == 0) return;

        // Pass 2: scan expanded use-site spectrum across all trees.
        // (a) ObjectCreationExpressionSyntax — `new T(...)`
        // (b) RecursivePatternSyntax with non-null Type — `is T t and { ... }`
        var siteFiles = new Dictionary<string, HashSet<string>>(System.StringComparer.Ordinal);
        foreach (var name in records.Keys)
        {
            siteFiles[name] = new HashSet<string>(System.StringComparer.Ordinal);
        }

        foreach (var tree in context.Compilation.SyntaxTrees)
        {
            var root = tree.GetRoot(context.CancellationToken);
            foreach (var node in root.DescendantNodes())
            {
                string? name = null;
                switch (node)
                {
                    case ObjectCreationExpressionSyntax oc:
                        name = ExtractTypeName(oc.Type);
                        break;
                    case RecursivePatternSyntax rp when rp.Type is not null:
                        name = ExtractTypeName(rp.Type);
                        break;
                    default:
                        continue;
                }

                if (name is null) continue;
                if (siteFiles.TryGetValue(name, out var set))
                {
                    set.Add(tree.FilePath ?? string.Empty);
                }
            }
        }

        // Pass 3: emit diagnostics for records whose external (non-decl-file)
        // use-site footprint meets the curated high-fanout threshold.
        foreach (var kv in records)
        {
            var recordName = kv.Key;
            var declFiles = kv.Value.DeclFiles;
            var declLocation = kv.Value.DeclLocation ?? Location.None;
            var sites = siteFiles[recordName];
            sites.ExceptWith(declFiles);
            if (sites.Count >= Threshold)
            {
                var fileList = string.Join(
                    ", ",
                    sites
                        .OrderBy(s => s, System.StringComparer.Ordinal)
                        .Select(Path.GetFileName));
                context.ReportDiagnostic(Diagnostic.Create(Rule, declLocation, recordName, sites.Count, fileList));
            }
        }
    }

    private static string? ExtractTypeName(TypeSyntax typeSyntax)
    {
        return typeSyntax switch
        {
            IdentifierNameSyntax id => id.Identifier.ValueText,
            QualifiedNameSyntax qn => qn.Right.Identifier.ValueText,
            GenericNameSyntax gn => gn.Identifier.ValueText,
            _ => null,
        };
    }
}
