using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Catalog;

/// <summary>
/// PR-Bol-2A: JSON-bound counterpart of <see cref="Pipeline.RefinementPhase"/> for
/// manifest catalog.json. Lives at <c>refinement_phases[]</c> (snake_case) on an MR
/// row when <c>assertion_type_code == "error-monotonic"</c>. Each entry carries the
/// phase <see cref="Role"/> name and per-phase parameter overrides that the launcher
/// passes into the multi-phase pipeline as <c>RefinementPhase.Parameters</c>.
/// </summary>
public sealed class RefinementPhaseDefinition
{
    public string Role { get; set; } = string.Empty;

    public Dictionary<string, string> Parameters { get; set; } = new();

    public void Validate(string mrId, int phaseIdx)
    {
        if (string.IsNullOrWhiteSpace(Role))
            throw new CatalogValidationException(
                $"MrBindingDefinition '{mrId}' refinement_phases[{phaseIdx}].role must be non-blank");
        if (Parameters is null)
            throw new CatalogValidationException(
                $"MrBindingDefinition '{mrId}' refinement_phases[{phaseIdx}].parameters must not be null");
    }
}
