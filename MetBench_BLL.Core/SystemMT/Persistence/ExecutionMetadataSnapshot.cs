using System;

namespace MetBench_BLL.SystemMT.Persistence;

/// <summary>
/// 5D-tag / catalog-source snapshot captured at execution time for every System-MT run.
/// Provides a stable reverse link from execution → MR taxonomy (PR #88 V3 5D-tag schema)
/// and the catalog source-of-truth (catalog.json manifest path).
/// </summary>
public sealed class ExecutionMetadataSnapshot
{
    public string MrId { get; set; } = string.Empty;

    /// <summary>
    /// Reference to <c>MetamorphicRelationV3.IdV3</c> (PR #88 V3 schema).
    /// <c>Guid.Empty</c> when the MR has no V3 record (legacy path or unwired test).
    /// </summary>
    public Guid V3MrIdRef { get; set; }

    public string SutName { get; set; } = string.Empty;
    public string Equation { get; set; } = string.Empty;
    public string ProgramType { get; set; } = string.Empty;
    public string MetaPattern { get; set; } = string.Empty;
    public string SourceLevel { get; set; } = string.Empty;
    public string FailureCorrelation { get; set; } = string.Empty;

    /// <summary>Path of the manifest the runtime catalog came from (e.g. "SUT/openmoc/catalog.json"). Empty if hardcoded.</summary>
    public string CatalogManifestPath { get; set; } = string.Empty;

    /// <summary>MetBench application version that produced the run; used for cross-version diffing.</summary>
    public string MetbenchVersion { get; set; } = string.Empty;
}
