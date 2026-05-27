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
/// METBENCH001 — Multi-projection record without ParityTests guard.
///
/// Phase 4 of the CI Cat B hardening plan
/// (docs/superpowers/plans/2026-05-27-ci-governance-cat-b-hardening-plan.md).
///
/// Detects <c>public sealed record</c> types constructed via <c>new T(...)</c>
/// in 2 or more distinct source files (excluding the declaration file itself).
/// Such types have multi-projection paths and a Phase-K + Phase-K+N change to
/// either side can silently desync if no ParityTests guard exists. The
/// canonical historical lesson is L1 / MrCatalogEntry.MetaPattern (T2/T3
/// post-merge review §2 finding L1).
///
/// Severity is intentionally <see cref="DiagnosticSeverity.Info"/> at MVP —
/// the analyzer informs but does not block builds. A future PR can promote
/// the rule to Warning or Error once the parity test inventory is complete.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MultiProjectionRecordAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "METBENCH001";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Multi-projection record without ParityTests guard",
        messageFormat: "Record '{0}' has {1} projection construction sites: {2}. Per CLAUDE.md §12.4 R1, ensure a corresponding ParityTests file exists and asserts every public field.",
        category: "MetBench.Governance",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Public sealed records with at least 2 distinct projection construction sites require a ParityTests file to guard against asymmetric field projection (CLAUDE.md §12.4 R1, T2/T3 chain L1 finding).",
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
        // Records of interest = public sealed records discovered via syntax.
        // record-name → (set of declaration-file paths, declaration location).
        // Syntax-only detection (per RS1030) — sufficient for name + file scope;
        // semantic model not required for this rule.
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

        // Pass 2: scan ObjectCreationExpression syntax across all trees.
        // For each record name we tracked, accumulate the set of distinct
        // source files containing a `new RecordName(...)` invocation.
        var siteFiles = new Dictionary<string, HashSet<string>>(System.StringComparer.Ordinal);
        foreach (var name in records.Keys)
        {
            siteFiles[name] = new HashSet<string>(System.StringComparer.Ordinal);
        }

        foreach (var tree in context.Compilation.SyntaxTrees)
        {
            var root = tree.GetRoot(context.CancellationToken);
            foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                var name = ExtractTypeName(creation.Type);
                if (name is null) continue;
                if (siteFiles.TryGetValue(name, out var set))
                {
                    set.Add(tree.FilePath ?? string.Empty);
                }
            }
        }

        // Pass 3: emit diagnostics for records with at least 2 distinct
        // external construction sites (= projection paths). External = files
        // other than the record's own declaration file(s). The threshold of
        // 2 filters records constructed in a single non-decl file (typically
        // a DTO returned from one factory) — those don't need parity guards.
        // Records constructed across 2+ external files indicate genuine
        // multi-projection (e.g. `CandidateMrProposal` from 3 discoverers,
        // `DiscoveryRunOutcome` from 4 sites in the discovery pipeline).
        foreach (var kv in records)
        {
            var recordName = kv.Key;
            var declFiles = kv.Value.DeclFiles;
            var declLocation = kv.Value.DeclLocation ?? Location.None;
            var sites = siteFiles[recordName];
            sites.ExceptWith(declFiles);
            if (sites.Count >= 2)
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
