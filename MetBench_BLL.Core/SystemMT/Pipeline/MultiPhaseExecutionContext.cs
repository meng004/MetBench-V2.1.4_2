using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Pipeline;

/// <summary>
/// PR-Bol-2A: input to <see cref="SystemMtPipeline.ExecuteMultiPhaseAsync"/>.
/// Wraps a <see cref="PipelineContext"/> (carrying the SUT command paths, working
/// directory, version snapshot, etc.) plus an ordered list of <see cref="RefinementPhase"/>
/// that the multi-phase pipeline will execute serially. The <c>Base</c> context must
/// also carry a pre-built <c>TypedSpec</c> + <c>TypedPredicate</c> (synthesized by the
/// launcher via <see cref="MetBench_BLL.SystemMT.Catalog.Typed.Migration.TypedSpecFactory.ForErrorMonotonic"/>)
/// — the multi-phase pipeline does not perform string-code dispatch and relies on the
/// typed pair being provided.
/// </summary>
public sealed record MultiPhaseExecutionContext(
    PipelineContext Base,
    IReadOnlyList<RefinementPhase> Phases);
