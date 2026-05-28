; Unshipped analyzer releases (Microsoft.CodeAnalysis.Analyzers RS2008 tracking).
; Format: https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
METBENCH001 | MetBench.Governance | Info | MultiProjectionRecordAnalyzer — multi-projection record without ParityTests guard (CLAUDE.md §12.4 R1)
METBENCH002 | MetBench.Governance | Info | FieldFlowTracerAnalyzer — generic field-flow tracer for public sealed records with >= 5 cross-file construction sites (v2 charter §6 P3)
