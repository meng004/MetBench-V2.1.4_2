namespace MetBench_BLL.SystemMT.Catalog.Editing;

/// <summary>
/// One-line summary of a SUT in the System MT catalog. Returned by
/// <see cref="ISystemMtSutEditor.ListSuts"/> for list views; the full program
/// payload lives in <see cref="SystemMtSutProgramDraft"/>.
/// </summary>
public sealed record SystemMtSutDescriptor(
    string SutId,
    string ManifestPath,
    string ProgramName,
    string Equation);
