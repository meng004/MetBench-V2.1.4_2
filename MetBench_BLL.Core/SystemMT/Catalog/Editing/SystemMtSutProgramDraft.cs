namespace MetBench_BLL.SystemMT.Catalog.Editing;

/// <summary>
/// Editable view of the <see cref="ProgramDefinition"/> section of a SUT's
/// <c>catalog.json</c>. The <c>mrs</c> section is intentionally absent — MR
/// authoring goes through <see cref="ISystemMtManifestCatalogEditor"/>.
/// Mutable so XAML two-way binding works directly; the editor copies into a
/// fresh <see cref="ProgramDefinition"/> on save.
/// </summary>
public sealed record SystemMtSutProgramDraft
{
    public string SutName { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public string Equation { get; set; } = string.Empty;
    public string EquationKey { get; set; } = string.Empty;
    public string ProgramType { get; set; } = string.Empty;
    public string RunnerScriptRelativePath { get; set; } = string.Empty;
    public string InputParserScriptRelativePath { get; set; } = string.Empty;
    public string OutputParserScriptRelativePath { get; set; } = string.Empty;
    public string InputAdapterScriptRelativePath { get; set; } = string.Empty;
    public string OutputAdapterScriptRelativePath { get; set; } = string.Empty;
    public string PythonExecutableKind { get; set; } = PythonExecutableKinds.System;
    public string ProfileProgramType { get; set; } = string.Empty;
    public string SolverMethod { get; set; } = string.Empty;
    public string RuntimeKey { get; set; } = string.Empty;
    public string InputContract { get; set; } = string.Empty;
    public string OutputContract { get; set; } = string.Empty;
    public string Adapter { get; set; } = string.Empty;
    public string DependencyRisk { get; set; } = string.Empty;

    public static SystemMtSutProgramDraft NewForSut(string sutId) => new()
    {
        SutName = sutId,
        ProgramName = sutId,
        ProgramType = "Num",
        ProfileProgramType = "Num",
        RuntimeKey = PythonExecutableKinds.System,
        PythonExecutableKind = PythonExecutableKinds.System,
    };

    public static SystemMtSutProgramDraft FromProgram(string sutName, ProgramDefinition program, SutProfileDefinition? profile = null) => new()
    {
        SutName = sutName,
        ProgramName = program.ProgramName,
        Equation = program.Equation,
        EquationKey = program.EquationKey,
        ProgramType = program.ProgramType,
        RunnerScriptRelativePath = program.RunnerScriptRelativePath,
        InputParserScriptRelativePath = program.InputParserScriptRelativePath,
        OutputParserScriptRelativePath = program.OutputParserScriptRelativePath,
        InputAdapterScriptRelativePath = program.InputAdapterScriptRelativePath,
        OutputAdapterScriptRelativePath = program.OutputAdapterScriptRelativePath,
        PythonExecutableKind = program.PythonExecutableKind,
        ProfileProgramType = profile?.ProgramType ?? string.Empty,
        SolverMethod = profile?.SolverMethod ?? string.Empty,
        RuntimeKey = profile?.RuntimeKey ?? string.Empty,
        InputContract = profile?.InputContract ?? string.Empty,
        OutputContract = profile?.OutputContract ?? string.Empty,
        Adapter = profile?.Adapter ?? string.Empty,
        DependencyRisk = profile?.DependencyRisk ?? string.Empty,
    };

    public ProgramDefinition ToProgram() => new()
    {
        ProgramName = ProgramName,
        Equation = Equation,
        EquationKey = EquationKey,
        ProgramType = ProgramType,
        RunnerScriptRelativePath = RunnerScriptRelativePath,
        InputParserScriptRelativePath = InputParserScriptRelativePath,
        OutputParserScriptRelativePath = OutputParserScriptRelativePath,
        InputAdapterScriptRelativePath = InputAdapterScriptRelativePath,
        OutputAdapterScriptRelativePath = OutputAdapterScriptRelativePath,
        PythonExecutableKind = PythonExecutableKind,
    };
}

public static class SystemMtSutProgramDraftProfileExtensions
{
    public static SutProfileDefinition ToProfile(this SystemMtSutProgramDraft draft) => new()
    {
        ProgramType = draft.ProfileProgramType,
        SolverMethod = draft.SolverMethod,
        RuntimeKey = draft.RuntimeKey,
        InputContract = draft.InputContract,
        OutputContract = draft.OutputContract,
        Adapter = draft.Adapter,
        DependencyRisk = draft.DependencyRisk,
    };
}
