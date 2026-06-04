using System;
using System.Collections.Generic;
using System.Linq;
using MetBench_BLL.SystemMT.Runtime;

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

    /// <summary>
    /// ExecutionEvidence v2 typed-verification block. Null on rows written by
    /// pre-v2 builds and on rows whose recorder did not receive a typed
    /// verification or property result; populated when the System MT pipeline
    /// dispatches through the Typed Semantic Catalog runtime.
    /// </summary>
    public TypedVerificationEvidence? TypedVerification { get; set; }

    /// <summary>
    /// Nullable runtime preflight evidence. Null for rows written before T1
    /// runtime governance and for callers that do not run a runtime preflight.
    /// </summary>
    public RuntimeEvidence? RuntimeEvidence { get; set; }

    /// <summary>
    /// PR-3 pair-level quality summary. Default-empty so evidence rows written
    /// before this field existed remain safe to read and render.
    /// </summary>
    private PairQualitySummary? _pairQuality = new();

    public PairQualitySummary PairQuality
    {
        get => _pairQuality ??= new();
        set => _pairQuality = value ?? new();
    }
}

public sealed class RuntimeEvidence
{
    public string RuntimeKey { get; set; } = string.Empty;

    public string RuntimeProfileDisplayName { get; set; } = string.Empty;

    public string RuntimeKind { get; set; } = string.Empty;

    public string ResolvedExecutablePath { get; set; } = string.Empty;

    public bool Passed { get; set; }

    public string FailureKind { get; set; } = RuntimeFailureKind.None.ToString();

    public string FailureDetail { get; set; } = string.Empty;

    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;

    public List<RuntimeCheckEvidence> Diagnostics { get; set; } = new();

    public static RuntimeEvidence FromPreflightResult(RuntimePreflightResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new RuntimeEvidence
        {
            RuntimeKey = result.Profile.RuntimeKey,
            RuntimeProfileDisplayName = result.Profile.DisplayName,
            RuntimeKind = result.Profile.Kind.ToString(),
            ResolvedExecutablePath = result.Profile.ExecutablePath ?? string.Empty,
            Passed = result.Passed,
            FailureKind = result.FailureKind.ToString(),
            FailureDetail = result.Detail,
            Diagnostics = result.Diagnostics.Select(RuntimeCheckEvidence.FromDiagnostic).ToList(),
        };
    }
}

public sealed class RuntimeCheckEvidence
{
    public string CheckKind { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool Passed { get; set; }

    public string FailureKind { get; set; } = RuntimeFailureKind.None.ToString();

    public string Detail { get; set; } = string.Empty;

    public bool Blocking { get; set; } = true;

    public int? ExitCode { get; set; }

    public bool TimedOut { get; set; }

    public string Stdout { get; set; } = string.Empty;

    public string Stderr { get; set; } = string.Empty;

    public static RuntimeCheckEvidence FromDiagnostic(RuntimePreflightDiagnostic diagnostic) => new()
    {
        CheckKind = diagnostic.CheckKind,
        Name = diagnostic.Name,
        Passed = diagnostic.Passed,
        FailureKind = diagnostic.FailureKind.ToString(),
        Detail = diagnostic.Detail,
        Blocking = diagnostic.Blocking,
        ExitCode = diagnostic.ExitCode,
        TimedOut = diagnostic.TimedOut,
        Stdout = diagnostic.Stdout,
        Stderr = diagnostic.Stderr,
    };
}
