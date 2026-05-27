using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Pipeline;

/// <summary>
/// PR-Bol-2A: one phase in a multi-phase refinement sequence consumed by
/// <see cref="SystemMtPipeline.ExecuteMultiPhaseAsync"/>. Each phase runs the SUT
/// once with its own <see cref="Parameters"/> dict (which overrides the blueprint's
/// <c>DefaultParameters</c> for that phase); the resulting metrics become a single
/// <c>RoleOutput</c> keyed by <see cref="Role"/>. Phase order is significant — the
/// last phase in the list maps to <c>ErrorMonotonicPredicate.ReferenceRole</c>; the
/// earlier phases become <c>OrderedRoles</c> in declared order.
/// </summary>
public sealed record RefinementPhase(
    string Role,
    IReadOnlyDictionary<string, string> Parameters);
