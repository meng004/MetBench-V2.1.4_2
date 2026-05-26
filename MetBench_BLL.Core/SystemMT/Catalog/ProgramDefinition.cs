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
    /// <summary>
    /// Manifest runtime key. Built-in keys (<c>system</c>, <c>openmoc</c>, <c>openmc</c>,
    /// <c>scipy</c>) are listed in <see cref="PythonExecutableKinds"/>, but the vocabulary is
    /// open — new SUT runtime families (e.g. <c>fenics</c>, <c>fipy</c>) are configured via
    /// <see cref="MetBench_BLL.SystemMT.Launcher.LauncherOptions.RuntimePythons"/> without
    /// editing this enum. Fail-closed behaviour for unknown non-system keys is enforced at
    /// resolution time by <see cref="MetBench_BLL.SystemMT.Launcher.LauncherOptions.ResolvePythonExecutable"/>.
    /// </summary>
    public string PythonExecutableKind { get; set; } = PythonExecutableKinds.System;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ProgramName))
            throw new CatalogValidationException("ProgramDefinition.ProgramName is required");
        if (string.IsNullOrWhiteSpace(RunnerScriptRelativePath))
            throw new CatalogValidationException(
                $"ProgramDefinition '{ProgramName}' RunnerScriptRelativePath is required");
        // PythonExecutableKind is NOT validated against a closed set here. Manifest authors
        // may declare new runtime keys (fenics, fipy, torch-surrogate, ...) without code
        // changes; the launcher resolves them through LauncherOptions.RuntimePythons and
        // fails closed at resolution time when unconfigured.
        if (string.IsNullOrWhiteSpace(PythonExecutableKind))
            throw new CatalogValidationException(
                $"ProgramDefinition '{ProgramName}' PythonExecutableKind is required (use \"system\" for stdlib-only SUTs)");
    }
}
