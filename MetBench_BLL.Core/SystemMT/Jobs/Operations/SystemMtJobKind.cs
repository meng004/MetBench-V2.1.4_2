namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>Unified async operation kind carried by a durable System MT job.</summary>
public enum SystemMtJobKind
{
    RunMr,
    RunBatch,
    ImportAssets,
    ExportAssets,
    ExportExecutionArtifacts,

    /// <summary>
    /// Reserved for a future standalone report-export operation. No operation handler is
    /// wired yet; <see cref="SystemMtJobService"/> validates its fields and then rejects the
    /// submission with <see cref="NotSupportedException"/>. Do not add a second report-export
    /// kind — implement the handler against this member when the operation is built.
    /// </summary>
    ExportReport,
}
