using System;
using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Persistence;

/// <summary>
/// Evidence-bearing aggregate persisted alongside the existing summary
/// <see cref="SystemMtResultRecord"/>. Captures sample-level traces + 5D-tag /
/// V3-MR snapshot so a failed assertion can be replayed and debugged without
/// re-running the pipeline.
/// </summary>
/// <remarks>
/// 1:1 with an Execution row (linked by <see cref="ExecutionId"/>). Persisted to its own
/// LiteDB collection (Task 6); the summary record stays summary-shaped.
/// </remarks>
public sealed class ExecutionEvidence
{
    public Guid IdEvidence { get; set; }

    /// <summary>FK to <see cref="SystemMtResultRecord"/> / Execution row.</summary>
    public Guid ExecutionId { get; set; }

    public ExecutionMetadataSnapshot Metadata { get; set; } = new();

    public List<ExecutionSampleTrace> SampleTraces { get; set; } = new();

    /// <summary>Effective transformation parameters at run time (DefaultParameters + caller overrides).</summary>
    public Dictionary<string, string> TransformationParameters { get; set; } = new();

    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}
