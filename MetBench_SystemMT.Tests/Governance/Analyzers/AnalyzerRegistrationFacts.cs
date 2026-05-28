using System.Linq;
using MetBench.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace MetBench_SystemMT.Tests.Governance.Analyzers;

/// <summary>
/// v2 charter §6 P3 — reflection-only smoke facts that lock in the Roslyn
/// analyzer registration surface (DiagnosticId + DefaultSeverity + Category).
/// No SyntaxTree / Compilation harness is set up: these tests are pure
/// instantiation + descriptor introspection, and exist so a future PR that
/// silently drops METBENCH002 (or downgrades severity / renames the category)
/// turns the CI test job red.
/// </summary>
public sealed class AnalyzerRegistrationFacts
{
    [Fact]
    public void METBENCH001_is_registered_with_Info_severity()
    {
        var analyzer = new MultiProjectionRecordAnalyzer();
        Assert.Contains(
            analyzer.SupportedDiagnostics,
            d => d.Id == "METBENCH001" && d.DefaultSeverity == DiagnosticSeverity.Info);
    }

    [Fact]
    public void METBENCH002_is_registered_with_Info_severity()
    {
        var analyzer = new FieldFlowTracerAnalyzer();
        Assert.Contains(
            analyzer.SupportedDiagnostics,
            d => d.Id == "METBENCH002" && d.DefaultSeverity == DiagnosticSeverity.Info);
    }

    [Fact]
    public void METBENCH002_category_is_MetBench_Governance()
    {
        var analyzer = new FieldFlowTracerAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics.Single(d => d.Id == "METBENCH002");
        Assert.Equal("MetBench.Governance", descriptor.Category);
    }
}
