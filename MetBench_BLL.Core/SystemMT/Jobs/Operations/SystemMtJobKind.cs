namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>Unified async operation kind carried by a durable System MT job.</summary>
public enum SystemMtJobKind
{
    RunMr,
    RunBatch,
    ImportAssets,
    ExportAssets,
    ExportExecutionEvidence,
    ExportReport,
}
