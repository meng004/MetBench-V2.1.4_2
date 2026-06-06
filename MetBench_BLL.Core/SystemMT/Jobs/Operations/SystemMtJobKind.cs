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
    /// Exports only the rendered report files (HTML plus any available Word/Excel/PDF) for one
    /// execution — no execution-result.json / execution-evidence.json data files. Handled by
    /// <see cref="ExportReportJobOperationHandler"/>; the report-only counterpart to
    /// <see cref="ExportExecutionArtifacts"/>.
    /// </summary>
    ExportReport,
}
