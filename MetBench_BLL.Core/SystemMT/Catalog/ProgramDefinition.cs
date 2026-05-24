using System.Collections.Generic;
using System.Linq;

namespace MetBench_BLL.SystemMT.Catalog;

public sealed class ProgramDefinition
{
    public string ProgramName { get; set; } = string.Empty;
    public string Equation { get; set; } = string.Empty;
    public string EquationKey { get; set; } = string.Empty;
    public string ProgramType { get; set; } = string.Empty;
    public string RunnerScriptRelativePath { get; set; } = string.Empty;
    public string InputParserScriptRelativePath { get; set; } = string.Empty;
    public string OutputParserScriptRelativePath { get; set; } = string.Empty;
    /// <summary>Program-level default input adapter; bindings may override via MrBindingDefinition.InputAdapterScriptRelativePath.</summary>
    public string InputAdapterScriptRelativePath { get; set; } = string.Empty;
    /// <summary>Program-level default output adapter; bindings may override via MrBindingDefinition.OutputAdapterScriptRelativePath.</summary>
    public string OutputAdapterScriptRelativePath { get; set; } = string.Empty;
    /// <summary>One of <see cref="PythonExecutableKinds"/>: "system" / "openmoc" / "openmc".</summary>
    public string PythonExecutableKind { get; set; } = PythonExecutableKinds.System;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ProgramName))
            throw new CatalogValidationException("ProgramDefinition.ProgramName is required");
        if (string.IsNullOrWhiteSpace(RunnerScriptRelativePath))
            throw new CatalogValidationException(
                $"ProgramDefinition '{ProgramName}' RunnerScriptRelativePath is required");
        if (!PythonExecutableKinds.All.Contains(PythonExecutableKind))
            throw new CatalogValidationException(
                $"ProgramDefinition '{ProgramName}' PythonExecutableKind '{PythonExecutableKind}' is not recognized; " +
                $"expected one of: {string.Join(", ", PythonExecutableKinds.All)}");
    }
}
