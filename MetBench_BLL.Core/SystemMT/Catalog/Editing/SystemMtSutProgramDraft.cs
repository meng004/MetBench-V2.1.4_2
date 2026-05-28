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

    public static SystemMtSutProgramDraft NewForSut(string sutId) => new()
    {
        SutName = sutId,
        ProgramName = sutId,
        ProgramType = "Num",
        PythonExecutableKind = PythonExecutableKinds.System,
    };

    public static SystemMtSutProgramDraft FromProgram(string sutName, ProgramDefinition program) => new()
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
